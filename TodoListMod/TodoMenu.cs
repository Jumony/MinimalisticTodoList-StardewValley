using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace SMAPIMod;

public class TodoMenu : IClickableMenu
{
    private readonly List<string> _todos = new();
    private TextBox _inputBox;
 
    private const int Padding = 20;
    private const int TitleHeight = 40;
    private const int ItemHeight = 28;
    private const int InputAreaHeight = 50;

    private const int exitButtonWidth = 32;
    private const int exitButtonHeight = 32;

    private ClickableTextureComponent exitButton;
    
    private int scrollOffset = 0; 
 
    public TodoMenu() : base(0, 0, 420, 500)
    {
        xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
        yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;
 
        // Use Game1.content.Load textures so TextBox doesn't try to draw
        // its own broken background from a null spritesheet
        _inputBox = new TextBox(
            Game1.content.Load<Texture2D>("LooseSprites\\textBox"),
            null,
            Game1.smallFont,
            Game1.textColor)
        {
            X = xPositionOnScreen + Padding - 4,
            // Push Y up enough that the texture renders fully inside the menu box
            Y = yPositionOnScreen + height - InputAreaHeight + 4,
            Width = width - Padding * 2,
            Selected = true
        };
 
        // Exit Button
        exitButton = new ClickableTextureComponent("exit",
            new Rectangle(xPositionOnScreen + width, yPositionOnScreen, exitButtonWidth, exitButtonHeight),
            null,
            "Exit",
            Game1.mouseCursors,
            new Rectangle(338, 494, 12, 12), 4f);
        
        Game1.keyboardDispatcher.Subscriber = _inputBox;
    }
 
    public override void draw(SpriteBatch b)
    {
        // Dim background
        b.Draw(Game1.fadeToBlackRect,
            Game1.graphics.GraphicsDevice.Viewport.Bounds,
            Color.Black * 0.5f);
 
        // Menu background
        IClickableMenu.drawTextureBox(
            b, xPositionOnScreen, yPositionOnScreen,
            width, height, Color.White);
 
        // Title
        string title = "To-Do List";
        Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
        b.DrawString(
            Game1.dialogueFont, title,
            new Vector2(xPositionOnScreen + (width - titleSize.X) / 2, yPositionOnScreen + Padding),
            Color.SaddleBrown);
 
        // Divider below title
        int dividerY = yPositionOnScreen + TitleHeight + Padding;
        b.Draw(Game1.staminaRect,
            new Rectangle(xPositionOnScreen + Padding, dividerY, width - Padding * 2, 2),
            Color.SaddleBrown * 0.4f);

        // Draw Exit Button
        exitButton.draw(b);
        
        // Todo items
        int itemY = dividerY + 10;
        if (_todos.Count == 0)
        {
            b.DrawString(Game1.smallFont, "Nothing here yet...",
                new Vector2(xPositionOnScreen + Padding, itemY),
                Color.Gray);
        }
        else
        {
            foreach (string todo in _todos)
            {
                b.Draw(Game1.staminaRect,
                    new Rectangle(xPositionOnScreen + Padding, itemY + 8, 6, 6),
                    Color.SaddleBrown);
 
                string display = TruncateToFit(todo, width - Padding * 2 - 16);
                b.DrawString(Game1.smallFont, display,
                    new Vector2(xPositionOnScreen + Padding + 16, itemY),
                    Game1.textColor);
 
                itemY += ItemHeight;
 
                if (itemY > yPositionOnScreen + height - InputAreaHeight - 20)
                {
                    b.DrawString(Game1.smallFont, "...",
                        new Vector2(xPositionOnScreen + Padding + 16, itemY),
                        Color.Gray);
                    break;
                }
            }
        }
 
        // Divider above input
        int inputDividerY = yPositionOnScreen + height - InputAreaHeight - 8;
        b.Draw(Game1.staminaRect,
            new Rectangle(xPositionOnScreen + Padding, inputDividerY, width - Padding * 2, 2),
            Color.SaddleBrown * 0.4f);
 
        // Placeholder hint
        if (string.IsNullOrEmpty(_inputBox.Text))
        {
            b.DrawString(Game1.smallFont, "Type a task, press Enter...",
                new Vector2(_inputBox.X + 8, _inputBox.Y + 8),
                Color.Gray);
        }
 
        _inputBox.Draw(b);
 
        base.draw(b);
        drawMouse(b);
    }
 
    public override void receiveKeyPress(Keys key)
    {
        // When the input box is active, swallow EVERYTHING except Enter and Escape
        // so game hotkeys (E, I, M, F, etc.) don't close or interact with the menu
        if (_inputBox.Selected)
        {
            if (key == Keys.Enter)
            {
                string text = _inputBox.Text?.Trim();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _todos.Add(text);
                    _inputBox.Text = "";
                }
            }
            else if (key == Keys.Escape)
            {
                exitThisMenu();
            }
            // All other keys: do nothing, TextBox handles them via keyboardDispatcher
            return;
        }
 
        if (key == Keys.Escape)
        {
            exitThisMenu();
            return;
        }
 
        base.receiveKeyPress(key);
    }

    public override void receiveScrollWheelAction(int direction)
    {
        // Scroll the todo list when mouse wheel is used
        int maxVisibleItems = (height - TitleHeight - InputAreaHeight - Padding * 3) / ItemHeight;
        int maxScroll = Math.Max(0, _todos.Count - maxVisibleItems);
        scrollOffset = Math.Clamp(scrollOffset + direction, 0, maxScroll);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (exitButton.containsPoint(x, y))
        {
            exitThisMenu(); 
        }
    }
    
    protected override void cleanupBeforeExit()
    {
        if (Game1.keyboardDispatcher.Subscriber == _inputBox)
            Game1.keyboardDispatcher.Subscriber = null;
 
        base.cleanupBeforeExit();
    }
 
    public override void emergencyShutDown()
    {
        if (Game1.keyboardDispatcher.Subscriber == _inputBox)
            Game1.keyboardDispatcher.Subscriber = null;
 
        base.emergencyShutDown();
    }
 
    private string TruncateToFit(string text, int maxWidth)
    {
        if (Game1.smallFont.MeasureString(text).X <= maxWidth)
            return text;
 
        while (text.Length > 0 && Game1.smallFont.MeasureString(text + "...").X > maxWidth)
            text = text[..^1]; // Gettin' fancy wit it
 
        return text + "...";
    }
}
 
