using StatLab.Core.Stats;

namespace StatLab.Core.Items;

// The tuning tables behind ItemBudget, loaded from data: base budget per item level, a multiplier per slot,
// and a weight per stat (how much budget one point of the stat costs). Copies its inputs, so it can't change.
// StatLab.Data validates the values (weights and multipliers above 0); a missing entry throws here.
public sealed class ItemBudgetTables
{
    private readonly Dictionary<int, double> _baseBudgetByLevel;
    private readonly Dictionary<SlotId, double> _slotMultipliers;
    private readonly Dictionary<StatId, double> _statWeights;

    public ItemBudgetTables(
        IReadOnlyDictionary<int, double> baseBudgetByLevel,
        IReadOnlyDictionary<SlotId, double> slotMultipliers,
        IReadOnlyDictionary<StatId, double> statWeights)
    {
        _baseBudgetByLevel = new Dictionary<int, double>(baseBudgetByLevel);
        _slotMultipliers = new Dictionary<SlotId, double>(slotMultipliers);
        _statWeights = new Dictionary<StatId, double>(statWeights);
    }

    public double BaseBudget(int itemLevel)
    {
        if (!_baseBudgetByLevel.TryGetValue(itemLevel, out var budget))
        {
            throw new ArgumentException($"No base budget for item level {itemLevel}.", nameof(itemLevel));
        }

        return budget;
    }

    public double SlotMultiplier(SlotId slot)
    {
        if (!_slotMultipliers.TryGetValue(slot, out var multiplier))
        {
            throw new ArgumentException($"No multiplier for slot '{slot.Value}'.", nameof(slot));
        }

        return multiplier;
    }

    public double StatWeight(StatId stat)
    {
        if (!_statWeights.TryGetValue(stat, out var weight))
        {
            throw new ArgumentException($"No weight for stat {stat}.", nameof(stat));
        }

        return weight;
    }
}
