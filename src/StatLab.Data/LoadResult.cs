namespace StatLab.Data;

// One problem found while loading. Location is a JSON path with its line, e.g. "stats[4].min (line 12)".
public sealed record LoadError(string File, string Location, string Message)
{
    public override string ToString()
    {
        return $"{File}: {Location}: {Message}";
    }
}

// Either the loaded value, or every problem found. Never both.
public sealed class LoadResult<T>
    where T : class
{
    private LoadResult(T? value, IReadOnlyList<LoadError> errors)
    {
        Value = value;
        Errors = errors;
    }

    public T? Value { get; }

    public IReadOnlyList<LoadError> Errors { get; }

    public bool Succeeded => Value != null;

    public static LoadResult<T> Success(T value)
    {
        return new LoadResult<T>(value, []);
    }

    public static LoadResult<T> Failure(IReadOnlyList<LoadError> errors)
    {
        return new LoadResult<T>(null, errors);
    }
}
