namespace StatLab.Core.Items;

// An equipment slot from data, e.g. "chest". Slots are data, not an enum: no formula depends on a specific slot.
public readonly record struct SlotId(string Value);
