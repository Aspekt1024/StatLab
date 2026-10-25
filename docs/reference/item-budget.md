# Item budget

<!-- Layer 3: reference. Used from milestone 3. -->

Items are hand-designed, but their stat values are generated, so retuning a weight updates every item automatically.

## Authoring

Each item is authored with:

- slot
- item level
- power modifier (default 1.0; above 1.0 for standout items such as set pieces or boss drops)
- stat shares that sum to 100% (e.g. 60% Strength, 40% Stamina)

Raw stat numbers are never authored.

## Calculation

```
budget    = baseBudget(itemLevel) × slotMultiplier × powerModifier
statValue = round(share × budget ÷ statWeight)
```

`baseBudget` comes from the level curve data; `slotMultiplier` and `statWeight` come from data tables.

Each stat is rounded to a whole number when the item's stats are generated. Item stats are what players see and compare, so they count as final output (AGENTS.md rule 6), and the tooltip and combat use the same number.

## Example

Item level 20 chest: base budget 40, slot multiplier 1.0, power modifier 1.0, authored as 60% Strength (weight 1.0) / 40% Stamina (weight 0.67).

- Strength = 0.6 × 40 ÷ 1.0 = 24
- Stamina = 0.4 × 40 ÷ 0.67 ≈ 23.88, rounded to 24

## Validation (at load time)

- Stat shares sum to 100%.
- Every referenced stat, slot and item level exists in the data tables.
