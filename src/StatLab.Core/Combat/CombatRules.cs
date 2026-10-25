namespace StatLab.Core.Combat;

// The only way combat state changes: Apply(state, command) → events. Changes state in place.
// Invalid commands are rejected with an event and leave state (including the rng) untouched.
// Each accepted command passes the turn to the next combatant still standing.
public static class CombatRules
{
    public static IReadOnlyList<CombatEvent> Apply(CombatState state, Command command)
    {
        return command switch
        {
            AttackCommand attack => ApplyAttack(state, attack),
            _ => throw new ArgumentException($"Unknown command type {command.GetType().Name}.", nameof(command)),
        };
    }

    private static IReadOnlyList<CombatEvent> ApplyAttack(CombatState state, AttackCommand command)
    {
        Combatant? attacker = state.Find(command.Attacker);
        Combatant? target = state.Find(command.Target);
        if (attacker == null || target == null)
        {
            return [new CommandRejected(command, RejectionReason.UnknownCombatant)];
        }

        if (state.IsOver)
        {
            return [new CommandRejected(command, RejectionReason.FightOver)];
        }

        // A defeated attacker is never the current actor, so this also covers attackers who are down.
        if (attacker.Id != state.CurrentActor)
        {
            return [new CommandRejected(command, RejectionReason.NotYourTurn)];
        }

        if (target.IsDefeated)
        {
            return [new CommandRejected(command, RejectionReason.CombatantDefeated)];
        }

        AttackOutcome outcome = state.Ruleset.ResolveDamage(attacker, target, state.Ruleset.BasicAttack, state.Rng);
        target.Health = Math.Max(target.Health - outcome.Damage, 0);
        state.Step++;
        state.AdvanceTurn();

        List<CombatEvent> events =
        [
            new AttackResolved(attacker.Id, target.Id, outcome.Result, outcome.Damage, target.Health),
        ];
        if (target.IsDefeated)
        {
            events.Add(new CombatantDefeated(target.Id));
        }

        return events;
    }
}
