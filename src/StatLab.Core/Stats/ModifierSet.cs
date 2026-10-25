using StatLab.Core.Hashing;

namespace StatLab.Core.Stats;

// Holds each source's modifiers and keeps them grouped by stat for StatPipeline.
// Groups are rebuilt from scratch on every change and sorted by content (type, value, then source),
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

    public ModifierSet Clone()
    {
        ModifierSet clone = new();
        clone.CopyFrom(this);
        return clone;
    }

    // Overwrites this set with other's sources and groups. The Modifier[] arrays are shared, not copied:
    // they're never changed after creation (Add copies its input; Rebuild makes new arrays).
    public void CopyFrom(ModifierSet other)
    {
        if (ReferenceEquals(other, this))
        {
            return;
        }

        _bySource.Clear();
        foreach (KeyValuePair<SourceId, Modifier[]> entry in other._bySource)
        {
            _bySource.Add(entry.Key, entry.Value);
        }

        Array.Copy(other._byStat, _byStat, _byStat.Length);
    }

    // Hashes the sorted groups (their order is fixed by content and source), plus the set of sources,
    // which also covers sources added with no modifiers. Sources are combined by summing, so dictionary
    // order doesn't matter.
    internal void AddToChecksum(Checksum checksum)
    {
        foreach (Modifier[] modifiers in _byStat)
        {
            checksum.Add(modifiers.Length);
            foreach (Modifier modifier in modifiers)
            {
                checksum.Add((int)modifier.Stat);
                checksum.Add((int)modifier.Type);
                checksum.Add(modifier.Value);
                checksum.Add(modifier.Source.Value);
            }
        }

        ulong sources = 0;
        foreach (SourceId source in _bySource.Keys)
        {
            sources += Checksum.Of((ulong)source.Value);
        }

        checksum.Add(_bySource.Count);
        checksum.Add(sources);
    }

    private void Rebuild()
    {
        List<Modifier>[] grouped = new List<Modifier>[StatIds.Count];
        for (var i = 0; i < StatIds.Count; i++)
        {
            grouped[i] = [];
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

    // Type, then value, then source. The source tie-break doesn't change any total (tied modifiers have
    // equal values), but it fixes the order completely, so equal sets are identical and hash the same.
    private static int CompareByContent(Modifier a, Modifier b)
    {
        var byType = a.Type.CompareTo(b.Type);
        if (byType != 0)
        {
            return byType;
        }

        var byValue = a.Value.CompareTo(b.Value);
        return byValue != 0 ? byValue : a.Source.Value.CompareTo(b.Source.Value);
    }
}
