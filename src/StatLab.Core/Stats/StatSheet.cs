using StatLab.Core.Rulesets;

namespace StatLab.Core.Stats;

// A character's stats: base values and modifiers in, cached final values out.
// Every change recalculates all final values, so reads are a single array lookup.
public sealed class StatSheet
{
    private readonly StatBlock _bases;
    private readonly ModifierSet _modifiers = new();
    private IRuleset _ruleset;
    private StatBlock _final;

    public StatSheet(StatBlock bases, IRuleset ruleset)
    {
        _bases = bases.Clone();
        _ruleset = ruleset;
        _final = StatCalculator.Calculate(_bases, _modifiers, _ruleset);
    }

    // Final value, after modifiers and conversions.
    public double this[StatId stat] => _final[stat];

    // Lets CombatState check every combatant uses its ruleset (rule 5).
    internal IRuleset Ruleset => _ruleset;

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

    public StatSheet Clone()
    {
        StatSheet clone = new(_bases, _ruleset);
        clone.CopyFrom(this);
        return clone;
    }

    // Overwrites this sheet with other's bases, modifiers and final values, without allocating.
    // The ruleset is shared, not copied: rulesets are stateless.
    public void CopyFrom(StatSheet other)
    {
        _bases.CopyFrom(other._bases);
        _modifiers.CopyFrom(other._modifiers);
        _final.CopyFrom(other._final);
        _ruleset = other._ruleset;
    }

    private void Recalculate()
    {
        _final = StatCalculator.Calculate(_bases, _modifiers, _ruleset);
    }
}
