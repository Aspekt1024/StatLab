# StatLab.Core: stage contract

<!-- Layer 2: "What do I do?" Read per task. -->

The portable rules library. Class library; referenced by `StatLab.Data`, `StatLab.Sim`, tests, and later the game.

## Reads

| Input | Location | Why |
| --- | --- | --- |
| Pipeline spec | `docs/reference/stat-pipeline.md` | Order of operations, modifier types, derived stats |
| Item budget spec | `docs/reference/item-budget.md` | Milestone 3 only |
| Decisions | `docs/reference/design-decisions.md` | Only when a design question comes up |

## Does

Milestone 1, in build order:

1. `StatId` enum, usable as an array index.
2. `StatBlock`: base values in an array indexed by `StatId`.
3. `Modifier`: stat, type (`Flat`, `PercentAdd`, `PercentMult`), value, source.
4. Pipeline for a single stat.
5. Add and remove modifiers by source.
6. Derived stats: primaries first, then ruleset conversions, then the pipeline again on derived stats.

Planned folders: `Stats/`, `Rulesets/`, `Combat/`, `Items/`, `Progression/`, `Random/`, plus `GameData.cs`.

## Writes

- Public types under the `StatLab.Core` namespace.
- No IO, no console output, no references to other projects in the solution.

## Done when (milestone 1)

Every case in `tests/CONTEXT.md` under milestone 1 passes.
