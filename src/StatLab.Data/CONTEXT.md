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
3. **Core: `GameData`**, done. One immutable object per ruleset folder: a ready `IRuleset` (Data builds it from its config), the budget tables, items and classes. Lookups are methods (`Item`, `Class`, `TryGetItem`, `TryGetClass`) whose errors name the id; `Items` and `Classes` are read-only lists sorted by id, ordinal. Item stats are computed once at build (`ItemStats(id)`), so table errors fail at load.
3b. **Core: data-driven stat rules**, done. `StatRule`, `StatSource` and `StatRules` (`Rulesets/`): conversions and limits per stat, from data. `VerticalRulesetConfig` holds `StatRules` plus the attack and armour constants. `StatRules.Validate` returns every problem; Data reports them as load errors.
4. **Data: project and ruleset config**, done. `StatLab.Data` (Newtonsoft.Json 13.0.3) and `StatLab.Data.Tests`. `VerticalRulesetLoader.Load(file, text)` returns a `LoadResult<VerticalRulesetConfig>`: the config, or every `LoadError` (file, JSON path with line, message). `JsonFields` reads fields and records missing, mistyped and unknown ones.
   - Checks: `formatVersion` (a mismatch is the only error reported); unknown fields (`$schema` allowed); stat names match `StatId` exactly; `StatRules.Validate`, run only once every stat entry parsed, so a typo isn't also reported as "missing stat"; constants: `critMultiplier ≥ 1`, `baseMissChance` in [0, 1], `armor.constant > 0`, others ≥ 0.
   - Files: `data/vertical/ruleset.json` (starting numbers, equal to the tests' round numbers; tune freely) and `data/schemas/vertical-ruleset.schema.json`. The schema lists stat names, so **update it when a `StatId` is added or removed**; the loader stays the final authority.
   - Unity's Newtonsoft package version still needs confirming against 13.0.3 when the engine is set up; any 13.0.x has the same API.
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
- **Ruleset files group by stat** (`"stats": [...]`): each stat's limits and sources in one entry, always a `sources` array, every stat listed.
- **Every file has `"formatVersion"`.** A file in an older shape fails with "format N, expected M" instead of a list of confusing field errors.
- **Unknown fields are load errors,** so a typo can't be silently ignored.
- **Ruleset config lists every stat; content is sparse.** Items, classes and saves only mention the stats they use; unmentioned stats are 0.

## Writes

- `GameData` (a Core type), built once and never changed.
- Load errors: file, location and message for each problem.

## Rules

- **Parsing and validation only.** No formulas here: the budget formula, conversions and combat stay in Core.
- **Names in files, ids in memory.** Stats, slots, items and classes are referenced by name in files; unknown names are load errors, never silent zeros.
- **Every load is validated.** Bad data fails at load with a clear message, never mid-fight.
- **Deterministic.** The same files give the same `GameData` on every machine. Anything iterated (items, classes) is ordered by id with ordinal comparison, never by file, folder-listing or dictionary order, which differ between operating systems. Seeded picks from a list (loot) and sim output depend on it.
- May do file IO (unlike Core); the loader itself works on text, not paths.
- Tests load from strings built in the test (`tests/CONTEXT.md`: tests never read `data/`). Checking that the real `data/` folder loads cleanly belongs to a Sim CLI validation command (milestone 5), unless that rule gets an exception.

## Done when (milestone 3)

Cases for milestone 3 in `tests/CONTEXT.md` pass (not yet written), including: the item budget spec's worked example, every validation rule rejecting bad data with a useful message, and the same files loading to the same `GameData` every time.
