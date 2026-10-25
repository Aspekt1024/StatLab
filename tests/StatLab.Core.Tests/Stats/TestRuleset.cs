using StatLab.Core.Combat;
using StatLab.Core.Random;
using StatLab.Core.Rulesets;
using StatLab.Core.Stats;

namespace StatLab.Core.Tests.Stats;

// Conversions: MaxHealth = Stamina × 10, CritChance = Agility × 0.01. Every other derived stat converts to 0.
// Limits: primaries ≥ 0, MaxHealth ≥ 1, CritChance between 0 and 0.75.
// Damage: always hits for attacker[ScalesWith] × Coefficient, with no randomness, so combat tests are exact.
// The basic attack is AttackPower × 1.
internal sealed class TestRuleset : IRuleset
{
    public static readonly TestRuleset Instance = new();

    public string Id => "test";

    public DamageSpec BasicAttack => new(StatId.AttackPower, 1, CanMiss: false, CanCrit: false);

    public AttackOutcome ResolveDamage(Combatant attacker, Combatant defender, DamageSpec spec, DeterministicRng rng)
    {
        return new AttackOutcome(AttackResult.Hit, attacker[spec.ScalesWith] * spec.Coefficient);
    }

    public double Convert(StatId derivedStat, StatBlock finalPrimaries)
    {
        return derivedStat switch
        {
            StatId.MaxHealth => finalPrimaries[StatId.Stamina] * 10,
            StatId.CritChance => finalPrimaries[StatId.Agility] * 0.01,
            _ => 0,
        };
    }

    public double Clamp(StatId stat, double value)
    {
        if (StatIds.IsPrimary(stat))
        {
            return Math.Max(value, 0);
        }

        return stat switch
        {
            StatId.MaxHealth => Math.Max(value, 1),
            StatId.CritChance => Math.Clamp(value, 0, 0.75),
            _ => value,
        };
    }
}
