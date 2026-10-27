using StatLab.Core.Rulesets;
using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Rulesets;

public class StatRulesTests
{
    private const double Tolerance = 1e-9;

    // Every stat listed with no limits and no sources, except the ones given.
    private static List<StatRule> AllStats(params StatRule[] overrides)
    {
        List<StatRule> rules = [];
        foreach (StatId stat in Enum.GetValues<StatId>())
        {
            StatRule? custom = overrides.FirstOrDefault(rule => rule.Stat == stat);
            rules.Add(custom ?? new StatRule(stat, null, null, []));
        }

        return rules;
    }

    private static StatBlock Primaries()
    {
        return new StatBlock { [StatId.Strength] = 20, [StatId.Stamina] = 10 };
    }

    [Fact]
    public void Derived_stat_is_the_sum_of_its_sources()
    {
        StatRules rules = new(AllStats(
            new StatRule(StatId.MaxHealth, null, null, [new(StatId.Stamina, 10), new(StatId.Strength, 2)])));

        // Stamina 10 × 10 + Strength 20 × 2 = 140
        Assert.Equal(140, rules.Convert(StatId.MaxHealth, Primaries()), Tolerance);
    }

    [Fact]
    public void Stat_with_no_sources_converts_to_zero()
    {
        StatRules rules = new(AllStats());

        Assert.Equal(0, rules.Convert(StatId.Armor, Primaries()), Tolerance);
    }

    [Fact]
    public void Limits_clamp_both_ways()
    {
        StatRules rules = new(AllStats(new StatRule(StatId.CritChance, 0, 0.75, [])));

        Assert.Equal(0.75, rules.Clamp(StatId.CritChance, 0.9), Tolerance);
        Assert.Equal(0, rules.Clamp(StatId.CritChance, -0.1), Tolerance);
    }

    [Fact]
    public void Stat_with_no_limits_is_unchanged()
    {
        StatRules rules = new(AllStats());

        Assert.Equal(-500, rules.Clamp(StatId.Armor, -500), Tolerance);
    }

    [Fact]
    public void Valid_rules_have_no_problems()
    {
        Assert.Empty(StatRules.Validate(AllStats()));
    }

    [Fact]
    public void Missing_stat_is_a_problem()
    {
        List<StatRule> rules = AllStats();
        rules.RemoveAll(rule => rule.Stat == StatId.Armor);

        var problem = Assert.Single(StatRules.Validate(rules));
        Assert.Contains("Armor", problem);
    }

    [Fact]
    public void Stat_listed_twice_is_a_problem()
    {
        List<StatRule> rules = AllStats();
        rules.Add(new StatRule(StatId.Armor, null, null, []));

        Assert.Single(StatRules.Validate(rules));
    }

    [Fact]
    public void Min_above_max_is_a_problem()
    {
        Assert.Single(StatRules.Validate(AllStats(new StatRule(StatId.CritChance, 1, 0.5, []))));
    }

    [Fact]
    public void Primary_stat_with_sources_is_a_problem()
    {
        Assert.Single(StatRules.Validate(AllStats(
            new StatRule(StatId.Strength, null, null, [new(StatId.Stamina, 1)]))));
    }

    // Derived-to-derived chains aren't supported yet: they'd need an ordering and a cycle check.
    [Fact]
    public void Derived_stat_as_a_source_is_a_problem()
    {
        Assert.Single(StatRules.Validate(AllStats(
            new StatRule(StatId.CritChance, null, null, [new(StatId.AttackPower, 0.01)]))));
    }

    [Fact]
    public void Same_source_twice_is_a_problem()
    {
        Assert.Single(StatRules.Validate(AllStats(
            new StatRule(StatId.MaxHealth, null, null, [new(StatId.Stamina, 10), new(StatId.Stamina, 5)]))));
    }

    // A loader reports everything in one pass.
    [Fact]
    public void Every_problem_is_reported_at_once()
    {
        List<StatRule> rules = AllStats(
            new StatRule(StatId.CritChance, 1, 0.5, []),
            new StatRule(StatId.Strength, null, null, [new(StatId.Stamina, 1)]));
        rules.RemoveAll(rule => rule.Stat == StatId.Armor);

        Assert.Equal(3, StatRules.Validate(rules).Count);
    }

    [Fact]
    public void Constructor_throws_for_invalid_rules()
    {
        List<StatRule> rules = AllStats();
        rules.RemoveAll(rule => rule.Stat == StatId.Armor);

        Assert.Throws<ArgumentException>(() => new StatRules(rules));
    }
}
