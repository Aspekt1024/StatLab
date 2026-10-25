namespace StatLab.Core.Combat;

// Something that happened, returned by CombatRules.Apply in the order it happened (rule 8).
public abstract record CombatEvent;

public sealed record AttackResolved(
    CombatantId Attacker,
    CombatantId Target,
    AttackResult Result,
    double Damage,
    double TargetHealthAfter) : CombatEvent;

public sealed record CombatantDefeated(CombatantId Combatant) : CombatEvent;

// The command was invalid; state is unchanged.
public sealed record CommandRejected(Command Command, RejectionReason Reason) : CombatEvent;

public enum RejectionReason
{
    UnknownCombatant,
    CombatantDefeated,
}
