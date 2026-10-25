using StatLab.Core.Rulesets.Vertical;

namespace StatLab.Core.Tests.Rulesets;

// A VerticalRuleset with round numbers, so the arithmetic in each test is easy to check by hand.
internal static class TestVerticalRuleset
{
    public static readonly VerticalRulesetConfig Config = new(
        HealthPerStamina: 10,
        ManaPerIntellect: 15,
        AttackPowerPerStrength: 2,
        CritChancePerAgility: 0.01,
        MaxCritChance: 0.75,
        BasicAttackCoefficient: 1,
        CritMultiplier: 2,
        BaseMissChance: 0.05,
        MissChancePerLevel: 0.01,
        ArmorPerAttackerLevel: 50,
        ArmorConstant: 100);

    public static readonly VerticalRuleset Instance = new(Config);
}
