using Microsoft.Graph.Models;

namespace OneDesk.Models;

public class Item(string name, string path, Item? parentFolder, DriveItem? driveItem) : IEquatable<Item>
{
    public Item(string name, string path, Item? parentFolder) : this(name, path, parentFolder, null)
    {
    }

    public string Name => name;

    public string Path => path;

    public Item? ParentFolder => parentFolder;

    public DriveItem? DriveItem => driveItem;

    public bool Equals(Item? other)
    {
        if (other is null) return false;

        return Name == other.Name && Path == other.Path && DriveItem?.Id == other.DriveItem?.Id;
    }
    public override bool Equals(object? obj) => Equals(obj as Item);
    public override int GetHashCode() => HashCode.Combine(Name, Path, DriveItem?.Id);

    public static bool operator ==(Item? left, Item? right) => Equals(left, right);
    public static bool operator !=(Item? left, Item? right) => !Equals(left, right);
}