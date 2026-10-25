# StatLab.Data: stage contract

<!-- Layer 2: "What do I do?" Read per task. DRAFT: the project doesn't exist yet; see "Open decisions" before building milestone 3. -->

Loads the files in `data/` into an immutable `GameData` that Core and Sim use: ruleset config, item budget tables, item definitions, and (decision 7) class base stats. References Core only (AGENTS.md rule 2). Loaded once; parsing cost is one-off.

## Reads

| Input | Location | Why |
| --- | --- | --- |
| Item budget spec | `docs/reference/item-budget.md` | What items are authored with, the budget formula, load-time validation |
| Ruleset contract | `docs/reference/rulesets.md` | `VerticalRulesetConfig` fields; `ArmorConstant > 0` |
| Stat identity | `docs/reference/stat-pipeline.md`, Stats | Files store stats by name; convert to `StatId` on load |
| Decisions | `docs/reference/design-decisions.md` | Data format, stat identity, modifier sources |

## Does

Milestone 3, in build order. Steps marked **Core** add types or formulas to `StatLab.Core` (formulas live in code, rule 4); steps marked **Data** add parsing and validation here.

1. **Core: item types and the budget formula** (`Items/`). `ItemDefinition` (id, slot, item level, power modifier, stat shares), the budget tables (level curve, slot multipliers, stat weights), and `ItemBudget` turning a definition into stat modifiers: `budget = baseBudget(itemLevel) × slotMultiplier × powerModifier`, `statValue = share × budget ÷ statWeight`. Decisions 5 and 6.
2. **Core: `GameData`.** One immutable object holding everything loaded: ruleset config, budget tables, items by id. Decision 4.
3. **Data: project and ruleset config.** Create `StatLab.Data` and `StatLab.Data.Tests`; load `data/vertical/` into `VerticalRulesetConfig`, with validation. Decisions 1, 2 and 3.
4. **Data: budget tables and items.** Stat and slot names converted on load; every reference must exist; shares sum to 100%.
5. **Data: loading a whole ruleset folder into `GameData`,** reporting every error found, not just the first.
6. **Core: equipping items on a combatant.** An item's computed modifiers, stamped with a fresh `SourceId`, added to its `StatSheet`. Decision 8.

## Writes

- `GameData` (a Core type), built once and never changed.
- Load errors: file, location and message for each problem.

## Rules

- **Parsing and validation only.** No formulas here: the budget formula, conversions and combat stay in Core.
- **Names in files, ids in memory.** Stats, slots and items are referenced by name in files; unknown names are load errors, never silent zeros.
- **Every load is validated.** Bad data fails at load with a clear message, never mid-fight.
- **Deterministic.** The same files give the same `GameData`. Anything iterated (items, tables) is ordered by id, not by file or dictionary order.
- May do file IO (unlike Core), but parsing works on text, not paths (decision 3).

## Open decisions (milestone 3)

Each lists the options and a recommendation. Move a decision into the sections above once it's made.

### 1. File format

- **A. JSON for config and items, CSV for flat tables (recommended):** ruleset config and items are nested (an item has a list of shares) and read well as JSON; the level curve, slot multipliers and stat weights are plain tables that are easiest to tune in a spreadsheet and export as CSV, as you did in AutoRpg.
- **B. JSON everywhere:** one parser, one format to learn; tuning tables are less pleasant to edit in bulk.
- **C. CSV everywhere:** best for spreadsheets; nested data (item shares) needs flattening, e.g. one row per item and stat.

### 2. JSON library (if JSON is used)

- **A. `System.Text.Json` (recommended for now):** built into .NET, fast, no dependency. Works in Unity through its NuGet package, but isn't Unity's default.
- **B. Newtonsoft.Json:** Unity's standard (`com.unity.nuget.newtonsoft-json`), and what AutoRpg used. An extra dependency for the sim.

This matters most if the game loads data through `StatLab.Data` too (decision 3).

### 3. What the loader reads

- **A. Text, with a folder helper on top (recommended):** the core loader takes file contents (strings or `TextReader`s); a small helper reads a folder from disk for Sim and tests. Unity and Godot hand over text from their own asset systems (Addressables, `res://`), so the game can use the same loader without file paths.
- **B. File paths only:** simplest for Sim; the game would need its own loading code, and two loaders can disagree.

### 4. How load errors are reported

- **A. Collect them all (recommended):** loading returns either `GameData` or a list of every problem (file, row or field, message). Fixing a big data change takes one pass, not one error at a time.
- **B. Throw on the first:** simplest, but painful with many mistakes.

### 5. Item ids

- **A. Readable keys (recommended):** e.g. `"ring_of_might"`, unique per file set. Easy to write, diff and reference from other files.
- **B. GUIDs, like AutoRpg's Unity assets:** stable through renames, but unreadable in plain data files and diffs.

Either way, item **instance** GUIDs (one per dropped copy) aren't needed until loot exists; when they are, they must come from the seeded rng (rule 3).

### 6. Rounding item stats

The budget formula gives fractions (the spec's example Stamina is ≈23.88).

- **A. Round each stat to a whole number when the item is generated (recommended):** item stats are what players see and compare, like WoW's integer stats, so this counts as "final output" under rule 6. Totals stay explainable: the tooltip says 24, combat uses 24.
- **B. Keep the fractions and round only for display:** strictest reading of rule 6, but the tooltip (24) and combat (23.88) disagree.

### 7. Class base stats and level growth

`rulesets.md` lists these as milestone 3 data, and balance question 1 (time-to-kill across levels 1–20) needs them.

- **A. Include a simple table now (recommended):** base primary stats per class per level (or base plus per-level growth), loaded like the budget tables, so characters can be built at any level.
- **B. Defer to milestone 6:** milestone 3 stays about items; balance work later adds classes.

### 8. Weapons and damage spread

`rulesets.md` says damage has no spread "until weapons arrive (milestone 3)".

- **A. Defer to milestone 4 (recommended):** weapon damage ranges belong with damage specs and abilities, which arrive together in milestone 4. Milestone 3 items give stats only.
- **B. Include now:** weapon items carry min–max damage, and the basic attack rolls within it. Needs a second rng draw and a `DamageSpec` change before abilities exist.

## Done when (milestone 3)

Cases for milestone 3 in `tests/CONTEXT.md` pass (not yet written), including: the item budget spec's worked example (Strength 24, Stamina ≈24 before rounding), every validation rule in the spec rejecting bad data with a useful message, and the same folder loading to the same `GameData` every time.
