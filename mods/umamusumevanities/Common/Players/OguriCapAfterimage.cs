using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;

namespace umamusumevanities.Common.Players;

public static class OguriCapAfterimage
{
    const int Lifespan = 30;
    const int MaxGhosts = 2;
    const int MaxPieces = 48;
    const int FollowupTick = 8;

    struct Piece
    {
        public Texture2D texture;
        public float x, y, rotation, ox, oy, sx, sy;
        public Rectangle rect;
        public Color color;
        public SpriteEffects effects;
        public int shader;
    }

    sealed class Ghost
    {
        public int age;
        public float worldX, worldY;
        public List<Piece>? pieces;
    }

    sealed class State
    {
        public int tick;
        public readonly List<Ghost> active = new();
    }

    static readonly Dictionary<int, State> ghosts = new();
    static readonly List<Ghost> ready = new();
    static readonly List<DrawData> copies = new();

    static Ghost MakeGhost(Player player)
    {
        Vector2 feet = player.Bottom;
        return new Ghost { worldX = feet.X, worldY = feet.Y };
    }

    static void Capture(PlayerDrawSet drawInfo, Ghost ghost)
    {
        GameArray<DrawData> cache = drawInfo.DrawDataCache_;
        int total = Math.Min(drawInfo.DrawDataCacheCount, cache.Length);
        Vector2 screen = Main.screenPosition;
        float originX = ghost.worldX - screen.X;
        float originY = ghost.worldY - screen.Y;
        var pieces = new List<Piece>(Math.Min(total, MaxPieces));
        for (int i = 0; i < total && pieces.Count < MaxPieces; i++)
        {
            DrawData data = cache[i];
            if (data.texture.IsNull)
                continue;
            Rectangle rect = data.sourceRect;
            if (rect.Width <= 0 || rect.Height <= 0)
                continue;
            pieces.Add(new Piece
            {
                texture = data.texture,
                x = data.position.X - originX,
                y = data.position.Y - originY,
                rect = rect,
                color = data.color,
                rotation = data.rotation,
                ox = data.origin.X,
                oy = data.origin.Y,
                sx = data.scale.X,
                sy = data.scale.Y,
                effects = data.effect,
                shader = data.shader,
            });
        }
        ghost.pieces = pieces;
    }

    static void DrawGhost(Ghost ghost, int quantity, List<DrawData> output)
    {
        float progress = ghost.age / (float)Lifespan;
        float opacity = 0.6f * (1 - progress);
        if (quantity <= 0 || opacity <= 0)
            return;
        float grow = 1 + 0.4f * progress;
        Vector2 screen = Main.screenPosition;
        float x = ghost.worldX - screen.X;
        float y = ghost.worldY - screen.Y;
        for (int i = 0; i < quantity; i++)
        {
            Piece piece = ghost.pieces![i];
            DrawData data = DrawData.New(piece.texture, new Vector2(x + piece.x * grow, y + piece.y * grow), piece.rect, Color.Multiply(piece.color, opacity),
                piece.rotation, new Vector2(piece.ox, piece.oy), new Vector2(piece.sx * grow, piece.sy * grow), piece.effects, 0);
            data.shader = piece.shader;
            output.Add(data);
        }
    }

    static void InsertGhosts(PlayerDrawSet drawInfo)
    {
        GameArray<DrawData> cache = drawInfo.DrawDataCache_;
        int total = Math.Min(drawInfo.DrawDataCacheCount, cache.Length);
        int free = cache.Length - total;
        if (free <= 0 || ready.Count == 0)
            return;
        int quota = Math.Min(MaxPieces, free / ready.Count);
        copies.Clear();
        if (quota > 0)
        {
            foreach (Ghost ghost in ready)
                DrawGhost(ghost, Math.Min(quota, ghost.pieces!.Count), copies);
        }
        int size = copies.Count;
        if (size == 0)
            return;
        for (int i = total - 1; i >= 0; i--)
            cache[i + size] = cache[i];
        for (int i = 0; i < size; i++)
            cache[i] = copies[i];
        drawInfo.DrawDataCacheCount = total + size;
    }

    public static void Activate(Player player)
    {
        var state = new State();
        state.active.Add(MakeGhost(player));
        ghosts[player.whoAmI] = state;
    }

    public static void Update(Player player)
    {
        int key = player.whoAmI;
        if (!ghosts.TryGetValue(key, out State? state))
            return;
        if (!player.active || player.dead || player.ghost)
        {
            ghosts.Remove(key);
            return;
        }
        state.tick++;
        for (int i = state.active.Count - 1; i >= 0; i--)
        {
            if (++state.active[i].age >= Lifespan)
                state.active.RemoveAt(i);
        }
        if (state.tick == FollowupTick)
        {
            if (state.active.Count >= MaxGhosts)
                state.active.RemoveAt(0);
            state.active.Add(MakeGhost(player));
        }
        if (state.active.Count == 0)
            ghosts.Remove(key);
    }

    public static void Install()
    {
        On_PlayerDrawLayers.DrawPlayer_RenderAllLayers += static (orig, drawInfo, positionalOffsets) =>
        {
            if (ghosts.Count > 0)
            {
                Player player = drawInfo.drawPlayer;
                if (!player.IsNull && ghosts.TryGetValue(player.whoAmI, out State? state) && drawInfo.shadow == 0 && player.active && !player.dead)
                {
                    ready.Clear();
                    foreach (Ghost ghost in state.active)
                    {
                        if (ghost.pieces == null)
                            Capture(drawInfo, ghost);
                        if (ghost.pieces!.Count > 0)
                            ready.Add(ghost);
                    }
                    InsertGhosts(drawInfo);
                }
            }
            orig(drawInfo, positionalOffsets);
        };
    }
}
