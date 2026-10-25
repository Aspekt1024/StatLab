using StatLab.Core.Combat;
using StatLab.Core.Rulesets;
using StatLab.Core.Stats;
using StatLab.Core.Tests.Rulesets;
using StatLab.Core.Tests.Stats;

namespace StatLab.Core.Tests.Combat;

public class CombatRulesTests
{
    private const double Tolerance = 1e-9;
    private const ulong Seed = 7;
    private static readonly CombatantId Warrior = new(1);
    private static readonly CombatantId Goblin = new(2);
    private static readonly CombatantId Grunt = new(3);
    private static readonly CombatantId Nobody = new(99);

    private sealed record UnknownCommand : Command;

    // TestRuleset always hits for AttackPower, so damage is exact. Stamina 10 gives 100 health.
    private static CombatState ExactDuel(double warriorAttackPower)
    {
        IRuleset ruleset = TestRuleset.Instance;
        Combatant warrior = new(Warrior, Team.Player, 1, new StatSheet(
            new StatBlock { [StatId.Stamina] = 10, [StatId.AttackPower] = warriorAttackPower }, ruleset));
        Combatant goblin = new(Goblin, Team.Enemy, 1, new StatSheet(new StatBlock { [StatId.Stamina] = 10 }, ruleset));
        return new CombatState(ruleset, Seed, [warrior, goblin]);
    }

    // One warrior (kills in one hit) against two enemies, in turn order warrior, goblin, grunt.
    private static CombatState Skirmish()
    {
        IRuleset ruleset = TestRuleset.Instance;
        Combatant warrior = new(Warrior, Team.Player, 1, new StatSheet(
            new StatBlock { [StatId.Stamina] = 10, [StatId.AttackPower] = 150 }, ruleset));
        Combatant goblin = new(Goblin, Team.Enemy, 1, new StatSheet(
            new StatBlock { [StatId.Stamina] = 10, [StatId.AttackPower] = 10 }, ruleset));
        Combatant grunt = new(Grunt, Team.Enemy, 1, new StatSheet(
            new StatBlock { [StatId.Stamina] = 10, [StatId.AttackPower] = 10 }, ruleset));
        return new CombatState(ruleset, Seed, [warrior, goblin, grunt]);
    }

    // The vertical ruleset rolls for miss and crit, so outcomes depend on the rng.
    private static CombatState RolledDuel()
    {
        IRuleset ruleset = TestVerticalRuleset.Instance;
        Combatant warrior = new(Warrior, Team.Player, 1, new StatSheet(
            new StatBlock { [StatId.Strength] = 20, [StatId.Agility] = 20, [StatId.Stamina] = 30 }, ruleset));
        Combatant goblin = new(Goblin, Team.Enemy, 1, new StatSheet(
            new StatBlock { [StatId.Strength] = 15, [StatId.Stamina] = 25, [StatId.Armor] = 50 }, ruleset));
        return new CombatState(ruleset, Seed, [warrior, goblin]);
    }

    private static List<CombatEvent> TradeBlows(CombatState state, int rounds)
    {
        List<CombatEvent> events = [];
        for (var i = 0; i < rounds; i++)
        {
            events.AddRange(CombatRules.Apply(state, new AttackCommand(Warrior, Goblin)));
            events.AddRange(CombatRules.Apply(state, new AttackCommand(Goblin, Warrior)));
        }

        return events;
    }

    [Fact]
    public void Attack_reduces_target_health_by_the_damage()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30);

        IReadOnlyList<CombatEvent> events = CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        // 100 - 30 = 70
        CombatEvent[] expected = [new AttackResolved(Warrior, Goblin, AttackResult.Hit, 30, 70)];
        Assert.Equal(expected, events);
        Assert.Equal(70, state.Find(Goblin)!.Health, Tolerance);
    }

    [Fact]
    public void Health_does_not_go_below_zero()
    {
        CombatState state = ExactDuel(warriorAttackPower: 150);

        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        // 100 - 150 = -50; stops at 0
        Assert.Equal(0, state.Find(Goblin)!.Health, Tolerance);
    }

    [Fact]
    public void Defeating_a_target_emits_CombatantDefeated_after_the_attack()
    {
        CombatState state = ExactDuel(warriorAttackPower: 150);

        IReadOnlyList<CombatEvent> events = CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        CombatEvent[] expected =
        [
            new AttackResolved(Warrior, Goblin, AttackResult.Hit, 150, 0),
            new CombatantDefeated(Goblin),
        ];
        Assert.Equal(expected, events);
    }

    [Fact]
    public void Accepted_command_advances_the_step()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30);

        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        Assert.Equal(1, state.Step);
    }

    [Fact]
    public void Attack_involving_an_unknown_combatant_is_rejected_and_changes_nothing()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30);
        AttackCommand command = new(Nobody, Goblin);

        IReadOnlyList<CombatEvent> events = CombatRules.Apply(state, command);

        CombatEvent[] expected = [new CommandRejected(command, RejectionReason.UnknownCombatant)];
        Assert.Equal(expected, events);
        Assert.Equal(100, state.Find(Goblin)!.Health, Tolerance);
        Assert.Equal(0, state.Step);
    }

    [Fact]
    public void First_listed_combatant_acts_first()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30);

        Assert.Equal(Warrior, state.CurrentActor);
    }

    [Fact]
    public void Accepted_command_passes_the_turn_to_the_next_combatant()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30);

        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        Assert.Equal(Goblin, state.CurrentActor);
    }

    [Fact]
    public void Command_out_of_turn_is_rejected_and_changes_nothing()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30);
        AttackCommand command = new(Goblin, Warrior);

        IReadOnlyList<CombatEvent> events = CombatRules.Apply(state, command);

        CombatEvent[] expected = [new CommandRejected(command, RejectionReason.NotYourTurn)];
        Assert.Equal(expected, events);
        Assert.Equal(100, state.Find(Warrior)!.Health, Tolerance);
        Assert.Equal(Warrior, state.CurrentActor);
    }

    [Fact]
    public void Defeated_combatants_are_skipped_in_turn_order()
    {
        CombatState state = Skirmish();

        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        // Turn order is warrior, goblin, grunt; the goblin is down, so the grunt is next
        Assert.Equal(Grunt, state.CurrentActor);
    }

    [Fact]
    public void Attack_on_a_defeated_target_is_rejected()
    {
        CombatState state = Skirmish();
        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));
        CombatRules.Apply(state, new AttackCommand(Grunt, Warrior));
        AttackCommand command = new(Warrior, Goblin);

        IReadOnlyList<CombatEvent> events = CombatRules.Apply(state, command);

        CombatEvent[] expected = [new CommandRejected(command, RejectionReason.CombatantDefeated)];
        Assert.Equal(expected, events);
        Assert.Equal(2, state.Step);
    }

    [Fact]
    public void Fight_is_not_over_while_both_teams_have_someone_standing()
    {
        CombatState state = Skirmish();

        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        Assert.False(state.IsOver);
        Assert.Null(state.Winner);
    }

    [Fact]
    public void Fight_is_over_when_one_team_has_no_one_standing()
    {
        CombatState state = ExactDuel(warriorAttackPower: 150);

        CombatRules.Apply(state, new AttackCommand(Warrior, Goblin));

        Assert.True(state.IsOver);
        Assert.Equal(Team.Player, state.Winner);
        Assert.Null(state.CurrentActor);
    }

    [Fact]
    public void Commands_after_the_fight_is_over_are_rejected()
    {
        CombatState state = ExactDuel(warriorAttackPower: 150);
        AttackCommand command = new(Warrior, Goblin);
        CombatRules.Apply(state, command);

        IReadOnlyList<CombatEvent> events = CombatRules.Apply(state, command);

        CombatEvent[] expected = [new CommandRejected(command, RejectionReason.FightOver)];
        Assert.Equal(expected, events);
        Assert.Equal(1, state.Step);
    }

    [Fact]
    public void Unknown_command_type_throws()
    {
        CombatState state = ExactDuel(warriorAttackPower: 30);

        Assert.Throws<ArgumentException>(() => CombatRules.Apply(state, new UnknownCommand()));
    }

    // Determinism (rule 3): compared exactly, not with a tolerance, because the promise is identical bits.
    [Fact]
    public void Same_seed_and_commands_produce_identical_events()
    {
        List<CombatEvent> first = TradeBlows(RolledDuel(), rounds: 10);
        List<CombatEvent> second = TradeBlows(RolledDuel(), rounds: 10);

        Assert.Equal(first, second);
    }

    // Rollback: restoring a snapshot must restore the rng too, or the replay rolls differently.
    [Fact]
    public void Restoring_a_snapshot_replays_the_same_events()
    {
        CombatState state = RolledDuel();
        CombatState snapshot = state.Clone();
        List<CombatEvent> original = TradeBlows(state, rounds: 5);

        state.CopyFrom(snapshot);
        List<CombatEvent> replayed = TradeBlows(state, rounds: 5);

        Assert.Equal(original, replayed);
    }
}
