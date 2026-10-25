using StatLab.Core.Rulesets;
using StatLab.Core.Stats;

namespace StatLab.Core.Combat;

// One participant in a fight. Readable by anyone; changed only by Core (internal setters), through Apply.
public sealed class Combatant
{
    private readonly StatSheet _stats;

    // Copies the sheet, so the caller can't change this combatant's stats afterwards. Starts at full health.
    public Combatant(CombatantId id, Team team, int level, StatSheet stats)
    {
        Id = id;
        Team = team;
        Level = level;
        _stats = stats.Clone();
        Health = _stats[StatId.MaxHealth];
    }

    public CombatantId Id { get; private set; }

    public Team Team { get; private set; }

    public int Level { get; internal set; }

    // Current health: a resource, not a stat.
    public double Health { get; internal set; }

    public bool IsDefeated => Health <= 0;

    // Final stat value, after modifiers and conversions.
    public double this[StatId stat] => _stats[stat];

    internal IRuleset Ruleset => _stats.Ruleset;

    public Combatant Clone()
    {
        Combatant clone = new(Id, Team, Level, _stats);
        clone.Health = Health;
        return clone;
    }

    // Overwrites this combatant with other's id, team, level, health and stats, without allocating.
    public void CopyFrom(Combatant other)
    {
        Id = other.Id;
        Team = other.Team;
        Level = other.Level;
        Health = other.Health;
        _stats.CopyFrom(other._stats);
    }
}
