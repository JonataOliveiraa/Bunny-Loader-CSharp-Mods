using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace BetterReforgeMod.Common.Utilities;

public static class DrawUtils
{
    public static void DrawInvBG(Rectangle rect, Color color) => Utils.DrawInvBG(Main.spriteBatch, rect, color);

    public static void Rect(Texture2D texture, Rectangle rectangle, Color color) => Main.spriteBatch.Draw(texture, rectangle, color);

    public static void BorderString(SpriteFont font, string text, Vector2 position, Color color, float scale) =>
        Utils.DrawBorderStringFourWay(Main.spriteBatch, font, text, position.X, position.Y, color, Color.Black, Vector2.Zero, scale);

    public static void NewSpriteBatchForUI()
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, default, default, default, default, default, new GameNullable<Matrix>(Matrix.Identity), true);
    }
}
