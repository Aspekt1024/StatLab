using StatLab.Core.Rulesets;
using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

// Conversions: MaxHealth = Stamina × 10, CritChance = Agility × 0.01. Every other derived stat converts to 0.
// Limits: primaries ≥ 0, MaxHealth ≥ 1, CritChance between 0 and 0.75.
internal sealed class TestRuleset : IRuleset
{
    public static readonly TestRuleset Instance = new();

    public double Convert(StatId derivedStat, StatBlock finalPrimaries)
    {
        return derivedStat switch
        {
            StatId.MaxHealth => finalPrimaries[StatId.Stamina] * 10,
            StatId.CritChance => finalPrimaries[StatId.Agility] * 0.01,
            _ => 0,
        };
    }

    public double Clamp(StatId stat, double value)
    {
        if (StatIds.IsPrimary(stat))
        {
            return Math.Max(value, 0);
        }

        return stat switch
        {
            StatId.MaxHealth => Math.Max(value, 1),
            StatId.CritChance => Math.Clamp(value, 0, 0.75),
            _ => value,
        };
    }
}
