# Tests: stage contract

<!-- Layer 2: "What do I do?" Read when writing or reviewing tests. -->

## Projects

| Project                      | Purpose |
|------------------------------| --- |
| `StatLab.Core.Tests` (xUnit) | Correctness of formulas, pipeline and data structures |
| `StatLab.Sim.Tests` (xUnit)  | Duel driver and policies, through Core's public API only |
| `StatLab.Balance.Tests`      | Balance targets per ruleset (milestone 6, not yet created) |

## Reads

- `docs/reference/stat-pipeline.md` for expected pipeline behaviour.
- The code under test.

## Rules

- Build fixtures in code (small, explicit `StatBlock`s and modifiers). Tests never read files from `data/`.
- Expected values are hand-calculated and written in the test, with the arithmetic in a comment.
- Compare floats with a tolerance, never `==`: `Assert.Equal(expected, actual, tolerance: 1e-9)`. Exception: determinism and rollback tests compare exactly, because their promise is identical bits.
- Use `[Fact]` for a single case, `[Theory]` with `[InlineData]` for the same behaviour over several inputs.
- Any randomness uses a fixed seed.
- One behaviour per test; name tests after the behaviour (`PercentAdd_modifiers_sum_before_multiplying`).

## Milestone 1 cases

- [x] Base value with no modifiers returns the base
- [x] Flat modifiers add
- [x] Two PercentAdd modifiers sum before multiplying (+10%, +10% → ×1.2)
- [x] Two PercentMult modifiers compound (+10%, +10% → ×1.21)
- [x] Flat applies before percentages (base 10, +5 flat, +50% → 22.5)
- [x] Removing by source restores the original value
- [x] Removing a source with modifiers on several stats removes all of them
- [x] Caps and clamps hold (crit cap, health ≥ 0)
- [x] A Stamina modifier flows through to max health via the derived phase
- [x] A modifier on a derived stat (e.g. +1% crit) applies after conversion
- [x] Negative modifiers (debuffs) behave correctly
- [x] A stat with no modifiers and a zero base returns zero
