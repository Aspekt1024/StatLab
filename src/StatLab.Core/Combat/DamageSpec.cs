using StatLab.Core.Stats;

namespace StatLab.Core.Combat;

// Describes one piece of damage, separately from who deals and receives it: attacker[ScalesWith] × Coefficient.
// The basic attack is one spec (IRuleset.BasicAttack); abilities will be others, defined in data (milestone 4).
public readonly record struct DamageSpec(StatId ScalesWith, double Coefficient, bool CanMiss, bool CanCrit);
