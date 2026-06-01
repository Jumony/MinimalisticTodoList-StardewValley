using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MinimalisticTodoList
{
    /// <summary>The mod entry point.</summary>
    internal sealed class ModEntry : Mod
    {
        public List<string> Tasks { get; set; } = new List<string>();
        private TodoListData Data = null;

        /*********
         ** Public methods
         *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            helper.Events.GameLoop.Saving += this.OnSaving;
        }

        /*********
         ** Private methods
         *********/
        /// <summary>Raised after the player presses a button on the keyboard, controller, or mouse.</summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            // print button presses to the console window
            this.Monitor.Log($"{Game1.player.Name} pressed {e.Button}.", LogLevel.Debug);

            if (e.Button == SButton.L && Game1.activeClickableMenu == null)
            {
                Game1.activeClickableMenu = new TodoMenu(this.Data);
            }
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            this.Data = this.Helper.Data.ReadSaveData<TodoListData>("todo-list-data") ?? new TodoListData();
        }

        private void OnSaving(object? sender, SavingEventArgs e)
        {
            this.Helper.Data.WriteSaveData<TodoListData>("todo-list-data", this.Data);
        }
    }
}