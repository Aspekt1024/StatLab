# Tests: stage contract

<!-- Layer 2: "What do I do?" Read when writing or reviewing tests. -->

## Projects

| Project                      | Purpose |
|------------------------------| --- |
| `StatLab.Core.Tests` (xUnit) | Correctness of formulas, pipeline and data structures |
| `StatLab.Sim.Tests` (xUnit)  | Duel driver and policies, through Core's public API only |
| `StatLab.Data.Tests` (xUnit) | Loaders and validation, from JSON built in the test (never from `data/`) |
| `StatLab.Balance.Tests`      | Balance targets per ruleset (milestone 6, not yet created) |

## Reads

- `docs/reference/stat-pipeline.md` for expected pipeline behaviour.
- The code under test.

## Rules

- Build fixtures in code (small, explicit `StatBlock`s and modifiers). Tests never read files from `data/`.
- Exception: loader tests (`StatLab.Data.Tests`) keep JSON fixtures as files in their project's `Fixtures/` folder, copied to the test output and read with `Fixture.Read(name)`. Standard indented JSON, no inline JSON in test code. A fixture is a valid file; each test breaks one thing in code.
- Expected values are hand-calculated and written in the test, with the arithmetic in a comment.
- Compare floats with a tolerance, never `==`: `Assert.Equal(expected, actual, tolerance: 1e-9)`. Exception: determinism and rollback tests compare exactly, because their promise is identical bits.
- Use `[Fact]` for a single case, `[Theory]` with `[InlineData]` for the same behaviour over several inputs.
- Any randomness uses a fixed seed.
- One behaviour per test; name tests after the behaviour (`PercentAdd_modifiers_sum_before_multiplying`).
- Golden-master tests pin a whole outcome to one value (an rng sequence, a fight's state checksum). When one fails, find what changed the outcome; update the pinned value only if the change was intended, and say so in the commit.

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

## Milestone 2 cases

Randomness (`DeterministicRngTests`)
- [x] Seed 42 produces the pinned reference sequence
- [x] Same seed, same sequence; neighbouring seeds differ
- [x] `NextDouble` is in [0, 1)
- [x] Clone and `CopyFrom` continue the same sequence, independently

Snapshots (`StatBlockTests`, `ModifierSetTests`, `StatSheetTests`, `CombatantTests`, `CombatStateTests`)
- [x] Every state type's `Clone` and `CopyFrom` copy everything, and changing one side leaves the other alone

Vertical ruleset (`VerticalRulesetTests`)
- [x] Primaries convert to derived stats; armour has no conversion
- [x] Crit is capped, MaxHealth ≥ 1, other stats ≥ 0
- [x] Armour reduction has diminishing returns and counts for less against higher-level attackers
- [x] Miss chance grows per level the defender is above, never below base
- [x] The roll table stacks miss, crit, hit, and squeezes crit when they exceed 100%
- [x] Hit, crit and miss damage for a basic attack
- [x] Damage specs: can't miss, can't crit, coefficient, and no rng draw when nothing rolls

Applying commands (`CombatRulesTests`, `CombatStateTests`)
- [x] An attack reduces health, never below 0, and emits `AttackResolved` then `CombatantDefeated`
- [x] Rejections (unknown combatant, fight over, not your turn, defeated target) leave state untouched
- [x] Unknown command types throw
- [x] Combatants built with another ruleset, or with repeated ids, are rejected
- [x] Turn order: first listed acts first, the turn passes on, defeated combatants are skipped
- [x] The fight is over when one team has no one standing; `Winner` is the team left
- [x] Same seed and commands produce identical events
- [x] Restoring a snapshot replays identical events

Checksum (`ChecksumTests`, `CombatStateChecksumTests`, `ModifierSetTests`)
- [x] Matches published FNV-1a vectors; 0.0 and -0.0 differ; string boundaries matter
- [x] Clones and restored snapshots have equal checksums; attacks and seeds change it
- [x] The same modifiers added in a different order give the same checksum
- [x] Golden master: five rounds of the rolled duel end in the pinned checksum

Duels (`StatLab.Sim.Tests`: `DuelTests`)
- [x] A duel runs until one team is defeated, changing the state it's given
- [x] A duel that can't end stops at the step cap (default 100) as a draw
- [x] Rejected commands still count towards the cap
- [x] Same seed, same result; evenly matched combatants finish with a winner within the cap
