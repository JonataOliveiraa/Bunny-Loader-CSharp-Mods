using System;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace DevToolMod.Core.Systems;

public static class MagnetSystem
{
    public static bool IsActive;

    const int MaxRange = 160;
    static float X = -1;
    static float Y = -1;

    public static void UpdatePlayer(Player player)
    {
        if (!IsActive) return;
        player.mouseInterface = true;
        Player.tileRangeX = Main.screenWidth;
        Player.tileRangeY = Main.screenHeight;
    }

    public static void DrawMagnet()
    {
        if (!IsActive) return;

        X = Main.worldMouseX - MaxRange / 2f;
        Y = Main.worldMouseY - MaxRange / 2f;

        if (Main.worldMouseLeft) PullNPCs();

        float pulse = MathF.Sin(Main.GlobalTimeWrappedHourly * 6) * 4;
        float width = MaxRange + pulse;
        float height = MaxRange + pulse;

        DrawHelper.Rectangle(
            Textures.Get("Circle"),
            new Rectangle((int)(X - pulse * 0.5f), (int)(Y - pulse * 0.5f), (int)width, (int)height),
            new Color(77, 66, 18, 77));
    }

    static bool CheckValidNPC(NPC npc) =>
        npc.active && !npc.dontCountMe && !npc.dontTakeDamage && npc.type != 488;

    static void PullNPCs()
    {
        float centerX = X + MaxRange * 0.5f + Main.screenPosition.X;
        float centerY = Y + MaxRange * 0.5f + Main.screenPosition.Y;
        float radius = MaxRange * 0.5f;
        float radiusSq = radius * radius;
        for (int i = 0; i < 200; i++)
        {
            NPC npc = Main.npc[i];
            if (!CheckValidNPC(npc)) continue;
            float dx = centerX - npc.Center.X;
            float dy = centerY - npc.Center.Y;
            float distSq = dx * dx + dy * dy;
            if (distSq > radiusSq) continue;
            Vector2 pos = npc.position;
            pos.X = centerX - npc.width * 0.5f;
            pos.Y = centerY - npc.height * 0.5f;
            npc.position = pos;
            npc.velocity = Vector2.Zero;
        }
    }
}
