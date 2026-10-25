using StatLab.Core.Combat;
using StatLab.Core.Random;
using StatLab.Core.Stats;

namespace StatLab.Core.Rulesets;

// A complete set of formulas, swapped whole (AGENTS.md rule 5). Stateless: tuning numbers come from an
// immutable config passed to the constructor. Contract: docs/reference/rulesets.md.
public interface IRuleset
{
    // Stable name, e.g. "vertical". Recorded in every results CSV and names the data folder data/<id>/.
    string Id { get; }

    // Returns what the final primary stats contribute to one derived stat (e.g. MaxHealth: Stamina × 10).
    // Read primaries only. Return 0 for stats with no conversion. The caller adds the stat's base.
    double Convert(StatId derivedStat, StatBlock finalPrimaries);

    // Applies this ruleset's limits to a stat's final value (e.g. crit cap, MaxHealth ≥ 1).
    // Called for every stat; return the value unchanged if it has no limits.
    double Clamp(StatId stat, double value);

    // The basic attack's damage, built from this ruleset's config.
    DamageSpec BasicAttack { get; }

    // Resolves one piece of damage: whether it lands, whether it crits, and its amount after mitigation.
    // Draw randomness only from rng. Don't change either combatant; Core applies the outcome.
    AttackOutcome ResolveDamage(Combatant attacker, Combatant defender, DamageSpec spec, DeterministicRng rng);
}
