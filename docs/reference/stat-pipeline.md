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
| `Value` | `12` for flat, `0.05` for 5%; negative for debuffs |
| `Source` | `SourceId` of the item instance, buff or talent that produced it |

Gear, buffs, talents and crafted items all produce modifiers; the pipeline does not care where they came from. Removing a source removes all of its modifiers.

`Modifier` is an immutable value (`readonly record struct`). To change one, remove it and add a new one.

### Sources

`SourceId` identifies one instance (this copy of the ring, this application of the buff), not a definition. Two copies of the same ring have different `SourceId`s, so removing one leaves the other.

- `SourceId` is a runtime-only `int`, assigned by a counter in game state. Never from `Guid.NewGuid()` or a static counter (rule 3).
- Never persisted. Saves identify item instances by GUID; on load, each instance is assigned a fresh `SourceId`.
- Modifiers are not persisted with a source. An item instance stores its rolled modifiers; the caller sets the source (`modifier with { Source = id }`) when it is equipped.
- `ModifierSet.Add(source, modifiers)` throws if any modifier's `Source` differs from `source`, or if `source` was already added.

## Order of operations (fixed)

```
final = (base + Σ Flat) × (1 + Σ PercentAdd) × Π(1 + each PercentMult)
```

then caps and clamping.

Example: base 10, +5 flat, +50% → (10 + 5) × 1.5 = 22.5.

- **PercentAdd** sums: two +10% give +20%. Use for common sources.
- **PercentMult** compounds: two +10% give +21%. Use sparingly; stacks grow fast.

Within one stat, modifiers are applied in canonical order: sorted by `Type`, then `Value`. Floating-point addition is not associative, so this makes the result depend only on the set of modifiers, not on source IDs or the order they were added. Same set, same bits: required for save/load and co-op peers to agree.

## Derived stats

1. Run every primary stat through the pipeline.
2. Convert primaries to derived stats using the active ruleset's conversion rules.
3. Run each derived stat through the pipeline, so modifiers like "+1% crit" apply.

Conversion rules belong to the ruleset, not the pipeline.

- A derived stat's pipeline base is its own base value plus the conversion: `MaxHealth = base + Stamina × rate`. Mobs can set derived stats directly and have no primaries.
- `IRuleset.Convert(derivedStat, finalPrimaries)` returns one stat's conversion. All conversions are computed before any derived stat is written.
- Which stats are primary is fixed in Core (`StatIds.IsPrimary`), not by the ruleset.
- `StatCalculator.Calculate(bases, modifiers, ruleset)` runs all three phases and owns their order.

## Rounding and caching

- Calculate in floating point; round only for display or final integer damage.
- `StatSheet` recalculates every final stat on every change (base set, source added or removed) and caches the result; reads are an array lookup. Recalculating everything means there is no dependency tracking to get wrong. Don't add a dirty flag or partial updates unless profiling asks for it.

## Changing this spec

The order of operations changes only together with its tests in `StatLab.Core.Tests`.
