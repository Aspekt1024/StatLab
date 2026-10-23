using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

public class StatPipelineTests
{
    private const double Tolerance = 1e-9;
    private static readonly SourceId Source = new(1);

    [Fact]
    public void Base_value_with_no_modifiers_returns_the_base()
    {
        var result = StatPipeline.Calculate(10, []);

        Assert.Equal(10, result, Tolerance);
    }

    [Fact]
    public void Zero_base_with_no_modifiers_returns_zero()
    {
        var result = StatPipeline.Calculate(0, []);

        Assert.Equal(0, result, Tolerance);
    }

    [Fact]
    public void Flat_modifiers_add()
    {
        Modifier first = new(StatId.Strength, ModifierType.Flat, 5, Source);
        Modifier second = new(StatId.Strength, ModifierType.Flat, 3, Source);

        var result = StatPipeline.Calculate(10, [first, second]);

        // 10 + 5 + 3 = 18
        Assert.Equal(18, result, Tolerance);
    }

    [Fact]
    public void Negative_modifiers_reduce_the_value()
    {
        Modifier flat = new(StatId.Armor, ModifierType.Flat, -20, Source);
        Modifier percent = new(StatId.Armor, ModifierType.PercentAdd, -0.1, Source);

        var result = StatPipeline.Calculate(100, [flat, percent]);

        // (100 - 20) × (1 - 0.1) = 72
        Assert.Equal(72, result, Tolerance);
    }

    [Fact]
    public void Flat_modifier_applies_before_percentages()
    {
        Modifier flat = new(StatId.Strength, ModifierType.Flat, 5, Source);
        Modifier percent = new(StatId.Strength, ModifierType.PercentAdd, 0.5, Source);

        var result = StatPipeline.Calculate(10, [flat, percent]);

        // (10 + 5) × (1 + 0.5) = 22.5
        Assert.Equal(22.5, result, Tolerance);
    }

    [Fact]
    public void PercentAdd_modifiers_sum_before_multiplying()
    {
        Modifier first = new(StatId.Strength, ModifierType.PercentAdd, 0.1, Source);
        Modifier second = new(StatId.Strength, ModifierType.PercentAdd, 0.1, Source);

        var result = StatPipeline.Calculate(100, [first, second]);

        // 100 × (1 + 0.1 + 0.1) = 120
        Assert.Equal(120, result, Tolerance);
    }

    [Fact]
    public void PercentMult_modifiers_compound()
    {
        Modifier first = new(StatId.Strength, ModifierType.PercentMult, 0.1, Source);
        Modifier second = new(StatId.Strength, ModifierType.PercentMult, 0.1, Source);

        var result = StatPipeline.Calculate(100, [first, second]);

        // 100 × 1.1 × 1.1 = 121
        Assert.Equal(121, result, Tolerance);
    }
}
