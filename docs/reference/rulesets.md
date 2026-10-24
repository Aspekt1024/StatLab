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
| `AttackOutcome ResolveAttack(Combatant attacker, Combatant defender, DeterministicRng rng)` | Resolves one whole attack: lands or not, crit or not, damage after mitigation | Below |

### Attacks

- The ruleset resolves the **whole** attack; Core decides **when** attacks happen. This fits WoW-style roll tables and 5e-style d20 vs armour class alike. The ruleset's internal formulas (mitigation, crit) can be separate methods with their own tests.
- `AttackOutcome(AttackResult Result, double Damage)`. `AttackResult` is `Miss`, `Hit` or `Critical`, shared by every ruleset; a ruleset produces only the kinds it uses. Adding a kind (e.g. `Dodge`) is an enum addition.
- Damage is after mitigation, never negative, unrounded (rule 6), and 0 on a miss.
- The ruleset never changes the combatants. Core applies the outcome to health.

### Level and configuration

- Level is a `Combatant` property, not a `StatId`. Gear and buffs don't change it; level gaps act through ruleset formulas (balance question 3).
- Tuning numbers come from an immutable config record per ruleset (e.g. `VerticalRulesetConfig`), passed to the ruleset's constructor. Tests build it in code; milestone 3 loads it from `data/<id>/`.

## Open decisions (milestone 2)

Each lists the options and a recommendation. Move a decision into the sections above once it's made.

### 2. Attack roll structure (inside the vertical ruleset)

- **A. Single roll table (recommended for vertical):** one roll decides miss / dodge / crit / hit, WoW-style. One rng draw per attack; outcome chances are easy to read and to balance.
- **B. Independent rolls:** roll to hit, then roll to crit. Simpler to explain; two draws; crit chance is effectively reduced by miss chance.

### 3. Armour mitigation formula (vertical ruleset)

- **A. Diminishing returns (recommended):** `reduction = armor / (armor + K × attackerLevel + C)`. Never reaches 100%; scales with level, which matters for balance question 1 (time-to-kill across levels). K and C are data.
- **B. Flat reduction:** `damage - armor`. Simple, but at high armour damage hits zero; needs a minimum-damage rule.

## Not in milestone 2

- Abilities, spells, healing, resources other than health.
- Level growth curves and class definitions (data, milestone 3).
- A second (bounded) ruleset. The contract should allow one, but only the vertical ruleset is built.
