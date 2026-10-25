# StatLab.Core: stage contract

<!-- Layer 2: "What do I do?" Read per task. -->

The portable rules library. Class library; referenced by `StatLab.Data`, `StatLab.Sim`, tests, and later the game.

## Reads

| Input | Location | Why |
| --- | --- | --- |
| Pipeline spec | `docs/reference/stat-pipeline.md` | Order of operations, modifier types, derived stats |
| Ruleset contract | `docs/reference/rulesets.md` | Milestone 2: what Core owns vs the ruleset, combat members |
| Item budget spec | `docs/reference/item-budget.md` | Milestone 3 only |
| Decisions | `docs/reference/design-decisions.md` | Only when a design question comes up |

## Does

Milestone 1 (done): `StatId`, `StatBlock`, `Modifier`, `StatPipeline`, `ModifierSet`, `StatCalculator`, `StatSheet`, `IRuleset.Convert` and `Clamp`. Spec: `docs/reference/stat-pipeline.md`.

Milestone 2, in build order. Each step lists the decisions to make before building it.

1. **`DeterministicRng`** (`Random/`), done: xoshiro256** seeded through SplitMix64, written out in Core so the sequence is identical on every runtime. A class with `Clone()`; public API is `NextDouble()` in [0, 1). Add `NextInt` when something needs it. A test pins the seed 42 sequence to reference values.
2. **`Combatant`** (`Combat/`), done: `CombatantId`, level, a private `StatSheet` read through `combatant[stat]`, current health (a resource, not a stat). Starts at full health.
   - Decided: mutable state changed only by Core (`internal` setters). Every mutable type has a deep `Clone()` and an allocation-free `CopyFrom(other)`, so a history type can snapshot before each `Apply` and realtime can later use a ring buffer. Immutable parts (rulesets, definitions, `Modifier` arrays) are shared, not copied.
   - Decided: the RNG and all counters (step, `SourceId`) live inside the game state, so `Apply(state, command)` needs no rng parameter and one snapshot captures everything. Name counters by step, not turn.
3. **`IRuleset` combat members, config and id**, done: `Id`, `BasicAttack`, and `ResolveDamage(attacker, defender, DamageSpec, rng)` returning `AttackOutcome(AttackResult, Damage)`. Contract in `rulesets.md`.
4. **Vertical ruleset** (`Rulesets/Vertical/`), done: `VerticalRuleset` and `VerticalRulesetConfig`. Formulas in `rulesets.md`, Vertical ruleset.
5. **Commands and events** (`Combat/`), done: `CombatState` (ruleset, rng, step, combatants; deep `Clone` and `CopyFrom`), `AttackCommand`, event records (`AttackResolved`, `CombatantDefeated`, `CommandRejected`), and `CombatRules.Apply(state, command) → events`.
   - `CombatState` holds its ruleset (shared, not copied) and rejects combatants built with another ruleset (rule 5).
   - Invalid commands return `CommandRejected` and leave state, including the rng, untouched. Unknown command types throw. `Step` counts accepted commands.
   - State checksum, done: `CombatState.ComputeChecksum()`, FNV-1a 64-bit (`Hashing/Checksum`), fed by an internal `AddToChecksum` on each state type. **When adding a field to a state type, update `Clone`, `CopyFrom` and `AddToChecksum` together**; they sit side by side for that reason. A golden-master test pins one fight's checksum.
6. **Turns and fight end** (`Combat/`), done: each `Combatant` has a `Team`; `CombatState` has `CurrentActor` (turn order is the order combatants were passed in, skipping the defeated), `IsOver` (one team has no one standing) and `Winner`. `Apply` rejects `NotYourTurn` and `FightOver`. The game and the sim share these rules.
7. **Duel driver** (`StatLab.Sim`, not Core), done: `Duel.Run(state, playerPolicy, enemyPolicy, maxSteps = 100)` asks each team's `ICombatPolicy` for its combatants' commands until the fight is over or the cap is hit (a draw). The game never runs it: players and enemy AI issue their own commands. The cap counts attempts, not `Step`, so rejected commands can't loop forever.

Planned folders: `Stats/`, `Rulesets/`, `Combat/`, `Hashing/`, `Items/`, `Progression/`, `Random/`, plus `GameData.cs`.

## Writes

- Public types under the `StatLab.Core` namespace.
- No IO, no console output, no references to other projects in the solution.

## Done when

- Milestone 2: done. Every milestone 2 case in `tests/CONTEXT.md` passes.
- Milestone 3: not yet planned. Its build order goes in "Does" once `StatLab.Data`'s contract is drafted.
