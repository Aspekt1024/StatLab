using StatLab.Core.Rulesets;
using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

public class StatCalculatorTests
{
    private const double Tolerance = 1e-9;
    private static readonly SourceId Sword = new(1);

    // MaxHealth = Stamina × 10, CritChance = Agility × 0.01. Every other derived stat converts to 0.
    private sealed class TestRuleset : IRuleset
    {
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

    private static readonly TestRuleset Ruleset = new();

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
