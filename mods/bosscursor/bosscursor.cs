using System;
using BossCursorMod.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

public class BossCursor : Mod
{
    const int MinScreenWidth = 578;
    const int MinScreenHeight = 260;
    const float BaseDistance = 50f;
    const float Margin = 64f;

    Texture2D cursorTexture;

    public override void Load()
    {
        On_Main.DrawRain += (orig, self) =>
        {
            orig(self);
            DrawCursors();
        };
    }

    public override void PostSetupContent()
    {
        cursorTexture = ModContent.Request<Texture2D>("bosscursor/Assets/Cursor").Value;
    }

    static bool IsOnScreen(NPC npc)
    {
        float left = npc.position.X - Main.screenPosition.X;
        float top = npc.position.Y - Main.screenPosition.Y;
        return left + npc.width >= 0 && left <= Main.screenWidth && top + npc.height >= 0 && top <= Main.screenHeight;
    }

    void DrawCursors()
    {
        Player player = Main.player[Main.myPlayer];
        if (player.IsNull || !player.active)
            return;

        CursorMode mode = BossCursorConfig.Instance.DisplayMode;
        if (mode == CursorMode.Never)
            return;

        int screenWidth = Main.screenWidth, screenHeight = Main.screenHeight;
        float distance = BaseDistance * MathF.Min((float)screenWidth / MinScreenWidth, (float)screenHeight / MinScreenHeight);
        float nearDistance = MathF.Max(distance * 2f, 96f);
        Vector2 playerCenter = player.Center;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.IsNull || !npc.active)
                continue;

            Vector2 toNpc = npc.Center - playerCenter;
            float toNpcLength = toNpc.Length();
            if (toNpcLength <= 0.001f)
                continue;
            if (mode == CursorMode.Outside && (IsOnScreen(npc) || toNpcLength <= nearDistance))
                continue;

            int headId = npc.GetBossHeadTextureIndex();
            int setHead = NPCID.Sets.BossHeadTextures[npc.type];
            if (headId == -1 && setHead <= -1)
                continue;
            if (headId == -1)
                headId = setHead;

            Vector2 direction = toNpc / toNpcLength;
            Vector2 head = playerCenter + direction * distance;
            Vector2 screenHead = head - Main.screenPosition;

            if (screenHead.X < Margin)
                head.X += Margin - screenHead.X;
            else if (screenHead.X > screenWidth - Margin - 32f)
                head.X -= screenHead.X - (screenWidth - Margin - 32f);

            if (screenHead.Y < Margin)
                head.Y += Margin - screenHead.Y;
            else if (screenHead.Y > screenHeight - Margin - 32f)
                head.Y -= screenHead.Y - (screenHeight - Margin - 32f);

            screenHead = head - Main.screenPosition;
            Texture2D headTexture = TextureAssets.NpcHeadBoss[headId].Value;
            if (headTexture.IsNull)
                continue;

            var rectangle = new Rectangle((int)(screenHead.X - headTexture.Width / 2f), (int)(screenHead.Y - headTexture.Height / 2f), headTexture.Width, headTexture.Height);
            Main.spriteBatch.Draw(headTexture, rectangle, Color.White);

            Vector2 arrowPosition = screenHead + direction * 32f;
            float rotation = MathF.Atan2(toNpc.Y, toNpc.X);
            var origin = new Vector2(cursorTexture.Width / 2f, cursorTexture.Height / 2f);
            Main.spriteBatch.Draw(cursorTexture, arrowPosition, null, Color.White, rotation, origin, Vector2.One, SpriteEffects.None, 0f);
        }
    }
}
