using StatLab.Core.Progression;
using StatLab.Core.Stats;
using StatLab.Core.Tests.Stats;

namespace StatLab.Core.Tests.Progression;

public class ClassDefinitionTests
{
    private const double Tolerance = 1e-9;
    private static readonly ClassId Warrior = new("warrior");

    // Level 2 jumps more than level 1 → 2 would suggest, to show rows are independent.
    private static ClassDefinition WarriorClass()
    {
        return new ClassDefinition(Warrior, new Dictionary<int, StatBlock>
        {
            [1] = new() { [StatId.Strength] = 10, [StatId.Stamina] = 8 },
            [2] = new() { [StatId.Strength] = 15, [StatId.Stamina] = 9, [StatId.MaxHealth] = 50 },
        });
    }

    [Fact]
    public void Base_stats_are_that_level_row()
    {
        StatBlock stats = WarriorClass().BaseStats(1);

        Assert.Equal(10, stats[StatId.Strength], Tolerance);
        Assert.Equal(8, stats[StatId.Stamina], Tolerance);
    }

    [Fact]
    public void Each_level_is_its_own_row()
    {
        StatBlock stats = WarriorClass().BaseStats(2);

        Assert.Equal(15, stats[StatId.Strength], Tolerance);
        Assert.Equal(9, stats[StatId.Stamina], Tolerance);
    }

    [Fact]
    public void Missing_level_throws()
    {
        Assert.Throws<ArgumentException>(() => WarriorClass().BaseStats(3));
    }

    // A row can set derived bases too; they add to conversions like any base.
    [Fact]
    public void Derived_base_adds_to_the_conversion()
    {
        StatSheet sheet = new(WarriorClass().BaseStats(2), TestRuleset.Instance);

        // 50 base + Stamina 9 × 10 = 140
        Assert.Equal(140, sheet[StatId.MaxHealth], Tolerance);
    }

    [Fact]
    public void Changing_returned_stats_does_not_change_the_class()
    {
        ClassDefinition warrior = WarriorClass();

        warrior.BaseStats(1)[StatId.Strength] = 99;

        Assert.Equal(10, warrior.BaseStats(1)[StatId.Strength], Tolerance);
    }

    [Fact]
    public void Changing_the_source_rows_after_creation_changes_nothing()
    {
        StatBlock level1 = new() { [StatId.Strength] = 10 };
        ClassDefinition warrior = new(Warrior, new Dictionary<int, StatBlock> { [1] = level1 });

        level1[StatId.Strength] = 99;

        Assert.Equal(10, warrior.BaseStats(1)[StatId.Strength], Tolerance);
    }
}
