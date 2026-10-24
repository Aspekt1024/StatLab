namespace StatLab.Core.Combat;

// What one attack did. Damage is after mitigation, never negative, and unrounded (rule 6); 0 on a miss.
// The ruleset only reports it; Core applies it to the defender's health.
public readonly record struct AttackOutcome(AttackResult Result, double Damage);
