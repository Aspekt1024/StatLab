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
}
