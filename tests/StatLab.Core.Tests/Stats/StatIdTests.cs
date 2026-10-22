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
}
