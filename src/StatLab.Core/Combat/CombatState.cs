using StatLab.Core.Random;
using StatLab.Core.Rulesets;

namespace StatLab.Core.Combat;

// Everything a fight's outcome depends on: ruleset, rng, step counter and combatants.
// One Clone() or CopyFrom() captures all of it. Changed only by CombatRules.Apply.
public sealed class CombatState
{
    private readonly List<Combatant> _combatants = new();

    // Copies the combatants. Throws if any were built with a different ruleset (rule 5) or ids repeat.
    public CombatState(IRuleset ruleset, ulong seed, IEnumerable<Combatant> combatants)
    {
        Ruleset = ruleset;
        Rng = new DeterministicRng(seed);

        foreach (Combatant combatant in combatants)
        {
            if (!ReferenceEquals(combatant.Ruleset, ruleset))
            {
                throw new ArgumentException(
                    $"Combatant {combatant.Id.Value} was built with ruleset '{combatant.Ruleset.Id}', not '{ruleset.Id}'.",
                    nameof(combatants));
            }

            if (Find(combatant.Id) != null)
            {
                throw new ArgumentException($"Combatant id {combatant.Id.Value} appears twice.", nameof(combatants));
            }

            _combatants.Add(combatant.Clone());
        }
    }

    private CombatState(CombatState other)
    {
        Ruleset = other.Ruleset;
        Rng = other.Rng.Clone();
        Step = other.Step;
        foreach (Combatant combatant in other._combatants)
        {
            _combatants.Add(combatant.Clone());
        }
    }

    // Shared, never copied: rulesets are stateless.
    public IRuleset Ruleset { get; private set; }

    // Number of accepted commands so far.
    public int Step { get; internal set; }

    public IReadOnlyList<Combatant> Combatants => _combatants;

    internal DeterministicRng Rng { get; }

    public Combatant? Find(CombatantId id)
    {
        foreach (Combatant combatant in _combatants)
        {
            if (combatant.Id == id)
            {
                return combatant;
            }
        }

        return null;
    }

    public CombatState Clone()
    {
        return new CombatState(this);
    }

    // Overwrites this state with other's. Allocation-free when both have the same number of combatants,
    // as when restoring a snapshot of the same fight.
    public void CopyFrom(CombatState other)
    {
        Ruleset = other.Ruleset;
        Rng.CopyFrom(other.Rng);
        Step = other.Step;

        if (_combatants.Count == other._combatants.Count)
        {
            for (var i = 0; i < _combatants.Count; i++)
            {
                _combatants[i].CopyFrom(other._combatants[i]);
            }

            return;
        }

        _combatants.Clear();
        foreach (Combatant combatant in other._combatants)
        {
            _combatants.Add(combatant.Clone());
        }
    }
}
