using StardewValley.Menus;

namespace SMAPIMod;

public struct TodoEntry
{
    public string task { get; }
    public bool isDone { get; set; }
    public ClickableTextureComponent removeButton { get; }
}