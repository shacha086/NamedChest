using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using SVObject = StardewValley.Object;

namespace NamedChest;

// ReSharper disable once UnusedType.Global
public class ModEntry : Mod
{
    private readonly PerScreen<Chest?> _currentChest = new();

    private bool _isOnHostComputer;
    private NamesData? _namesData;
    private const string SaveKey = "shacha.NamedChest";

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.UpdateTicked += GameLoopOnUpdateTicked;
        helper.Events.Display.RenderingHud += DisplayOnRenderingHud;
        helper.Events.Display.RenderedActiveMenu += DisplayOnRenderedActiveMenu;
        helper.Events.GameLoop.SaveLoaded += GameLoopOnSaveLoaded;
        helper.ConsoleCommands.Add("dbg", "", OnDebugCommand);
    }

    private void DisplayOnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Game1.activeClickableMenu is not ItemGrabMenu itemGrabMenu)
        {
            return;
        }

        if (itemGrabMenu.source != ItemGrabMenu.source_chest)
        {
            return;
        }

        if (itemGrabMenu.chestColorPicker.itemToDrawColored is not Chest chestForShow)
        {
            return;
        }

        var x = itemGrabMenu.chestColorPicker.xPositionOnScreen + itemGrabMenu.chestColorPicker.width +
                IClickableMenu.borderWidth / 2;
        var y = itemGrabMenu.chestColorPicker.yPositionOnScreen + 16;
        var mousePosition = Game1.getMousePosition();
        var (width, height) =
            ItemRegistry.GetData(chestForShow.QualifiedItemId).GetSourceRect().Size;
        var rect = new Rectangle(x, y - 32, width * 4, height * 4 - 32);
        if (rect.Contains(mousePosition))
        {
            IClickableMenu.drawHoverText(Game1.spriteBatch, "更改名称", Game1.smallFont);
        }

        Monitor.LogOnce($"rect: {rect}, mouse: {mousePosition}", LogLevel.Alert);
    }

    private void GameLoopOnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _isOnHostComputer = Context.IsOnHostComputer;
        _namesData = ReadName();
    }

    private void DisplayOnRenderingHud(object? sender, RenderingHudEventArgs e)
    {
        if (Game1.activeClickableMenu != null)
        {
            return;
        }

        if (_currentChest.Value == null)
        {
            return;
        }

        var overrideX = -1;
        var overrideY = -1;
        var tile = Utility.ModifyCoordinatesForUIScale(
            Game1.GlobalToLocal(
                new Vector2(_currentChest.Value.TileLocation.X, _currentChest.Value.TileLocation.Y) * Game1.tileSize
            )
        );
        if (Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0)
        {
            overrideX = (int)(tile.X + Utility.ModifyCoordinateForUIScale(32));
            overrideY = (int)(tile.Y + Utility.ModifyCoordinateForUIScale(32));
        }

        var chestId = _currentChest.Value.GlobalInventoryId;
        if (chestId != null && (_namesData?.NamePair.TryGetValue(chestId, out var chestName) ?? false))
        {
            IClickableMenu.drawHoverText(
                Game1.spriteBatch,
                chestName,
                Game1.smallFont,
                overrideX: overrideX,
                overrideY: overrideY
            );
        }
    }

    private void OnDebugCommand(string name, string[] args)
    {
        Monitor.Log($"Current Chest: {_currentChest.Value}", LogLevel.Alert);
        Monitor.Log($"Current Chest Inventory: {_currentChest.Value?.netItems.Get()}", LogLevel.Alert);
    }

    private void GameLoopOnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!e.IsMultipleOf(4))
        {
            return;
        }

        _currentChest.Value = null;

        var gamepadTile = Game1.player.CurrentTool != null
            ? Utility.snapToInt(Game1.player.GetToolLocation() / Game1.tileSize)
            : Utility.snapToInt(Game1.player.GetGrabTile());
        var mouseTile = Game1.currentCursorTile;

        var tile = Game1.options.gamepadControls && Game1.timerUntilMouseFade <= 0 ? gamepadTile : mouseTile;
        if (Game1.currentLocation == null)
        {
            return;
        }

        if (!(Game1.currentLocation.Objects?.TryGetValue(tile, out var currentObject) ?? false)) return;
        if (currentObject is Chest chest)
        {
            _currentChest.Value = chest;
        }
    }

    private void SaveName(NamesData model)
    {
        if (_isOnHostComputer)
        {
            Helper.Data.WriteSaveData(SaveKey, model);
        }
        else
        {
            Helper.Data.WriteJsonFile("data.json", model);
        }
    }

    private NamesData ReadName()
    {
        if (_isOnHostComputer)
        {
            return Helper.Data.ReadSaveData<NamesData>(SaveKey) ?? new NamesData();
        }

        return Helper.Data.ReadJsonFile<NamesData>("data.json") ?? new NamesData();
    }
}