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

Decided (milestone 1):

| Member | Purpose | Spec |
| --- | --- | --- |
| `double Convert(StatId derivedStat, StatBlock finalPrimaries)` | One derived stat's contribution from final primaries | `stat-pipeline.md`, Derived stats |
| `double Clamp(StatId stat, double value)` | Limits applied to each stat's final value | `stat-pipeline.md`, Caps and clamping |

Milestone 2 adds combat members; their shape is open decision 1 below.

## Open decisions (milestone 2)

Each lists the options and a recommendation. Move a decision into the sections above once it's made.

### 1. How much of an attack the ruleset decides

- **A. The ruleset resolves the whole attack (recommended):** `AttackOutcome ResolveAttack(Combatant attacker, Combatant defender, DeterministicRng rng)` returns hit/miss, crit and damage. Core owns *when* attacks happen; the ruleset owns *how* one resolves. Fits very different rulesets: WoW-style roll tables and 5e-style d20 vs armour class are both one method.
- **B. Core sequences fine-grained formulas:** `HitChance`, `CritChance`, `BaseDamage`, `Mitigation`, called by Core in a fixed order. Mirrors how `StatCalculator` owns phase order, but forces every ruleset into the same attack structure, which a bounded ruleset may not fit.

Either way, a ruleset's internal formulas (mitigation, crit) can be separate methods with their own tests.

### 2. Attack roll structure (inside the vertical ruleset)

- **A. Single roll table (recommended for vertical):** one roll decides miss / dodge / crit / hit, WoW-style. One rng draw per attack; outcome chances are easy to read and to balance.
- **B. Independent rolls:** roll to hit, then roll to crit. Simpler to explain; two draws; crit chance is effectively reduced by miss chance.

### 3. Armour mitigation formula (vertical ruleset)

- **A. Diminishing returns (recommended):** `reduction = armor / (armor + K × attackerLevel + C)`. Never reaches 100%; scales with level, which matters for balance question 1 (time-to-kill across levels). K and C are data.
- **B. Flat reduction:** `damage - armor`. Simple, but at high armour damage hits zero; needs a minimum-damage rule.

### 4. Where level lives

- **A. A combatant property, not a stat (recommended):** gear and buffs don't modify level; level gap affects hit chance and mitigation through ruleset formulas (balance question 3).
- **B. A `StatId`:** goes through the pipeline like any stat, so a "+1 level" buff is possible. Mixes a progression concept into stats.

### 5. Ruleset configuration

- **A. An immutable config record per ruleset (recommended):** e.g. `VerticalRulesetConfig` with mitigation constants, crit cap, conversion rates; passed to the ruleset's constructor. Tests build it in code now; milestone 3 loads it from `data/vertical/`.
- **B. Constants in code until milestone 3:** fastest now, but breaks rule 4 and means rewriting the ruleset later.

### 6. Ruleset identity

- **A. `string Id` on `IRuleset` (recommended):** e.g. `"vertical"`. Every results CSV records the ruleset id (design decision), and the id names the data folder.
- **B. Use the type name:** no extra member, but renaming the class silently changes recorded results.

## Not in milestone 2

- Abilities, spells, healing, resources other than health.
- Level growth curves and class definitions (data, milestone 3).
- A second (bounded) ruleset. The contract should allow one, but only the vertical ruleset is built.
