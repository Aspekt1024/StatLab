using StatLab.Core.Rulesets;

namespace StatLab.Core.Stats;

// A character's stats: base values and modifiers in, cached final values out.
// Every change recalculates all final values, so reads are a single array lookup.
public sealed class StatSheet
{
    private readonly StatBlock _bases;
    private readonly ModifierSet _modifiers = new();
    private readonly IRuleset _ruleset;
    private StatBlock _final;

    public StatSheet(StatBlock bases, IRuleset ruleset)
    {
        _bases = bases.Clone();
        _ruleset = ruleset;
        _final = StatCalculator.Calculate(_bases, _modifiers, _ruleset);
    }

    // Final value, after modifiers and conversions.
    public double this[StatId stat] => _final[stat];

    public void SetBase(StatId stat, double value)
    {
        _bases[stat] = value;
        Recalculate();
    }

    public void AddSource(SourceId source, ReadOnlySpan<Modifier> modifiers)
    {
        _modifiers.Add(source, modifiers);
        Recalculate();
    }

    public bool RemoveSource(SourceId source)
    {
        if (!_modifiers.Remove(source))
        {
            return false;
        }

        Recalculate();
        return true;
    }

    private void Recalculate()
    {
        _final = StatCalculator.Calculate(_bases, _modifiers, _ruleset);
    }
}
