namespace OneDesk.Models;

public class Item(string name, string path, Item? parentFolder)
{
    public string Name => name;

    public string Path => path;

    public Item? ParentFolder => parentFolder;
}