# StatLab.Sim: stage contract

<!-- Layer 2: "What do I do?" Read per task. -->

The balance simulator. Drives fights built from `StatLab.Core` using strategies, and (from milestone 5) runs sweeps from the command line and writes CSV results. Never referenced by anything (AGENTS.md rule 2); the game doesn't use it.

## Reads

| Input | Location | Why |
| --- | --- | --- |
| Ruleset contract | `docs/reference/rulesets.md` | What a fight can do, and who owns what |
| Decisions | `docs/reference/design-decisions.md` | "Fight rules vs strategy", seeding parallel runs, results output |

## Does

- **Duels** (`Duels/`), milestone 2: `Duel.Run(state, playerPolicy, enemyPolicy, maxSteps = 100)` runs a fight to the end with one `ICombatPolicy` per team, returning `DuelResult(Winner, Steps)`. `BasicAttackPolicy` attacks the first opponent standing.
- **Ability strategies**, milestone 4: policies that pick abilities (best, worst, random) for balancing.
- **CLI, sweeps, CSV output**, milestone 5.

## Rules

- Fight rules (turn order, fight end, damage) come from Core only. Sim decides *which* command, never *what it does*.
- Talks to Core through its public API only (no `InternalsVisibleTo`).
- Policies that need randomness keep their own seeded `DeterministicRng`, never the fight's, so comparing two policies doesn't shift combat rolls.
- Parallel runs are seeded `baseSeed + runIndex`; every results CSV records ruleset id, seed and run count.
- Unlike Core, Sim may do IO and console output (from milestone 5).

## Done when (milestone 2)

The duel cases in `tests/CONTEXT.md` pass.
