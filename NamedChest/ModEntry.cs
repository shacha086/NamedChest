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

    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.UpdateTicked += GameLoopOnUpdateTicked;
        helper.Events.Display.RenderingHud += DisplayOnRenderingHud;
        helper.Events.Display.RenderedActiveMenu += DisplayOnRenderedActiveMenu;
        helper.Events.GameLoop.SaveLoaded += GameLoopOnSaveLoaded;
    }

    private void DisplayOnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (!_isOnHostComputer)
        {
            return;
        }
        if (Game1.activeClickableMenu is not ItemGrabMenu itemGrabMenu)
        {
            return;
        }

        if (itemGrabMenu.source != ItemGrabMenu.source_chest)
        {
            return;
        }

        var sourceChest = Helper.Reflection.GetField<Chest>(itemGrabMenu, "sourceItem").GetValue();

        if (itemGrabMenu.chestColorPicker.itemToDrawColored is not Chest chestForShow)
        {
            return;
        }

        if (!chestForShow.playerChest.Value)
        {
            return;
        }

        var chestColorPicker = itemGrabMenu.chestColorPicker;
        
        if (!Game1.player.showChestColorPicker)
        {
            chestForShow.draw(Game1.spriteBatch, chestColorPicker.xPositionOnScreen + chestColorPicker.width + IClickableMenu.borderWidth / 2, chestColorPicker.yPositionOnScreen + 16, local: true);
        }

        if (!IsTouchedChest(chestColorPicker, chestForShow))
        {
            return;
        }
        
        IClickableMenu.drawHoverText(Game1.spriteBatch, "更改名称", Game1.smallFont);
        
        if (Helper.Input.GetState(SButton.MouseLeft) == SButtonState.Released)
        {
            Game1.activeClickableMenu = new CancelableNamingMenu(answer =>
            {
                SaveName(sourceChest, answer);
                Game1.exitActiveMenu();
            }, "更改名称", ReadName(sourceChest));
        }
    }

    private static bool IsTouchedChest(IClickableMenu chestColorPicker, Chest chestForShow)
    {
        var x = chestColorPicker.xPositionOnScreen + chestColorPicker.width +
                IClickableMenu.borderWidth / 2;
        var y = chestColorPicker.yPositionOnScreen + 16;
        var mousePosition = Game1.getMousePosition();
        var (width, height) =
            chestForShow.boundingBox.Value.Size;
        var rect = new Rectangle(x, y - 32, width, height + 32);
        return rect.Contains(mousePosition);
    }

    private void GameLoopOnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _isOnHostComputer = Context.IsOnHostComputer;
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

        var chestName = ReadName(_currentChest.Value);
        if (chestName != null)
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

    private void SaveName(Chest sourceChest, string name)
    {
        if (_isOnHostComputer)
        {
            sourceChest.modData[$"{ModManifest.UniqueID}/chest_name"] = name;
        }
    }

    private string? ReadName(Chest sourceChest)
    {
        return sourceChest.modData.TryGetValue($"{ModManifest.UniqueID}/chest_name", out var name) ? name : null;
    }
}