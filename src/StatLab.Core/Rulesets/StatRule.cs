using StatLab.Core.Stats;

namespace StatLab.Core.Rulesets;

// One primary stat feeding a derived stat: contributes finalPrimaries[Stat] × Rate.
public readonly record struct StatSource(StatId Stat, double Rate);

// A stat's entry in a ruleset's data: optional limits, and for derived stats, the primaries it comes from.
// Null Min or Max means no limit on that side.
public sealed record StatRule(StatId Stat, double? Min, double? Max, IReadOnlyList<StatSource> Sources);
