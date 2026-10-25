using StatLab.Core.Stats;

namespace StatLab.Core.Items;

// How much of an item's budget goes to one stat, as a fraction: 0.6 = 60%. An item's shares sum to 1.
public readonly record struct StatShare(StatId Stat, double Share);
