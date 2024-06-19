using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace NamedChest;

public class CancelableNamingMenu : NamingMenu
{
    // ReSharper disable once InconsistentNaming (for unified style)
    // ReSharper disable once FieldCanBeMadeReadOnly.Global (for unified style)
    // ReSharper disable once MemberCanBePrivate.Global (for unified style)
    public ClickableTextureComponent cancelNamingButton;

    public CancelableNamingMenu(doneNamingBehavior b, string title, string? defaultName = null) : base(b, title,
        defaultName)
    {
        textBox.X -= 24;
        var textureComponent = new ClickableTextureComponent(new Rectangle(
                textBox.X + textBox.Width + 32 + 4 + 64, Game1.uiViewport.Height / 2 - 8, 64, 64), Game1.mouseCursors,
            Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47), 1f)
        {
            myID = 105,
            leftNeighborID = 102,
            rightNeighborID = 103
        };

        cancelNamingButton = textureComponent;

        doneNamingButton.rightNeighborID = 105;
        doneNamingButton.bounds.X = textBox.X + textBox.Width + 32 + 4;

        randomButton.leftNeighborID = 105;
        randomButton.bounds.X = textBox.X + textBox.Width + 64 + 64 + 48 - 8;
        if (!Game1.options.SnappyMenus)
            return;
        base.populateClickableComponentList();
        base.snapToDefaultClickableComponent();
    }

    public override void draw(SpriteBatch b)
    {
        base.draw(b);
        cancelNamingButton.draw(b);
    }

    public override void performHoverAction(int x, int y)
    {
        base.performHoverAction(x, y);
        cancelNamingButton.scale = cancelNamingButton.containsPoint(x, y)
            ? Math.Min(1.1f, cancelNamingButton.scale + 0.05f)
            : Math.Max(1f, cancelNamingButton.scale - 0.05f);
    }

    public override void receiveLeftClick(int x, int y, bool playSound = true)
    {
        base.receiveLeftClick(x, y, playSound);
        if (!cancelNamingButton.containsPoint(x, y)) return;
        exitThisMenuNoSound();
        Game1.playSound("smallSelect");
    }
}