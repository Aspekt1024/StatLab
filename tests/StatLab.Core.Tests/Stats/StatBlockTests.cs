using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

public class StatBlockTests
{
    private const double Tolerance = 1e-9;

    [Fact]
    public void New_block_has_every_stat_at_zero()
    {
        StatBlock block = new();

        foreach (StatId stat in Enum.GetValues<StatId>())
        {
            Assert.Equal(0, block[stat], Tolerance);
        }
    }

    [Fact]
    public void Set_value_is_returned_by_get()
    {
        const double value = 12.5;
        StatBlock block = new()
        {
            [StatId.Strength] = value
        };

        Assert.Equal(value, block[StatId.Strength], Tolerance);
    }

    [Fact]
    public void Setting_one_stat_leaves_others_unchanged()
    {
        StatBlock block = new()
        {
            [StatId.Strength] = 10
        };

        foreach (StatId stat in Enum.GetValues<StatId>())
        {
            if (stat == StatId.Strength) continue;
            Assert.Equal(0, block[stat], Tolerance);
        }
    }

    [Fact]
    public void Clone_has_the_same_values()
    {
        StatBlock original = new()
        {
            [StatId.Strength] = 10,
            [StatId.Armor] = 250,
        };

        StatBlock clone = original.Clone();

        Assert.Equal(10, clone[StatId.Strength], Tolerance);
        Assert.Equal(250, clone[StatId.Armor], Tolerance);
    }

    // Guards against Clone() sharing the original's array.
    [Fact]
    public void Changing_a_clone_does_not_change_the_original()
    {
        StatBlock original = new() { [StatId.Strength] = 10 };
        StatBlock clone = original.Clone();

        clone[StatId.Strength] = 99;

        Assert.Equal(10, original[StatId.Strength], Tolerance);
    }
}
