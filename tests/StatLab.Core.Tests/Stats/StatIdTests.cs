using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

public class StatIdTests
{
    // StatBlock uses (int)StatId as an array index, so values must be 0..n-1 with no gaps.
    [Fact]
    public void StatId_values_are_contiguous_from_zero()
    {
        StatId[] values = Enum.GetValues<StatId>();

        for (var i = 0; i < values.Length; i++)
        {
            Assert.Equal(i, (int)values[i]);
        }
    }

    // IsPrimary throws for an unclassified stat, so this fails when a new StatId isn't added there.
    [Fact]
    public void Every_stat_is_classified_as_primary_or_derived()
    {
        foreach (StatId stat in Enum.GetValues<StatId>())
        {
            StatIds.IsPrimary(stat); // Throws if stat is not defined
        }
    }
    
    // A StatId can only exist in either the PrimaryStats array, or the DerivedStats array, but never both
    [Fact]
    public void No_primary_StatId_is_also_defined_as_derived()
    {
        foreach (StatId derivedStat in StatIds.DerivedStats)
        {
            Assert.DoesNotContain(derivedStat, StatIds.PrimaryStats);
        }
    }
}
