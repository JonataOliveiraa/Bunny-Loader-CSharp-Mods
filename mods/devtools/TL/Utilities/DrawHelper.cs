using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace DevToolMod.TL.Utilities;

public static class DrawHelper
{
    // Inventory-style panel background (nine-slice). The game's Utils.DrawInvBG hard-codes a
    // 10 pixel border: only the middle stretches, so a smaller panel looks like its background
    // did not change. DrawSplicedPanel slices the same way but takes the border width.
    public static void InventoryPanel(Rectangle rect, Color color, float border = 10)
    {
        int b = Math.Max(1, (int)MathF.Floor(border + 0.5f));
        Texture2D texture = TextureAssets.InventoryBack13.Value;
        Utils.DrawSplicedPanel(Main.spriteBatch, texture, rect.X, rect.Y, rect.Width, rect.Height, b, b, b, b, color);
    }

    public static void BuffTooltip(string name, string tooltip, int x = -1, int y = -1)
    {
        if (x == -1) x = Main.mouseX;
        if (y == -1) y = Main.mouseY;
        Main.instance.MouseText_DrawBuffTooltip(name, tooltip, ref x, ref y);
    }

    public static void Rectangle(Texture2D texture, Rectangle rectangle, Color color) =>
        Main.spriteBatch.Draw(texture, rectangle, color);

    public static SpriteFont MouseTextFont() => FontAssets.MouseText.Value;

    public static void String(SpriteFont font, string text, Vector2 position, Color color, float scale = 1f) =>
        Main.spriteBatch.DrawString(font, text, position, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

    public static void BorderString(SpriteFont font, string text, Vector2 position, Color color, float scale = 1f) =>
        Utils.DrawBorderStringFourWay(Main.spriteBatch, font, text, position.X, position.Y, color, Color.Black, Vector2.Zero, scale);

    // Starts a new batch in screen coordinates (Identity). It checks first that the batch is
    // open: End() without a Begin() throws, and the mobile UI layers do not always keep it open.
    public static void NewSpriteBatchForUI()
    {
        SpriteBatch batch = Main.spriteBatch;
        if (batch._beginCalled) batch.End();
        batch.Begin(SpriteSortMode.Deferred, default, default, default, default, default, Matrix.Identity, true);
    }
}
