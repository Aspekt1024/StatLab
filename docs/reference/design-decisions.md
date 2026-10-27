# Design decisions

<!-- Layer 3: reference. Read only when a "why" question comes up. -->

| Decision | Choice | Why |
| --- | --- | --- |
| Combat | Turn-based first; realtime mode planned on the same Core | Turn-based is easier to balance, paper-prototype and network. Core counts steps, never clock time, so realtime is a different loop (fixed-rate ticks) over the same state |
| Time | Core advances in discrete steps (`Step` counter); turn-based is one step per action, realtime is fixed-rate ticks | No clock time in Core (rule 3); one model serves both modes |
| Multiplayer | Co-op (2–4, peer-hosted) as default | Small-group play; no servers |
| Architecture | Presentation separate from core logic | Testability; co-op becomes "send commands" |
| State changes | `Apply(state, command) → events`. State is mutable, changed only by Core (`internal` setters); the RNG and all counters live inside the state | Determinism, replays, co-op. Mutable fits `StatSheet` and avoids allocating per step; one state object means one snapshot captures everything |
| Fight rules vs strategy | Turn order, fight end and winner (by `Team`) live in Core; choosing commands (`ICombatPolicy`) and the duel driver live in `StatLab.Sim` | The game and the sim must share the rules or balance results won't match play; strategies differ per caller (player input, enemy AI, balance policies) |
| Rollback | Snapshots taken outside `Apply` by a history type; every mutable state type has a deep `Clone()` and an allocation-free `CopyFrom(other)`; a state checksum detects missed fields and desyncs | Undo, previews, AI lookahead and desync recovery now; a ring buffer of pre-allocated snapshots for realtime rollback later. Sweeps call `Apply` directly and pay nothing |
| Language | C# | Runs standalone and in Unity/Godot without a language boundary |
| Formulas vs numbers | Formulas in code, numbers in data | Types, tests and debugging for formulas; tuning without recompiling |
| Data format | JSON everywhere (Newtonsoft.Json, matching Unity's package), loaded once into immutable `GameData`; tables are arrays of rows; the loader reads text | One parser in the game and the sim; Unity and Godot pass text from their asset systems; rows suit a future editor. CSV only in optional import/export tools |
| Data editing | Files in `data/` are the source of truth; a custom editor reads and writes them; JSON Schema files meanwhile | The sim can't read engine assets, so the game and sim must share the files |
| Item ids | Readable keys (`"ring_of_might"`); instance GUIDs from the seeded rng once loot exists | Easy to write, diff and reference; renames are caught at load |
| Item stat rounding | Whole numbers, rounded when an item's stats are generated | Item stats are what players compare (final output under rule 6); tooltip and combat agree |
| Stat maths in data | Conversions (derived = Σ primary × rate) and every stat's limits are data (`StatRules`); non-linear formulas stay in ruleset code. Every stat must be listed in a ruleset file | Like GAS attribute-based magnitudes: new stat interactions without code changes. Listing every stat means a new `StatId` can't be half-configured |
| Data versioning | Authored data is versioned by git; each file carries a `formatVersion` so shape changes fail with a clear message. Ruleset config lists every stat; content (items, classes, saves) is sparse: unmentioned stats are 0 | Loader errors list every file to fix when a stat is added, removed or renamed |
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
- Progression model. Class tables (base stats per class per level, built in milestone 3 step 2) vs use-based growth: each stat has its own XP and levels up from use, the character levels up after enough stat level-ups, and each level-up grants a chosen reward (a skill increase as a talent source, or an ability). Either way progression only produces what combat consumes (base `StatBlock`, modifier sources, `Level`), so stats and combat don't change. Open within use-based: which events train which stats (data), whether XP is awarded after a fight from its events or live during it, XP curves and caps per stat. Under use-based, class tables become starting stats and mob definitions, and balance questions need sim training policies. Decide before `StatLab.Data` loads class tables (milestone 3 step 5).
- Save migrations (when saves exist). Saves record a version; loading runs ordered migration steps (e.g. v3 → v4 renames "Haste" to "AttackSpeed", v4 → v5 drops "Spirit"). Stats are saved by name, so renames are a name mapping. Replays aren't migrated: they record their data and report a mismatch.
- Data hash (milestone 5). A checksum of the loaded `GameData`, like `CombatState.ComputeChecksum`, recorded in replays, saves and results CSVs, and compared by co-op peers on connect.
- Derived-to-derived stat chains (e.g. Agility → AttackPower → CritChance). Not supported yet: `StatRules` allows primary sources only. Supporting chains needs an evaluation order and a load-time cycle check.
- Where game AI lives. `ICombatPolicy` starts in `StatLab.Sim`. When the first real enemy AI is written (milestone 4, with abilities), game AI policies move to Core (or a shared project), so the sim runs the same enemy behaviour as the game; otherwise balance results describe enemies players never face. Balancing strategies (best, worst, try-every-ability) stay in Sim. Open: whether AI sees only `CombatState`, or also engine information such as positions and line of sight.
- Positions and range: simple spatial state in Core (positions, distances, maybe a grid) vs the engine sending only commands. Core must decide outcomes; engine physics must not.
