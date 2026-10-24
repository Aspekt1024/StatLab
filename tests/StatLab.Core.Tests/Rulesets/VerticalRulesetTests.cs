using StatLab.Core.Combat;
using StatLab.Core.Random;
using StatLab.Core.Rulesets.Vertical;
using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Rulesets;

public class VerticalRulesetTests
{
    private const double Tolerance = 1e-9;

    // Round numbers so the arithmetic in each test is easy to check by hand.
    private static readonly VerticalRulesetConfig Config = new(
        HealthPerStamina: 10,
        ManaPerIntellect: 15,
        AttackPowerPerStrength: 2,
        CritChancePerAgility: 0.01,
        MaxCritChance: 0.75,
        DamagePerAttackPower: 1,
        CritMultiplier: 2,
        BaseMissChance: 0.05,
        MissChancePerLevel: 0.01,
        ArmorPerAttackerLevel: 50,
        ArmorConstant: 100);

    private static readonly VerticalRuleset Ruleset = new(Config);

    // The first roll from seed 42 is 0.0839 (pinned in DeterministicRngTests).
    private const ulong Seed = 42;

    private static Combatant MakeCombatant(int id, int level, StatBlock bases)
    {
        return new Combatant(new CombatantId(id), level, new StatSheet(bases, Ruleset));
    }

    [Theory]
    [InlineData(StatId.Stamina, 10, StatId.MaxHealth, 100)]     // 10 × 10
    [InlineData(StatId.Intellect, 10, StatId.MaxMana, 150)]     // 10 × 15
    [InlineData(StatId.Strength, 10, StatId.AttackPower, 20)]   // 10 × 2
    [InlineData(StatId.Agility, 10, StatId.CritChance, 0.1)]    // 10 × 0.01
    public void Primary_stat_converts_to_its_derived_stat(StatId primary, double value, StatId derived, double expected)
    {
        StatBlock primaries = new() { [primary] = value };

        Assert.Equal(expected, Ruleset.Convert(derived, primaries), Tolerance);
    }

    [Fact]
    public void Armor_has_no_conversion()
    {
        StatBlock primaries = new() { [StatId.Strength] = 10, [StatId.Agility] = 10 };

        Assert.Equal(0, Ruleset.Convert(StatId.Armor, primaries), Tolerance);
    }

    [Fact]
    public void Crit_chance_is_capped()
    {
        Assert.Equal(0.75, Ruleset.Clamp(StatId.CritChance, 0.9), Tolerance);
    }

    [Fact]
    public void Max_health_is_at_least_one()
    {
        Assert.Equal(1, Ruleset.Clamp(StatId.MaxHealth, -50), Tolerance);
    }

    [Fact]
    public void Other_stats_are_at_least_zero()
    {
        Assert.Equal(0, Ruleset.Clamp(StatId.Stamina, -5), Tolerance);
        Assert.Equal(0, Ruleset.Clamp(StatId.Armor, -20), Tolerance);
    }

    [Fact]
    public void Armor_reduction_has_diminishing_returns()
    {
        // 150 / (150 + 50 × 1 + 100) = 0.5
        Assert.Equal(0.5, Ruleset.ArmorReduction(150, attackerLevel: 1), Tolerance);
    }

    [Fact]
    public void Same_armor_reduces_less_against_a_higher_level_attacker()
    {
        // 150 / (150 + 50 × 5 + 100) = 0.3
        Assert.Equal(0.3, Ruleset.ArmorReduction(150, attackerLevel: 5), Tolerance);
    }

    [Fact]
    public void No_armor_means_no_reduction()
    {
        Assert.Equal(0, Ruleset.ArmorReduction(0, attackerLevel: 1), Tolerance);
    }

    [Fact]
    public void Miss_chance_grows_for_each_level_the_defender_is_above()
    {
        // 0.05 + 3 × 0.01 = 0.08
        Assert.Equal(0.08, Ruleset.MissChance(attackerLevel: 1, defenderLevel: 4), Tolerance);
    }

    [Fact]
    public void Miss_chance_never_drops_below_base_against_lower_levels()
    {
        Assert.Equal(0.05, Ruleset.MissChance(attackerLevel: 10, defenderLevel: 1), Tolerance);
    }

    // Miss 5%, crit 20%: [0, 0.05) misses, [0.05, 0.25) crits, the rest hits.
    [Theory]
    [InlineData(0.0, AttackResult.Miss)]
    [InlineData(0.04, AttackResult.Miss)]
    [InlineData(0.05, AttackResult.Critical)]
    [InlineData(0.24, AttackResult.Critical)]
    [InlineData(0.26, AttackResult.Hit)]
    [InlineData(0.99, AttackResult.Hit)]
    public void Roll_table_stacks_miss_then_crit_then_hit(double roll, AttackResult expected)
    {
        Assert.Equal(expected, VerticalRuleset.RollResult(roll, missChance: 0.05, critChance: 0.2));
    }

    [Fact]
    public void Crit_is_squeezed_when_miss_and_crit_exceed_one()
    {
        // Miss 30% + crit 80% > 100%: crit fills [0.3, 1), leaving no hits.
        Assert.Equal(AttackResult.Critical, VerticalRuleset.RollResult(0.99, missChance: 0.3, critChance: 0.8));
    }

    [Fact]
    public void Hit_deals_attack_power_reduced_by_armor()
    {
        Combatant attacker = MakeCombatant(1, 1, new StatBlock { [StatId.Strength] = 50 });
        Combatant defender = MakeCombatant(2, 1, new StatBlock { [StatId.Stamina] = 10, [StatId.Armor] = 150 });

        AttackOutcome outcome = Ruleset.ResolveAttack(attacker, defender, new DeterministicRng(Seed));

        // Roll 0.0839: not below miss 0.05, crit 0 → hit. AttackPower 50 × 2 = 100; × (1 - 0.5) = 50
        Assert.Equal(AttackResult.Hit, outcome.Result);
        Assert.Equal(50, outcome.Damage, Tolerance);
    }

    [Fact]
    public void Critical_multiplies_damage_before_armor()
    {
        Combatant attacker = MakeCombatant(1, 1, new StatBlock { [StatId.Strength] = 50, [StatId.Agility] = 10 });
        Combatant defender = MakeCombatant(2, 1, new StatBlock { [StatId.Stamina] = 10, [StatId.Armor] = 150 });

        AttackOutcome outcome = Ruleset.ResolveAttack(attacker, defender, new DeterministicRng(Seed));

        // Roll 0.0839 is in [0.05, 0.15) → crit. 100 × 2 = 200; × (1 - 0.5) = 100
        Assert.Equal(AttackResult.Critical, outcome.Result);
        Assert.Equal(100, outcome.Damage, Tolerance);
    }

    [Fact]
    public void Miss_deals_no_damage()
    {
        Combatant attacker = MakeCombatant(1, 1, new StatBlock { [StatId.Strength] = 50 });
        Combatant defender = MakeCombatant(2, 5, new StatBlock { [StatId.Stamina] = 10 });

        AttackOutcome outcome = Ruleset.ResolveAttack(attacker, defender, new DeterministicRng(Seed));

        // Miss chance 0.05 + 4 × 0.01 = 0.09; roll 0.0839 < 0.09 → miss
        Assert.Equal(AttackResult.Miss, outcome.Result);
        Assert.Equal(0, outcome.Damage, Tolerance);
    }
}
