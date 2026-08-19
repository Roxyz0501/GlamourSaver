namespace GlamourSaver.Models;

public sealed record CoordinateItem(int SlotIndex, string SlotName, uint ItemId, string ItemName, string DatabaseUrl);

public sealed record CoordinateSnapshot(string CharacterName, IReadOnlyList<CoordinateItem> Items);

public readonly record struct ScreenRegion(int X, int Y, int Width, int Height)
{
    public bool IsValid => Width > 16 && Height > 16;
}
