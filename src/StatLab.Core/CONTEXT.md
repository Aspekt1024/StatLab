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
2. **`Combatant`** (`Combat/`): id, level, `StatSheet`, current health (a resource, not a stat).
   - Open: state changes. Recommended: immutable state, where `Apply` returns a new state, matching `Apply(state, command, rng) → (state, events)`. Alternative: mutable state changed in place, simpler but makes snapshots and "what-if" runs harder.
3. **`IRuleset` combat members, config and id:** `rulesets.md` open decisions 1, 4, 5 and 6.
4. **Vertical ruleset** (`Rulesets/`): conversions, limits and attack resolution. `rulesets.md` open decisions 2 and 3.
5. **Commands and events** (`Combat/`): an `Attack` command; `Apply(state, command, rng) → (state, events)`.
   - Open: event shape. Recommended: small record types (`AttackResolved`, `CombatantDefeated`) returned as a list, as the design decision describes. Alternative: an event sink interface Core calls, which avoids the list but is harder to test.
6. **Duel loop** (`Combat/`): two combatants take turns until one is defeated.
   - Open: who acts first. Recommended: a fixed order for now (the first combatant passed in), so results don't depend on an extra roll. Alternatives: alternate who goes first per run, or an initiative stat.
   - Open: turn cap. Recommended: a maximum turn count after which the duel ends in a draw, so a duel where neither side can damage the other always ends.

Planned folders: `Stats/`, `Rulesets/`, `Combat/`, `Items/`, `Progression/`, `Random/`, plus `GameData.cs`.

## Writes

- Public types under the `StatLab.Core` namespace.
- No IO, no console output, no references to other projects in the solution.

## Done when (milestone 2)

Every case in `tests/CONTEXT.md` under milestone 2 passes (cases not yet written). They must include: the same seed and commands produce an identical event sequence, and a duel between evenly matched combatants ends within the turn cap.
