namespace StatLab.Core.Items;

// A readable item definition key from data, e.g. "ring_of_might". Identifies the kind of item, not one copy.
public readonly record struct ItemId(string Value);
