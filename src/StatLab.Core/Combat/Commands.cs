namespace StatLab.Core.Combat;

// Something a player or AI asks to happen. Applied only through CombatRules.Apply.
public abstract record Command;

public sealed record AttackCommand(CombatantId Attacker, CombatantId Target) : Command;
