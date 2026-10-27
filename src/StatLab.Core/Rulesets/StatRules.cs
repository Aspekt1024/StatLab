using StatLab.Core.Stats;

namespace StatLab.Core.Rulesets;

// A ruleset's data-driven stat maths: each derived stat is the sum of its sources (primary × rate), and every
// stat has optional limits. The shape (a weighted sum, a clamp) is code; the mapping and numbers are data
// (rule 4). Non-linear formulas such as armour stay in the ruleset's code.
public sealed class StatRules
{
    private readonly StatSource[][] _sources = new StatSource[StatIds.Count][];
    private readonly double[] _min = new double[StatIds.Count];
    private readonly double[] _max = new double[StatIds.Count];

    // Throws if Validate finds any problem.
    public StatRules(IReadOnlyList<StatRule> rules)
    {
        List<string> problems = Validate(rules);
        if (problems.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", problems), nameof(rules));
        }

        foreach (StatRule rule in rules)
        {
            var index = (int)rule.Stat;
            _sources[index] = rule.Sources.ToArray();
            _min[index] = rule.Min ?? double.NegativeInfinity;
            _max[index] = rule.Max ?? double.PositiveInfinity;
        }
    }

    // Every problem with these rules, so a loader can report them all at once. Empty means valid.
    public static List<string> Validate(IReadOnlyList<StatRule> rules)
    {
        List<string> problems = [];
        var listed = new bool[StatIds.Count];

        foreach (StatRule rule in rules)
        {
            if (listed[(int)rule.Stat])
            {
                problems.Add($"{rule.Stat} is listed more than once.");
            }

            listed[(int)rule.Stat] = true;

            if (rule.Min > rule.Max)
            {
                problems.Add($"{rule.Stat} has min {rule.Min} above max {rule.Max}.");
            }

            if (StatIds.IsPrimary(rule.Stat) && rule.Sources.Count > 0)
            {
                problems.Add($"{rule.Stat} is a primary stat and can't have sources.");
            }

            var used = new bool[StatIds.Count];
            foreach (StatSource source in rule.Sources)
            {
                if (!StatIds.IsPrimary(source.Stat))
                {
                    problems.Add($"{rule.Stat} can't come from {source.Stat}: sources must be primary stats.");
                }

                if (used[(int)source.Stat])
                {
                    problems.Add($"{rule.Stat} lists {source.Stat} as a source more than once.");
                }

                used[(int)source.Stat] = true;
            }
        }

        for (var i = 0; i < StatIds.Count; i++)
        {
            if (!listed[i])
            {
                problems.Add($"{(StatId)i} is missing: every stat must be listed.");
            }
        }

        return problems;
    }

    // Sum of the stat's sources. 0 for stats with none.
    public double Convert(StatId derivedStat, StatBlock finalPrimaries)
    {
        var total = 0.0;
        foreach (StatSource source in _sources[(int)derivedStat])
        {
            total += finalPrimaries[source.Stat] * source.Rate;
        }

        return total;
    }

    public double Clamp(StatId stat, double value)
    {
        return Math.Clamp(value, _min[(int)stat], _max[(int)stat]);
    }
}
