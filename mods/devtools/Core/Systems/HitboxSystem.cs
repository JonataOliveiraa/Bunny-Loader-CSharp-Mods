using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace DevToolMod.Core.Systems;

public enum HitboxMode
{
    None,
    NPCs,
    Projectiles,
    NPCsAndProjectiles,
    Count,
}

public static class HitboxSystem
{
    public static HitboxMode HitboxMode = HitboxMode.None;

    static void DrawHitbox(Entity entity, Color color)
    {
        Texture2D texture = TextureAssets.MagicPixel.Value;
        int w = entity.width, h = entity.height;
        if (w == 0 || h == 0) return;
        float x = entity.position.X, y = entity.position.Y;
        DrawHelper.Rectangle(texture, new Rectangle((int)(x - Main.screenPosition.X), (int)(y - Main.screenPosition.Y), w, h), color);
    }

    public static void DrawNPCs(bool behindTiles)
    {
        if (HitboxMode == HitboxMode.NPCs || HitboxMode == HitboxMode.NPCsAndProjectiles)
        {
            var color = new Color(255, 0, 0, 60);
            for (int i = 0; i < 200; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.IsNull || !npc.active) continue;
                if (npc.behindTiles == behindTiles)
                    DrawHitbox(npc, color);
            }
        }
    }

    public static void DrawProj(Projectile proj)
    {
        if (HitboxMode == HitboxMode.Projectiles || HitboxMode == HitboxMode.NPCsAndProjectiles)
            DrawHitbox(proj, new Color(0, 0, 255, 60));
    }
}
