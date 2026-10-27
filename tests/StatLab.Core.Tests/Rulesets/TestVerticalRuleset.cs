using StatLab.Core.Rulesets;
using StatLab.Core.Rulesets.Vertical;
using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Rulesets;

// A VerticalRuleset with round numbers, so the arithmetic in each test is easy to check by hand.
// MaxHealth = Stamina × 10, MaxMana = Intellect × 15, AttackPower = Strength × 2, CritChance = Agility × 0.01.
// Limits: every stat ≥ 0, MaxHealth ≥ 1, CritChance ≤ 0.75.
internal static class TestVerticalRuleset
{
    public static readonly StatRules Stats = new(
    [
        new(StatId.Strength, 0, null, []),
        new(StatId.Agility, 0, null, []),
        new(StatId.Intellect, 0, null, []),
        new(StatId.Stamina, 0, null, []),
        new(StatId.MaxHealth, 1, null, [new(StatId.Stamina, 10)]),
        new(StatId.MaxMana, 0, null, [new(StatId.Intellect, 15)]),
        new(StatId.AttackPower, 0, null, [new(StatId.Strength, 2)]),
        new(StatId.CritChance, 0, 0.75, [new(StatId.Agility, 0.01)]),
        new(StatId.Armor, 0, null, []),
    ]);

    public static readonly VerticalRulesetConfig Config = new(
        Stats,
        BasicAttackCoefficient: 1,
        CritMultiplier: 2,
        BaseMissChance: 0.05,
        MissChancePerLevel: 0.01,
        ArmorPerAttackerLevel: 50,
        ArmorConstant: 100);

    public static readonly VerticalRuleset Instance = new(Config);
}
