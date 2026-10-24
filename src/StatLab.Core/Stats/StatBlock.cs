namespace StatLab.Core.Stats;

// Base stat values, indexed by StatId. Knows nothing about modifiers or final values.
public sealed class StatBlock
{
    private readonly double[] _values;

    public StatBlock()
    {
        _values = new double[StatIds.Count];
    }

    private StatBlock(double[] values)
    {
        _values = values;
    }

    public double this[StatId stat]
    {
        get => _values[(int)stat];
        set => _values[(int)stat] = value;
    }

    public StatBlock Clone()
    {
        return new StatBlock((double[])_values.Clone());
    }

    // Overwrites this block's values with other's, without allocating.
    public void CopyFrom(StatBlock other)
    {
        Array.Copy(other._values, _values, _values.Length);
    }
}
