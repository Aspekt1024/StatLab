namespace StatLab.Core.Items;

// A hand-designed item, as authored (docs/reference/item-budget.md). Raw stat numbers are never authored:
// ItemBudget generates them from the budget tables. PowerModifier is 1.0 by default, above 1.0 for standout items.
public sealed record ItemDefinition(
    ItemId Id,
    SlotId Slot,
    int ItemLevel,
    double PowerModifier,
    IReadOnlyList<StatShare> Shares);
