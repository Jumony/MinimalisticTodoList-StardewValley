using StardewValley.Menus;

namespace MinimalisticTodoList;

public struct TodoEntry
{
    public string task { get; }
    public bool isDone { get; set; }
    public ClickableTextureComponent removeButton { get; }
}