# StatLab.Data: stage contract

<!-- Layer 2: "What do I do?" Read per task. The project doesn't exist yet; it's created in step 3 below. -->

Loads the JSON files in `data/` into an immutable `GameData` that Core and Sim use: ruleset config, item budget tables, item definitions and class base stats. References Core only (AGENTS.md rule 2). Loaded once; parsing cost is one-off.

## Reads

| Input | Location | Why |
| --- | --- | --- |
| Item budget spec | `docs/reference/item-budget.md` | What items are authored with, the budget formula, rounding, load-time validation |
| Ruleset contract | `docs/reference/rulesets.md` | `VerticalRulesetConfig` fields; `ArmorConstant > 0` |
| Stat identity | `docs/reference/stat-pipeline.md`, Stats | Files store stats by name; convert to `StatId` on load |
| Decisions | `docs/reference/design-decisions.md` | Data format, item ids, stat identity, modifier sources |

## Does

Milestone 3, in build order. Steps marked **Core** add types or formulas to `StatLab.Core` (formulas live in code, rule 4); steps marked **Data** add parsing and validation here.

1. **Core: item types and the budget formula** (`Items/`), done. `ItemId` and `SlotId` (readable string keys), `StatShare` (fractions: 0.6 = 60%), `ItemDefinition`, `ItemBudgetTables`, and `ItemBudget.Calculate` returning one Flat `Modifier` per share with a default source (equipping stamps it).
   - The level curve is a per-level table (one row per item level, any shape). Slots are defined in data, not an enum. Stats round half away from zero (2.5 → 3).
   - Core throws for a missing level, slot or stat weight; Data's load validation (step 5) must check shares sum to 1, weights and multipliers are above 0, and power modifiers are above 0.
2. **Core: class base stats** (`Progression/`), done. `ClassId` (readable key) and `ClassDefinition`: one full row of base stats per level, any stat (primaries and derived bases such as a class base MaxHealth). Read through `BaseStats(level)`, which returns a copy; a missing level throws. Data's validation (step 5) must check every level a character can reach has a row.
3. **Core: `GameData`.** One immutable object holding everything loaded: ruleset config, budget tables, items and classes by id.
4. **Data: project and ruleset config.** Create `StatLab.Data` and `StatLab.Data.Tests`, reference Newtonsoft.Json, and load `data/vertical/` config into `VerticalRulesetConfig` with validation. Add a JSON Schema per file type (`*.schema.json`) for autocomplete and checks in Rider.
5. **Data: budget tables, items and classes.** Before building, settle the progression model (open question in `design-decisions.md`): it decides whether class files hold per-level tables or starting stats plus XP curves. Stat, slot, item and class names converted on load; every reference must exist; shares sum to 100%.
6. **Data: loading a whole ruleset folder into `GameData`,** reporting every error found.
7. **Core: equipping items on a combatant.** An item's computed modifiers, stamped with a fresh `SourceId`, added to its `StatSheet`.

## Decisions

- **Format: JSON everywhere, parsed with Newtonsoft.Json.** Newtonsoft is Unity's standard package; pin a 13.0.x version matching Unity's `com.unity.nuget.newtonsoft-json`, so the game and the sim parse identically. Tables are arrays of rows (`[{ "level": 1, "baseBudget": 10 }, ...]`), ready for a future editor. CSV never reaches the loader; spreadsheet round-trips, if wanted, are CLI import/export tools (milestone 5).
- **Editing:** files in `data/` are the source of truth. A custom editor (a Unity editor window or a standalone tool) reads and writes the same JSON; until one exists, JSON Schema files give autocomplete and checking.
- **The loader reads text, with a folder helper on top.** The core loader takes file contents (strings or `TextReader`s), so Unity and Godot can pass text from their own asset systems. A small helper reads a folder from disk for Sim and tests.
- **Load errors are collected.** Loading returns either `GameData` or every problem found (file, row or field, message), so one pass fixes a big data change.
- **Item ids are readable keys** (`"ring_of_might"`), unique across the data. Item *instance* GUIDs wait for loot, and will come from the seeded rng (rule 3).
- **Item stats are whole numbers,** rounded when the item's stats are generated. They're what players see and compare, so they're final output under rule 6, and the tooltip and combat agree.
- **Class base stats and level growth are in milestone 3.**
- **Weapons and damage spread wait for milestone 4,** with damage specs and abilities. Milestone 3 items give stats only.

## Writes

- `GameData` (a Core type), built once and never changed.
- Load errors: file, location and message for each problem.

## Rules

- **Parsing and validation only.** No formulas here: the budget formula, conversions and combat stay in Core.
- **Names in files, ids in memory.** Stats, slots, items and classes are referenced by name in files; unknown names are load errors, never silent zeros.
- **Every load is validated.** Bad data fails at load with a clear message, never mid-fight.
- **Deterministic.** The same files give the same `GameData`. Anything iterated (items, tables) is ordered by id, not by file or dictionary order.
- May do file IO (unlike Core); the loader itself works on text, not paths.
- Tests load from strings built in the test (`tests/CONTEXT.md`: tests never read `data/`). Checking that the real `data/` folder loads cleanly belongs to a Sim CLI validation command (milestone 5), unless that rule gets an exception.

## Done when (milestone 3)

Cases for milestone 3 in `tests/CONTEXT.md` pass (not yet written), including: the item budget spec's worked example, every validation rule rejecting bad data with a useful message, and the same files loading to the same `GameData` every time.
