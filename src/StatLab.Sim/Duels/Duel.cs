using StatLab.Core.Combat;

namespace StatLab.Sim.Duels;

// Runs a fight to the end by asking each team's policy for its combatants' commands. A sim tool; the game
// never runs this. Uses Core's turn order and fight-end rules, so results match real play.
public static class Duel
{
    public const int DefaultMaxSteps = 100;

    // Changes state in place; pass a clone to keep the original. maxSteps caps attempts, not accepted
    // commands, so a policy that keeps issuing rejected commands still ends.
    public static DuelResult Run(
        CombatState state,
        ICombatPolicy playerPolicy,
        ICombatPolicy enemyPolicy,
        int maxSteps = DefaultMaxSteps)
    {
        var startStep = state.Step;

        for (var attempt = 0; attempt < maxSteps && !state.IsOver; attempt++)
        {
            CombatantId actor = state.CurrentActor!.Value;
            ICombatPolicy policy = state.Find(actor)!.Team == Team.Player ? playerPolicy : enemyPolicy;
            CombatRules.Apply(state, policy.ChooseCommand(state, actor));
        }

        return new DuelResult(state.Winner, state.Step - startStep);
    }
}
