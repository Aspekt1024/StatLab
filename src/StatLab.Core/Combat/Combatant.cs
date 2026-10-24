using StatLab.Core.Stats;

namespace StatLab.Core.Combat;

// One participant in a fight. Readable by anyone; changed only by Core (internal setters), through Apply.
public sealed class Combatant
{
    private readonly StatSheet _stats;

    // Copies the sheet, so the caller can't change this combatant's stats afterwards. Starts at full health.
    public Combatant(CombatantId id, int level, StatSheet stats)
    {
        Id = id;
        Level = level;
        _stats = stats.Clone();
        Health = _stats[StatId.MaxHealth];
    }

    public CombatantId Id { get; private set; }

    public int Level { get; internal set; }

    // Current health: a resource, not a stat.
    public double Health { get; internal set; }

    // Final stat value, after modifiers and conversions.
    public double this[StatId stat] => _stats[stat];

    public Combatant Clone()
    {
        Combatant clone = new(Id, Level, _stats);
        clone.Health = Health;
        return clone;
    }

    // Overwrites this combatant with other's id, level, health and stats, without allocating.
    public void CopyFrom(Combatant other)
    {
        Id = other.Id;
        Level = other.Level;
        Health = other.Health;
        _stats.CopyFrom(other._stats);
    }
}
