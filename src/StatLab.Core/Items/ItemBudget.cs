using StatLab.Core.Stats;

namespace StatLab.Core.Items;

// Turns an authored item into its stats (docs/reference/item-budget.md):
// budget = baseBudget(itemLevel) × slotMultiplier × powerModifier
// stat   = round(share × budget ÷ statWeight), x.5 rounded away from zero
public static class ItemBudget
{
    // One Flat modifier per share, in the item's share order. Sources are left at their default:
    // equipping stamps them with a fresh SourceId (docs/reference/stat-pipeline.md, Sources).
    public static Modifier[] Calculate(ItemDefinition item, ItemBudgetTables tables)
    {
        var budget = tables.BaseBudget(item.ItemLevel) * tables.SlotMultiplier(item.Slot) * item.PowerModifier;

        Modifier[] modifiers = new Modifier[item.Shares.Count];
        for (var i = 0; i < modifiers.Length; i++)
        {
            StatShare share = item.Shares[i];
            var value = Math.Round(share.Share * budget / tables.StatWeight(share.Stat), MidpointRounding.AwayFromZero);
            modifiers[i] = new Modifier(share.Stat, ModifierType.Flat, value, default);
        }

        return modifiers;
    }
}
