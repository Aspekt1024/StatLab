using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

public class StatSheetTests
{
    private const double Tolerance = 1e-9;
    private static readonly SourceId Sword = new(1);

    [Fact]
    public void Final_values_are_calculated_on_creation()
    {
        StatBlock bases = new() { [StatId.Stamina] = 10 };

        StatSheet sheet = new(bases, TestRuleset.Instance);

        // 10 × 10 = 100
        Assert.Equal(100, sheet[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Adding_a_source_updates_final_values()
    {
        StatSheet sheet = new(new StatBlock { [StatId.Stamina] = 10 }, TestRuleset.Instance);

        sheet.AddSource(Sword, [new Modifier(StatId.Stamina, ModifierType.Flat, 5, Sword)]);

        // (10 + 5) × 10 = 150
        Assert.Equal(150, sheet[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Removing_a_source_restores_final_values()
    {
        StatSheet sheet = new(new StatBlock { [StatId.Stamina] = 10 }, TestRuleset.Instance);
        sheet.AddSource(Sword, [new Modifier(StatId.Stamina, ModifierType.Flat, 5, Sword)]);

        sheet.RemoveSource(Sword);

        // 10 × 10 = 100
        Assert.Equal(100, sheet[StatId.MaxHealth], Tolerance);
    }

    // e.g. a level-up raising base Stamina.
    [Fact]
    public void Setting_a_base_updates_derived_values()
    {
        StatSheet sheet = new(new StatBlock { [StatId.Stamina] = 10 }, TestRuleset.Instance);

        sheet.SetBase(StatId.Stamina, 12);

        // 12 × 10 = 120
        Assert.Equal(120, sheet[StatId.MaxHealth], Tolerance);
    }

    // The sheet copies its bases, so the cache can't go stale behind its back.
    [Fact]
    public void Changing_the_original_bases_does_not_change_the_sheet()
    {
        StatBlock bases = new() { [StatId.Stamina] = 10 };
        StatSheet sheet = new(bases, TestRuleset.Instance);

        bases[StatId.Stamina] = 99;

        // Still 10 × 10 = 100
        Assert.Equal(100, sheet[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Changing_a_clone_does_not_change_the_original()
    {
        StatSheet original = new(new StatBlock { [StatId.Stamina] = 10 }, TestRuleset.Instance);
        StatSheet clone = original.Clone();

        clone.AddSource(Sword, [new Modifier(StatId.Stamina, ModifierType.Flat, 5, Sword)]);
        clone.SetBase(StatId.Stamina, 20);

        // Still 10 × 10 = 100
        Assert.Equal(100, original[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void CopyFrom_restores_an_earlier_snapshot()
    {
        StatSheet sheet = new(new StatBlock { [StatId.Stamina] = 10 }, TestRuleset.Instance);
        StatSheet snapshot = sheet.Clone();
        sheet.AddSource(Sword, [new Modifier(StatId.Stamina, ModifierType.Flat, 5, Sword)]);

        sheet.CopyFrom(snapshot);

        // Back to 10 × 10 = 100, and the sword is gone
        Assert.Equal(100, sheet[StatId.MaxHealth], Tolerance);
        Assert.False(sheet.RemoveSource(Sword));
    }

    [Fact]
    public void Changing_the_source_after_CopyFrom_does_not_change_the_target()
    {
        StatSheet source = new(new StatBlock { [StatId.Stamina] = 10 }, TestRuleset.Instance);
        StatSheet target = new(new StatBlock(), TestRuleset.Instance);
        target.CopyFrom(source);

        source.AddSource(Sword, [new Modifier(StatId.Stamina, ModifierType.Flat, 5, Sword)]);

        // Still 10 × 10 = 100
        Assert.Equal(100, target[StatId.MaxHealth], Tolerance);
    }
}
