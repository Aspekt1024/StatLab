using StatLab.Core.Stats;

namespace StatLab.Core.Rulesets;

// A complete set of formulas, swapped whole (AGENTS.md rule 5). Milestone 2 adds combat formulas.
public interface IRuleset
{
    // Returns what the final primary stats contribute to one derived stat (e.g. MaxHealth: Stamina × 10).
    // Read primaries only. Return 0 for stats with no conversion. The caller adds the stat's base.
    double Convert(StatId derivedStat, StatBlock finalPrimaries);
}
