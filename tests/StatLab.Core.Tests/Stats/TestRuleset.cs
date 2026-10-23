using StatLab.Core.Rulesets;
using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

// MaxHealth = Stamina × 10, CritChance = Agility × 0.01. Every other derived stat converts to 0.
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
}
