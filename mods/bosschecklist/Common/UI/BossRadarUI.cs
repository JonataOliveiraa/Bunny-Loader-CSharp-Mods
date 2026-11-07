using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.UI;
using static BossChecklistMod.Common.UI.Common;

namespace BossChecklistMod.Common.UI;

public class BossRadarUI : UIState
{
    const int Max = 20;

    struct RadarDraw
    {
        public int head;
        public float x, y, rotation;
        public bool los;
    }

    HashSet<int>? whitelist;
    bool whitelistMini;
    readonly RadarDraw[] draws = new RadarDraw[Max];
    int drawCount;
    readonly bool[] losKnown = new bool[200];
    readonly bool[] los = new bool[200];
    int frame;

    public void InvalidateWhitelist() => whitelist = null;

    HashSet<int> Whitelist()
    {
        bool mini = Cfg.Feature.RadarMiniBosses;
        if (whitelist != null && whitelistMini == mini && whitelist.Count > 0)
            return whitelist;
        whitelistMini = mini;
        var list = new HashSet<int>();
        foreach (EntryInfo e in Tracker.sorted)
        {
            if (e.type == Data.EVENT || (e.type == Data.MINIBOSS && !mini))
                continue;
            foreach (int id in e.npcIDs)
            {
                bool hasHead = id < NPCID.Sets.BossHeadTextures.Length && NPCID.Sets.BossHeadTextures[id] != -1;
                if (hasHead || id >= NPCID.Count)
                    list.Add(id);
            }
        }
        whitelist = list;
        return list;
    }

    // Runs every frame while the radar is on, so it allocates nothing.
    public bool Prepare()
    {
        drawCount = 0;
        if (!Cfg.Feature.EnableBossRadar || !Tracker.finalized)
            return false;
        Player player = Util.Local;
        if (player.IsNull || Main.mapFullscreen || Main.gameMenu)
            return false;
        HashSet<int> list = Whitelist();
        frame++;
        bool viewRead = false;
        int sw = 0, sh = 0, rx = 0, ry = 0, rw = 0, rh = 0;
        Vector2 pc = default;
        for (int i = 0; i < 200 && drawCount < Max; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.IsNull || !npc.active || !list.Contains(npc.type))
                continue;
            int head = npc.GetBossHeadTextureIndex();
            if (head < 0)
                continue;
            if (!viewRead)
            {
                viewRead = true;
                sw = Main.screenWidth;
                sh = Main.screenHeight;
                Vector2 zoom = Main.GameViewMatrix.Zoom;
                float zx = Math.Min(0.25f * (zoom.X - 1), 0.175f), zy = Math.Min(0.25f * (zoom.Y - 1), 0.175f);
                Vector2 sp = Main.screenPosition;
                int pw = Main.PendingResolutionWidth != 0 ? Main.PendingResolutionWidth : sw;
                int ph = Main.PendingResolutionHeight != 0 ? Main.PendingResolutionHeight : sh;
                rx = (int)(sp.X + pw * zx);
                ry = (int)(sp.Y + ph * zy);
                rw = (int)(pw * (1 - 2 * zx));
                rh = (int)(ph * (1 - 2 * zy));
                pc = player.Center;
            }
            int w = npc.width, h = npc.height;
            Vector2 pos = npc.position;
            int left = rx - w / 2, top = ry - h / 2;
            if (pos.X < left + rw + w && pos.X + w > left && pos.Y < top + rh + h && pos.Y + h > top)
                continue;
            Vector2 c = npc.Center;
            float dx = c.X - pc.X, dy = c.Y - pc.Y;
            if (dx == 0)
                dx = 0.0001f;
            if (dy == 0)
                dy = 0.0001f;
            if (player.gravDir != 1)
                dy = -dy;
            float slope = dy / dx;
            int padX = (sw + w) / 2, padY = (sh + h) / 2;
            float y = dy > 0 ? Math.Min(dy, padY) : dy < -padY ? -padY : dy;
            float x = y / slope;
            if (x > 0)
                x = Math.Min(x, padX);
            else if (x <= -padX)
                x = -padX;
            y = x * slope;
            x += padX - w / 2f;
            y += padY - h / 2f;
            if (frame % 10 == 0 || !losKnown[i])
            {
                losKnown[i] = true;
                los[i] = Collision.CanHitLine(player.position, player.width, player.height, npc.position, w, h);
            }
            draws[drawCount++] = new RadarDraw { head = head, x = x, y = y, rotation = MathF.Atan2(dy, dx), los = los[i] };
        }
        return drawCount > 0;
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        int sw = Main.screenWidth, sh = Main.screenHeight;
        float opacity = Cfg.Feature.RadarOpacity != 0 ? Cfg.Feature.RadarOpacity : 0.75f;
        float k = UIScale();
        Texture2D arrow = Tex("Extra_RadarArrow");
        SpriteBatch batch = Main.spriteBatch;
        for (int i = 0; i < drawCount; i++)
        {
            RadarDraw d = draws[i];
            Texture2D texture = Tracker.BossHeadTexture(d.head);
            if (texture.IsNull)
                continue;
            int tw = texture.Width, th = texture.Height;
            const int m = 10;
            float px = d.x, py = d.y;
            if (px >= sw - tw - m)
                px = sw - tw - m;
            if (px <= tw + m)
                px = tw + m;
            if (py >= sh - th - m)
                py = sh - th - m;
            if (py <= th + m)
                py = th + m;
            float a = d.los ? opacity : opacity - 0.25f;
            int g = D.Round(211 * a), al = D.Round(255 * a);
            float x = px / k, y = py / k;
            D.Sprite(texture, D.Round(x - tw / 2f), D.Round(y - th / 2f), tw, th, D.Rgb(g, g, g, al));
            if (arrow.IsNull)
                continue;
            float ax = x + MathF.Cos(d.rotation) * 24, ay = y + MathF.Sin(d.rotation) * 24;
            Color color = d.los ? new Color(0, D.Round(128 * opacity), 0, 150) : new Color(D.Round(255 * opacity), 0, 0, 150);
            batch.Draw(arrow, new Vector2(ax, ay), default(GameNullable<Rectangle>), color, d.rotation, new Vector2(8, 8), 1f, SpriteEffects.None, 0f);
        }
    }
}
