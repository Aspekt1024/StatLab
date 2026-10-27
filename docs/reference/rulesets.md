# Rulesets

<!-- Layer 3: reference. The contract between Core and a ruleset. DRAFT: see "Open decisions" before building milestone 2. -->

## What a ruleset is

A ruleset is a complete set of formulas for one formula mode (e.g. vertical WoW-style scaling, or bounded 5e-style numbers). It implements `IRuleset` and is swapped whole (AGENTS.md rule 5); formulas from different rulesets are never mixed.

## Who owns what

| Core owns | The ruleset owns |
| --- | --- |
| Phase and turn order: stat phases (`StatCalculator`), turn sequence, when attacks resolve | The numbers each step produces: conversions, limits, combat formulas |
| State and how it changes (`Apply(state, command, rng) → (state, events)`) | Nothing persistent: rulesets are stateless |
| Events, emitted for every outcome | — |
| The seeded `DeterministicRng`, passed in | Any randomness, drawn only from the rng it's given |

## Rules

- **Stateless.** A ruleset holds only immutable configuration. Same inputs (and same rng state) give the same output.
- **Formulas in code, numbers in data** (AGENTS.md rule 4). Each ruleset has its own data folder, `data/<ruleset-id>/`. Until `StatLab.Data` exists (milestone 3), tests build configuration in code.
- **Randomness only through the rng parameter** (rule 3). No `System.Random`, no clock, no static state.
- **No IO, logging or rendering** (rules 1 and 8). Outcomes are returned; Core turns them into events.

## Contract

| Member | Purpose | Spec |
| --- | --- | --- |
| `string Id` | Stable name, e.g. `"vertical"`. Recorded in every results CSV; names the data folder `data/<id>/` | — |
| `double Convert(StatId derivedStat, StatBlock finalPrimaries)` | One derived stat's contribution from final primaries | `stat-pipeline.md`, Derived stats |
| `double Clamp(StatId stat, double value)` | Limits applied to each stat's final value | `stat-pipeline.md`, Caps and clamping |
| `DamageSpec BasicAttack` | The basic attack's damage, built from the ruleset's config | Below |
| `AttackOutcome ResolveDamage(Combatant attacker, Combatant defender, DamageSpec spec, DeterministicRng rng)` | Resolves one piece of damage: lands or not, crit or not, amount after mitigation | Below |

### Attacks

- The ruleset resolves the **whole** attack; Core decides **when** attacks happen. This fits WoW-style roll tables and 5e-style d20 vs armour class alike. The ruleset's internal formulas (mitigation, crit) can be separate methods with their own tests.
- `DamageSpec(ScalesWith, Coefficient, CanMiss, CanCrit)` describes one piece of damage separately from who deals and receives it: `attacker[ScalesWith] × Coefficient`. The basic attack is the ruleset's `BasicAttack`; abilities will supply their own specs from data (milestone 4), so the ruleset never needs to know which abilities exist.
- Damage that can neither miss nor crit doesn't draw from the rng.
- No damage schools yet: until milestone 4 adds them, armour reduces all damage.
- `AttackOutcome(AttackResult Result, double Damage)`. `AttackResult` is `Miss`, `Hit` or `Critical`, shared by every ruleset; a ruleset produces only the kinds it uses. Adding a kind (e.g. `Dodge`) is an enum addition.
- Damage is after mitigation, never negative, unrounded (rule 6), and 0 on a miss.
- The ruleset never changes the combatants. Core applies the outcome to health.

### Level and configuration

- Level is a `Combatant` property, not a `StatId`. Gear and buffs don't change it; level gaps act through ruleset formulas (balance question 3).
- Tuning numbers come from an immutable config record per ruleset (e.g. `VerticalRulesetConfig`), passed to the ruleset's constructor. Tests build it in code; milestone 3 loads it from `data/<id>/`.

## Vertical ruleset (`"vertical"`)

WoW-style scaling: numbers grow a lot with level and gear. `VerticalRuleset`, tuned by `VerticalRulesetConfig`.

- **Conversions and limits:** data, through `StatRules` (below). Which primaries feed which derived stat, at what rate, and every stat's min and max are all in the ruleset file; the vertical ruleset has no stat-specific code.
- **Attack roll:** one roll in [0, 1) against stacked ranges: miss, then crit, then hit. If miss + crit exceed 1, crit is squeezed into what's left. One rng draw per attack.
- **Miss chance:** `BaseMissChance + MissChancePerLevel × levels the defender is above the attacker`. Never below base.
- **Basic attack:** `AttackPower × BasicAttackCoefficient`; can miss and crit.
- **Damage:** `attacker[spec.ScalesWith] × spec.Coefficient`, × `CritMultiplier` on a crit, then × (1 − armour reduction). No damage spread until weapons arrive (milestone 4, with abilities).
- **Armour reduction:** `armor / (armor + ArmorPerAttackerLevel × attackerLevel + ArmorConstant)`. Diminishing returns: never reaches 100%, and the same armour counts for less against higher-level attackers. `ArmorConstant` must be above 0 (validate at load, milestone 3).

## Stat rules (data-driven conversions and limits)

Like Unreal GAS's attribute-based magnitudes: the common, linear stat maths is data; non-linear formulas (armour, the roll table, miss chance) stay in the ruleset's code.

- A ruleset file lists **every stat** once. Each entry has an optional `min` and `max`, and derived stats may list `sources`: primaries with a rate. A derived stat's conversion is the sum of `primary × rate` over its sources.
- `StatRules` (Core) holds them, indexed by `StatId`. `StatRules.Validate` returns every problem at once, and the constructor throws if there are any; `StatLab.Data` reports the same problems as load errors.
- Checks: every stat listed exactly once; `min ≤ max`; only derived stats have sources; sources are primaries (no derived-to-derived chains yet: they'd need an ordering and a cycle check); no source listed twice.
- Adding a `StatId` makes every ruleset file fail to load until the new stat has an entry, so a stat can't be half-configured.

```json
{
  "formatVersion": 1,
  "stats": [
    {
      "stat": "Stamina",
      "min": 0
    },
    {
      "stat": "MaxHealth",
      "min": 1,
      "sources": [
        {
          "stat": "Stamina",
          "rate": 10
        },
        {
          "stat": "Strength",
          "rate": 2
        }
      ]
    },
    {
      "stat": "CritChance",
      "min": 0,
      "max": 0.75,
      "sources": [
        {
          "stat": "Agility",
          "rate": 0.01
        }
      ]
    }
  ],
  "attacks": {
    "basicAttackCoefficient": 1,
    "critMultiplier": 2,
    "baseMissChance": 0.05,
    "missChancePerLevel": 0.01
  },
  "armor": {
    "perAttackerLevel": 50,
    "constant": 100
  }
}
```

(Abridged: a real file lists every stat. Files use standard indented JSON, two spaces per level; layout carries no meaning.)

## Not in milestone 2

- Abilities, spells, healing, resources other than health, damage schools (milestone 4).
- Level growth curves and class definitions (data, milestone 3).
- A second (bounded) ruleset. The contract should allow one, but only the vertical ruleset is built.
