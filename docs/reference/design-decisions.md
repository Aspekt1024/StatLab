# Design decisions

<!-- Layer 3: reference. Read only when a "why" question comes up. -->

| Decision | Choice | Why |
| --- | --- | --- |
| Combat | Turn-based | Easier to balance, paper-prototype and network |
| Multiplayer | Co-op (2–4, peer-hosted) as default | Small-group play; no servers |
| Architecture | Presentation separate from core logic | Testability; co-op becomes "send commands" |
| State changes | `Apply(state, command, rng) → (state, events)` | Determinism, replays, co-op |
| Language | C# | Runs standalone and in Unity/Godot without a language boundary |
| Formulas vs numbers | Formulas in code, numbers in data | Types, tests and debugging for formulas; tuning without recompiling |
| Data format | Plain CSV/JSON loaded once into immutable `GameData` | Engine never depends on spreadsheet formats; parsing cost is one-off |
| Stat identity | Implicit `StatId` values (declaration order) as runtime indices; persisted by name | Array indexing in hot paths; reordering or adding stats never breaks saved data or Unity assets |
| Formula modes | Whole `IRuleset` implementations (e.g. vertical, bounded) with their own data | Mixing formulas from different systems produces nonsense |
| Items | Hand-designed, stats generated from a budget | Retuning weights updates all items |
| Co-op loot | Shared pool; items bind to a character on assignment until death | Fairness; attachment to one's own character |
| Progression | Defined arc; future content extends it | Healthier scope for a solo developer |
| Visuals | Separate track; grey-box until then; leaning 3D isometric, fixed camera | Systems first; fits turn-based play and existing 3D skills |
| Parallel sim runs | Each run seeded from `baseSeed + runIndex` | Deterministic results without a shared RNG |
| Results output | Every CSV records ruleset id, seed and run count | Results stay comparable later |

## Open questions

- Solo scaling vs group scaling (deferred). Goal: players care about their own character, not managing others' gear.
- Whether death is permanent (affects how much item binding stings).
- Engine choice; check target framework and C# version before writing much `Core` code.
