using StatLab.Core.Combat;
using StatLab.Core.Rulesets;
using StatLab.Core.Stats;
using StatLab.Sim.Duels;

namespace StatLab.Sim.Tests.Duels;

public class DuelTests
{
    private const double Tolerance = 1e-9;
    private const ulong Seed = 11;
    private static readonly CombatantId Warrior = new(1);
    private static readonly CombatantId Goblin = new(2);
    private static readonly BasicAttackPolicy Attack = BasicAttackPolicy.Instance;

    // Both have 100 health (Stamina 10 × 10); the always-hit ruleset makes every attack land.
    private static CombatState ExactDuel(double warriorAttackPower, double goblinAttackPower)
    {
        IRuleset ruleset = AlwaysHitRuleset.Instance;
        Combatant warrior = new(Warrior, Team.Player, 1, new StatSheet(
            new StatBlock { [StatId.Stamina] = 10, [StatId.AttackPower] = warriorAttackPower }, ruleset));
        Combatant goblin = new(Goblin, Team.Enemy, 1, new StatSheet(
            new StatBlock { [StatId.Stamina] = 10, [StatId.AttackPower] = goblinAttackPower }, ruleset));
        return new CombatState(ruleset, Seed, [warrior, goblin]);
    }

    // Identical combatants with the vertical ruleset: real miss and crit rolls decide the winner.
    private static CombatState EvenRolledDuel()
    {
        IRuleset ruleset = TestVerticalRuleset.Instance;
        StatBlock bases = new() { [StatId.Strength] = 20, [StatId.Agility] = 10, [StatId.Stamina] = 30 };
        Combatant warrior = new(Warrior, Team.Player, 1, new StatSheet(bases, ruleset));
        Combatant goblin = new(Goblin, Team.Enemy, 1, new StatSheet(bases, ruleset));
        return new CombatState(ruleset, Seed, [warrior, goblin]);
    }

    // Always names a combatant who isn't in the fight, so every command is rejected.
    private sealed class RejectedCommandPolicy : ICombatPolicy
    {
        public Command ChooseCommand(CombatState state, CombatantId self)
        {
            return new AttackCommand(new CombatantId(99), self);
        }
    }

    [Fact]
    public void Duel_runs_until_one_team_is_defeated()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30, goblinAttackPower: 10);

        DuelResult result = Duel.Run(state, Attack, Attack);

        // Warrior hits on steps 1, 3, 5, 7: 100 → 70 → 40 → 10 → 0. The goblin gets 3 attacks in between.
        Assert.Equal(new DuelResult(Team.Player, 7), result);
    }

    [Fact]
    public void Duel_changes_the_state_it_is_given()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30, goblinAttackPower: 10);

        Duel.Run(state, Attack, Attack);

        // Goblin: 3 hits of 10 → 70
        Assert.Equal(0, state.Find(Goblin)!.Health, Tolerance);
        Assert.Equal(70, state.Find(Warrior)!.Health, Tolerance);
    }

    [Fact]
    public void Duel_that_cannot_end_stops_at_the_step_cap_as_a_draw()
    {
        CombatState state = ExactDuel(warriorAttackPower: 0, goblinAttackPower: 0);

        DuelResult result = Duel.Run(state, Attack, Attack, maxSteps: 10);

        Assert.Equal(new DuelResult(null, 10), result);
    }

    [Fact]
    public void Default_step_cap_is_100()
    {
        CombatState state = ExactDuel(warriorAttackPower: 0, goblinAttackPower: 0);

        DuelResult result = Duel.Run(state, Attack, Attack);

        Assert.Equal(new DuelResult(null, 100), result);
    }

    // The cap counts attempts, so a policy whose commands are always rejected can't loop forever.
    [Fact]
    public void Rejected_commands_still_count_towards_the_cap()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30, goblinAttackPower: 10);
        RejectedCommandPolicy rejected = new();

        DuelResult result = Duel.Run(state, rejected, rejected, maxSteps: 10);

        Assert.Equal(new DuelResult(null, 0), result);
    }

    [Fact]
    public void Basic_attack_policy_targets_the_first_opponent_standing()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30, goblinAttackPower: 10);

        Assert.Equal(new AttackCommand(Warrior, Goblin), Attack.ChooseCommand(state, Warrior));
        Assert.Equal(new AttackCommand(Goblin, Warrior), Attack.ChooseCommand(state, Goblin));
    }

    // Determinism (rule 3): compared exactly.
    [Fact]
    public void Same_seed_produces_the_same_result()
    {
        DuelResult first = Duel.Run(EvenRolledDuel(), Attack, Attack);
        DuelResult second = Duel.Run(EvenRolledDuel(), Attack, Attack);

        Assert.Equal(first, second);
    }

    // Milestone 2 "done when": an even fight with real rolls ends with a winner, inside the cap.
    [Fact]
    public void Evenly_matched_combatants_finish_within_the_cap()
    {
        DuelResult result = Duel.Run(EvenRolledDuel(), Attack, Attack);

        Assert.NotNull(result.Winner);
        Assert.InRange(result.Steps, 1, Duel.DefaultMaxSteps - 1);
    }
}
