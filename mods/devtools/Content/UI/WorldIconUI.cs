using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace DevToolMod.Content.UI;

public class WorldIconUI : ModInterface
{
    public static bool IsActive;
    public static WorldIconUI? Instance;

    public static void DrawIcons() => Instance?.Draw();

    bool active;
    Rectangle tempRect = new(0, 0, 1, 1);

    public WorldIconUI()
    {
        Instance = this;
    }

    void CheckActive() => active = IsActive;

    static bool IsOnScreen(float x, float y) =>
        x >= Main.screenPosition.X ||
        x <= Main.screenPosition.X + Main.screenWidth ||
        y >= Main.screenPosition.Y ||
        y <= Main.screenPosition.Y + Main.screenHeight;

    public override void Draw()
    {
        CheckActive();
        if (!active) return;

        int wSpawnX = Main.spawnTileX * 16 + 8;
        int wSpawnY = Main.spawnTileY * 16 + 8;
        DrawIconAt(Textures.Get("SpawnPoint"), wSpawnX, wSpawnY);

        Player player = Main.player[Main.myPlayer];
        if (player.SpawnX != -1 && player.SpawnY != -1)
        {
            int spX = player.SpawnX * 16 + 16;
            int spY = player.SpawnY * 16 - 8;
            DrawIconAt(Textures.Get("SpawnBed"), spX, spY);
        }
    }

    void DrawIconAt(Texture2D texture, float x, float y)
    {
        if (!IsOnScreen(x, y)) return;

        float scale = Main.screenHeight / 246f * 0.75f;
        tempRect.Width = (int)(texture.Width * scale);
        tempRect.Height = (int)(texture.Height * scale);
        tempRect.X = (int)(x - Main.screenPosition.X - tempRect.Width * 0.5f);
        tempRect.Y = (int)(y - Main.screenPosition.Y - tempRect.Height * 0.5f);

        DrawHelper.Rectangle(texture, tempRect, Color.White);
    }
}
