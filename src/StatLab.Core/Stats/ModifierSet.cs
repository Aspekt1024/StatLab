namespace StatLab.Core.Stats;

// Holds each source's modifiers and keeps them grouped by stat for StatPipeline.
// Groups are rebuilt from scratch on every change and sorted by content (type, then value),
// so the same set of modifiers always produces the same order. See docs/reference/stat-pipeline.md.
public sealed class ModifierSet
{
    private readonly Dictionary<SourceId, Modifier[]> _bySource = new();
    private readonly Modifier[][] _byStat = new Modifier[StatIds.Count][];

    public ModifierSet()
    {
        Rebuild();
    }

    public void Add(SourceId source, ReadOnlySpan<Modifier> modifiers)
    {
        if (_bySource.ContainsKey(source))
        {
            throw new ArgumentException($"Source {source.Value} has already been added.", nameof(source));
        }

        foreach (Modifier modifier in modifiers)
        {
            if (modifier.Source != source)
            {
                throw new ArgumentException(
                    $"Modifier from source {modifier.Source.Value} cannot be added under source {source.Value}.",
                    nameof(modifiers));
            }
        }

        _bySource.Add(source, modifiers.ToArray());
        Rebuild();
    }

    public bool Remove(SourceId source)
    {
        if (!_bySource.Remove(source))
        {
            return false;
        }

        Rebuild();
        return true;
    }

    public ReadOnlySpan<Modifier> For(StatId stat)
    {
        return _byStat[(int)stat];
    }

    private void Rebuild()
    {
        List<Modifier>[] grouped = new List<Modifier>[StatIds.Count];
        for (var i = 0; i < StatIds.Count; i++)
        {
            grouped[i] = new List<Modifier>();
        }

        foreach (Modifier[] modifiers in _bySource.Values)
        {
            foreach (Modifier modifier in modifiers)
            {
                grouped[(int)modifier.Stat].Add(modifier);
            }
        }

        for (var i = 0; i < StatIds.Count; i++)
        {
            grouped[i].Sort(CompareByContent);
            _byStat[i] = grouped[i].ToArray();
        }
    }

    private static int CompareByContent(Modifier a, Modifier b)
    {
        var byType = a.Type.CompareTo(b.Type);
        return byType != 0 ? byType : a.Value.CompareTo(b.Value);
    }
}
