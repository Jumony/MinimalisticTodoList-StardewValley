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

/*
 * Reminder to self to mention in README:
 * - Text input limit is 27 characters to prevent overflow and ensure delete button fits
 */

public class TodoMenu : IClickableMenu
{
    private readonly List<string> _todos = new();
    private readonly TextBox _inputBox;
 
    private const int Padding = 20;
    private const int TitleHeight = 40;
    private const int ItemHeight = 28;
    private const int InputAreaHeight = 50;
    private const int ItemStartPosOffset = 10;  // Small gap between title divider and first item

    private const int TextLimit = 27;
    
    private const float ExitButtonScale = 4f;
    private const float DeleteTaskButtonScale = 2f;

    private readonly ClickableTextureComponent _exitButton;
    private readonly List<ClickableTextureComponent> _deleteTaskButtons = new();
    
    private int _scrollOffset = 0; 
 
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
            Selected = true,
            textLimit = TextLimit,
        };
 
        // Exit Button
        _exitButton = new ClickableTextureComponent("exit",
            new Rectangle(xPositionOnScreen + width, yPositionOnScreen, (int)(12 * ExitButtonScale), (int)(12 * ExitButtonScale)),  // 12x12 texture * 4f scale = 48 pixels
            null,
            "Exit",
            Game1.mouseCursors,
            new Rectangle(338, 494, 12, 12), ExitButtonScale);
        RebuildDeleteButtons();
        
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
        _exitButton.draw(b);
        
        // Todo items
        int itemY = dividerY + ItemStartPosOffset;
        int maxVisibleItems = (height - TitleHeight - InputAreaHeight - Padding * 3) / ItemHeight;
        
        if (_todos.Count == 0)
        {
            b.DrawString(Game1.smallFont, "Nothing here yet...",
                new Vector2(xPositionOnScreen + Padding, itemY),
                Color.Gray);
        }
        else
        {
            for (int i = _scrollOffset; i < _todos.Count; i++)
            {
                // If the number of items goes off the box 
                if (itemY + ItemHeight > yPositionOnScreen + height - InputAreaHeight - 20)
                {
                    b.DrawString(Game1.smallFont, "...",
                        new Vector2(xPositionOnScreen + Padding + 16, itemY),
                        Color.Gray);
                    break;
                }
                
                // Bullet point
                b.Draw(Game1.staminaRect,
                    new Rectangle(xPositionOnScreen + Padding, itemY + 8, 6, 6),
                    Color.SaddleBrown);
 
                // Task string
                string display = TruncateToFit(_todos[i], width - Padding * 2 - (int)(12 * DeleteTaskButtonScale) - 16); // Account for delete button width and some spacing;
                b.DrawString(Game1.smallFont, display,
                    new Vector2(xPositionOnScreen + Padding + 16, itemY),
                    Game1.textColor);

                int buttonIndex = i - _scrollOffset;
                if (buttonIndex < _deleteTaskButtons.Count)
                {
                    _deleteTaskButtons[buttonIndex].draw(b);
                }
                
                itemY += ItemHeight;
            }

            // If player scrolls down or is at the bottom
            if (_scrollOffset > 0)
            {
                b.DrawString(Game1.smallFont, "^",
                    new Vector2(xPositionOnScreen + width - Padding - 16, dividerY + 10),
                    Color.SaddleBrown * 0.6f);
            }

            // If player is all the way at the top
            if (_scrollOffset < Math.Max(0, _todos.Count - maxVisibleItems))
            {
                b.DrawString(Game1.smallFont, "v",
                    new Vector2(xPositionOnScreen + width - Padding - 16,
                        yPositionOnScreen + height - InputAreaHeight - 30),
                    Color.SaddleBrown * 0.6f);
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
                    RebuildDeleteButtons();
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
        int maxVisibleItems = (height - TitleHeight - InputAreaHeight - Padding * 3) / ItemHeight;
        int maxScroll = Math.Max(0, _todos.Count - maxVisibleItems);

        _scrollOffset = Math.Clamp(_scrollOffset - (direction / 120), 0, maxScroll);
        // RebuildDeleteButtons();
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        if (_exitButton.containsPoint(x, y))
        {
            exitThisMenu();
            return;
        }
        
        for (int i = 0; i < _deleteTaskButtons.Count; i++)
        {
            if (_deleteTaskButtons[i].containsPoint(x, y))
            {
                int todoIndex = i + _scrollOffset;
                _todos.RemoveAt(todoIndex);
                
                int maxVisibleItems = (height - TitleHeight - InputAreaHeight - Padding * 3) / ItemHeight;
                int maxScroll = Math.Max(0, _todos.Count - maxVisibleItems);
                _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
                
                RebuildDeleteButtons();
                return;
            }
        }
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);

        float targetScale = _exitButton.containsPoint(x, y) ? 4.5f : 4f;
        _exitButton.scale = MathHelper.Lerp(_exitButton.scale, targetScale, 0.2f);
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

    private void RebuildDeleteButtons()
    {
        _deleteTaskButtons.Clear();

        int dividerY = yPositionOnScreen + TitleHeight + Padding;
        int itemY = dividerY + ItemStartPosOffset;

        for (int i = _scrollOffset; i < _todos.Count; i++)
        {
            // Don't build buttons for items that go off the menu
            if (itemY + ItemHeight > yPositionOnScreen + height - InputAreaHeight - 20)
            {
                break;
            }
            
            _deleteTaskButtons.Add(new ClickableTextureComponent("delete_" + i,
                new Rectangle(xPositionOnScreen + width - 48, itemY + 8, (int)(12 * DeleteTaskButtonScale), (int)(12 * DeleteTaskButtonScale)),
                null,
                "Delete",
                Game1.mouseCursors,
                new Rectangle(338, 494, 12, 12), DeleteTaskButtonScale));
            
            itemY += ItemHeight;
        }
    }
}
 
