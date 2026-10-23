using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

public class StatCalculatorTests
{
    private const double Tolerance = 1e-9;
    private static readonly SourceId Sword = new(1);
    private static readonly TestRuleset Ruleset = TestRuleset.Instance;

    [Fact]
    public void Primary_stat_is_its_base_plus_its_modifiers()
    {
        StatBlock bases = new() { [StatId.Strength] = 10 };
        ModifierSet modifiers = new();
        modifiers.Add(Sword, [new Modifier(StatId.Strength, ModifierType.Flat, 5, Sword)]);

        StatBlock final = StatCalculator.Calculate(bases, modifiers, Ruleset);

        // 10 + 5 = 15
        Assert.Equal(15, final[StatId.Strength], Tolerance);
    }

    [Fact]
    public void Stamina_modifier_flows_through_to_max_health()
    {
        StatBlock bases = new() { [StatId.Stamina] = 10 };
        ModifierSet modifiers = new();
        modifiers.Add(Sword, [new Modifier(StatId.Stamina, ModifierType.Flat, 5, Sword)]);

        StatBlock final = StatCalculator.Calculate(bases, modifiers, Ruleset);

        // Stamina 10 + 5 = 15; MaxHealth 15 × 10 = 150
        Assert.Equal(150, final[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Derived_stat_adds_its_base_to_the_conversion()
    {
        StatBlock bases = new()
        {
            [StatId.Stamina] = 10,
            [StatId.MaxHealth] = 100,
        };

        StatBlock final = StatCalculator.Calculate(bases, new ModifierSet(), Ruleset);

        // 100 base + 10 × 10 = 200
        Assert.Equal(200, final[StatId.MaxHealth], Tolerance);
    }

    // A mob with no primaries: health is set directly as a base.
    [Fact]
    public void Derived_stat_with_no_primaries_is_its_base()
    {
        StatBlock bases = new() { [StatId.MaxHealth] = 500 };

        StatBlock final = StatCalculator.Calculate(bases, new ModifierSet(), Ruleset);

        // 500 base + 0 × 10 = 500
        Assert.Equal(500, final[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Modifier_on_a_derived_stat_applies_after_conversion()
    {
        StatBlock bases = new() { [StatId.Agility] = 10 };
        ModifierSet modifiers = new();
        modifiers.Add(Sword, [new Modifier(StatId.CritChance, ModifierType.Flat, 0.01, Sword)]);

        StatBlock final = StatCalculator.Calculate(bases, modifiers, Ruleset);

        // Agility 10 × 0.01 = 0.10; + 0.01 crit = 0.11
        Assert.Equal(0.11, final[StatId.CritChance], Tolerance);
    }

    [Fact]
    public void Crit_above_the_cap_is_clamped_to_the_cap()
    {
        StatBlock bases = new() { [StatId.Agility] = 100 };

        StatBlock final = StatCalculator.Calculate(bases, new ModifierSet(), Ruleset);

        // 100 × 0.01 = 1.0; capped at 0.75
        Assert.Equal(0.75, final[StatId.CritChance], Tolerance);
    }

    [Fact]
    public void Crit_within_the_cap_is_unchanged()
    {
        StatBlock bases = new() { [StatId.Agility] = 50 };

        StatBlock final = StatCalculator.Calculate(bases, new ModifierSet(), Ruleset);

        // 50 × 0.01 = 0.5; below the 0.75 cap
        Assert.Equal(0.5, final[StatId.CritChance], Tolerance);
    }

    [Fact]
    public void Max_health_is_clamped_to_at_least_one()
    {
        StatBlock bases = new() { [StatId.Stamina] = 10 };
        ModifierSet modifiers = new();
        modifiers.Add(Sword, [new Modifier(StatId.MaxHealth, ModifierType.Flat, -500, Sword)]);

        StatBlock final = StatCalculator.Calculate(bases, modifiers, Ruleset);

        // 10 × 10 - 500 = -400; clamped to 1
        Assert.Equal(1, final[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Negative_primary_is_clamped_before_conversion()
    {
        StatBlock bases = new()
        {
            [StatId.Stamina] = 10,
            [StatId.MaxHealth] = 50,
        };
        ModifierSet modifiers = new();
        modifiers.Add(Sword, [new Modifier(StatId.Stamina, ModifierType.Flat, -30, Sword)]);

        StatBlock final = StatCalculator.Calculate(bases, modifiers, Ruleset);

        // Stamina 10 - 30 = -20; clamped to 0. MaxHealth 50 + 0 × 10 = 50, not 50 - 200
        Assert.Equal(50, final[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Percent_modifier_on_a_derived_stat_scales_base_and_conversion()
    {
        StatBlock bases = new()
        {
            [StatId.Stamina] = 10,
            [StatId.MaxHealth] = 50,
        };
        ModifierSet modifiers = new();
        modifiers.Add(Sword, [new Modifier(StatId.MaxHealth, ModifierType.PercentAdd, 0.1, Sword)]);

        StatBlock final = StatCalculator.Calculate(bases, modifiers, Ruleset);

        // (50 + 10 × 10) × 1.1 = 165
        Assert.Equal(165, final[StatId.MaxHealth], Tolerance);
    }
}
