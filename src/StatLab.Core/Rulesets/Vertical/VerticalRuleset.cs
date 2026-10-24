using StatLab.Core.Combat;
using StatLab.Core.Random;
using StatLab.Core.Stats;

namespace StatLab.Core.Rulesets.Vertical;

// Vertical scaling: numbers grow with level and gear; armour and miss chance scale with level.
// Attacks use a single roll table (miss, then crit, then hit) and AttackPower-based damage with no spread.
public sealed class VerticalRuleset : IRuleset
{
    private readonly VerticalRulesetConfig _config;

    public VerticalRuleset(VerticalRulesetConfig config)
    {
        _config = config;
    }

    public string Id => "vertical";

    public double Convert(StatId derivedStat, StatBlock finalPrimaries)
    {
        return derivedStat switch
        {
            StatId.MaxHealth => finalPrimaries[StatId.Stamina] * _config.HealthPerStamina,
            StatId.MaxMana => finalPrimaries[StatId.Intellect] * _config.ManaPerIntellect,
            StatId.AttackPower => finalPrimaries[StatId.Strength] * _config.AttackPowerPerStrength,
            StatId.CritChance => finalPrimaries[StatId.Agility] * _config.CritChancePerAgility,
            _ => 0,
        };
    }

    // The floors are structural, not tuning: no negative stats, and nothing alive has 0 max health.
    public double Clamp(StatId stat, double value)
    {
        return stat switch
        {
            StatId.MaxHealth => Math.Max(value, 1),
            StatId.CritChance => Math.Clamp(value, 0, _config.MaxCritChance),
            _ => Math.Max(value, 0),
        };
    }

    public AttackOutcome ResolveAttack(Combatant attacker, Combatant defender, DeterministicRng rng)
    {
        var miss = MissChance(attacker.Level, defender.Level);
        AttackResult result = RollResult(rng.NextDouble(), miss, attacker[StatId.CritChance]);
        if (result == AttackResult.Miss)
        {
            return new AttackOutcome(AttackResult.Miss, 0);
        }

        var damage = attacker[StatId.AttackPower] * _config.DamagePerAttackPower;
        if (result == AttackResult.Critical)
        {
            damage *= _config.CritMultiplier;
        }

        damage *= 1 - ArmorReduction(defender[StatId.Armor], attacker.Level);
        return new AttackOutcome(result, Math.Max(damage, 0));
    }

    // Base chance, plus a step for each level the defender is above the attacker. Never below base.
    internal double MissChance(int attackerLevel, int defenderLevel)
    {
        var levelsAbove = Math.Max(defenderLevel - attackerLevel, 0);
        return Math.Clamp(_config.BaseMissChance + levelsAbove * _config.MissChancePerLevel, 0, 1);
    }

    // One roll against stacked ranges: [0, miss) misses, [miss, miss + crit) crits, the rest hits.
    // If miss + crit exceed 1, crit is squeezed into what's left.
    internal static AttackResult RollResult(double roll, double missChance, double critChance)
    {
        if (roll < missChance)
        {
            return AttackResult.Miss;
        }

        var crit = Math.Min(critChance, 1 - missChance);
        return roll < missChance + crit ? AttackResult.Critical : AttackResult.Hit;
    }

    // Diminishing returns: approaches but never reaches 1. The same armour counts for less against
    // higher-level attackers.
    internal double ArmorReduction(double armor, int attackerLevel)
    {
        if (armor <= 0)
        {
            return 0;
        }

        return armor / (armor + _config.ArmorPerAttackerLevel * attackerLevel + _config.ArmorConstant);
    }
}
