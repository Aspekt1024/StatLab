# Design decisions

<!-- Layer 3: reference. Read only when a "why" question comes up. -->

| Decision | Choice | Why |
| --- | --- | --- |
| Combat | Turn-based first; realtime mode planned on the same Core | Turn-based is easier to balance, paper-prototype and network. Core counts steps, never clock time, so realtime is a different loop (fixed-rate ticks) over the same state |
| Time | Core advances in discrete steps (`Step` counter); turn-based is one step per action, realtime is fixed-rate ticks | No clock time in Core (rule 3); one model serves both modes |
| Multiplayer | Co-op (2–4, peer-hosted) as default | Small-group play; no servers |
| Architecture | Presentation separate from core logic | Testability; co-op becomes "send commands" |
| State changes | `Apply(state, command) → events`. State is mutable, changed only by Core (`internal` setters); the RNG and all counters live inside the state | Determinism, replays, co-op. Mutable fits `StatSheet` and avoids allocating per step; one state object means one snapshot captures everything |
| Rollback | Snapshots taken outside `Apply` by a history type; every mutable state type has a deep `Clone()` and an allocation-free `CopyFrom(other)`; a state checksum detects missed fields and desyncs | Undo, previews, AI lookahead and desync recovery now; a ring buffer of pre-allocated snapshots for realtime rollback later. Sweeps call `Apply` directly and pay nothing |
| Language | C# | Runs standalone and in Unity/Godot without a language boundary |
| Formulas vs numbers | Formulas in code, numbers in data | Types, tests and debugging for formulas; tuning without recompiling |
| Data format | Plain CSV/JSON loaded once into immutable `GameData` | Engine never depends on spreadsheet formats; parsing cost is one-off |
| Stat identity | Implicit `StatId` values (declaration order) as runtime indices; persisted by name | Array indexing in hot paths; reordering or adding stats never breaks saved data or Unity assets |
| Modifier sources | Runtime-only `int` `SourceId` per instance, from a counter in game state; GUIDs only at the save boundary | Two copies of one item stay distinct; deterministic IDs; removal by source instead of re-subtracting values |
| Modifier order within a stat | Sorted by content (type, then value), not by source or insertion | Floating-point sums depend on order; content order gives identical bits for the same set of modifiers. Rounding only moves the boundary; fixed-point is too invasive |
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
- Network model. Host-authoritative (only the host must be deterministic; floating point stays fine) vs lockstep or peer rollback (every peer must produce identical bits across machines and runtimes, likely pushing towards fixed-point). Leaning host-authoritative, which fits "peer-hosted".
- Duration units for buffs, cooldowns and attack speed: steps in Core, with each mode defining how long a turn is, or durations authored per mode. Balance targets differ too (turns-to-kill vs seconds-to-kill).
- Positions and range: simple spatial state in Core (positions, distances, maybe a grid) vs the engine sending only commands. Core must decide outcomes; engine physics must not.
