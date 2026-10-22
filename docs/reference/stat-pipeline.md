# Stat pipeline

<!-- Layer 3: reference. The single source of truth for how stats are calculated. -->

## Stats

- **Primary stats**: Strength, Agility, Intellect, Stamina, etc. From class base values, level growth and gear.
- **Derived stats**: max health, attack power, crit chance, armor mitigation, etc. Calculated from primaries; these are what combat uses.

`StatId` is an enum used as an array index. `StatBlock` holds base values in an array indexed by `StatId`.

`StatId` values are implicit: declaration order sets the array index, so values are contiguous from 0.

- Values are runtime indices only and may change when stats are added or reordered. Never persist or transmit them.
- Anything persisted (data files, Unity assets, saves, replays) stores the stat by name and converts to `StatId` on load. Renaming a stat is therefore a breaking change to persisted data.
- Primary vs derived is not inferred from value ranges.

## Modifiers

A modifier changes one stat, from one source:

| Field | Meaning |
| --- | --- |
| `Stat` | The `StatId` it changes |
| `Type` | `Flat`, `PercentAdd` or `PercentMult` |
| `Value` | `12` for flat, `0.05` for 5% |
| `Source` | The item, buff or talent that produced it |

Gear, buffs, talents and crafted items all produce modifiers; the pipeline does not care where they came from. Removing a source removes all of its modifiers.

## Order of operations (fixed)

```
final = (base + Σ Flat) × (1 + Σ PercentAdd) × Π(1 + each PercentMult)
```

then caps and clamping.

Example: base 10, +5 flat, +50% → (10 + 5) × 1.5 = 22.5.

- **PercentAdd** sums: two +10% give +20%. Use for common sources.
- **PercentMult** compounds: two +10% give +21%. Use sparingly; stacks grow fast.

## Derived stats

1. Run every primary stat through the pipeline.
2. Convert primaries to derived stats using the active ruleset's conversion rules.
3. Run each derived stat through the pipeline, so modifiers like "+1% crit" apply.

Conversion rules belong to the ruleset, not the pipeline.

## Rounding and caching

- Calculate in floating point; round only for display or final integer damage.
- Recalculating on every query is fine for now. A dirty flag is a later optimization; don't build it yet.

## Changing this spec

The order of operations changes only together with its tests in `StatLab.Core.Tests`.
