using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;

namespace BossChecklistMod.Common.UI;

public static class D
{
    public static int Round(double value) => (int)Math.Floor(value + 0.5);

    public static Color Rgb(int r, int g, int b, int a = 255) => new(r, g, b, a);

    static Texture2D Pixel => TextureAssets.MagicPixel.Value;
    public static SpriteFont Font => FontAssets.MouseText.Value;
    static SpriteBatch Batch => Main.spriteBatch;

    struct BatchState
    {
        public SpriteSortMode sort;
        public BlendState blend;
        public DepthStencilState depth;
        public RasterizerState raster;
        public Effect effect;
        public GameNullable<Matrix> matrix;
        public bool deferred;
    }

    static bool restarted;
    static BatchState saved;

    // The phone batches by texture unless told otherwise, and this UI draws things in layers:
    // the batch is restarted in order for the layer, and put back as it was afterwards.
    public static void Ordered(bool on)
    {
        SpriteBatch sb = Batch;
        if (on)
        {
            restarted = false;
            if (sb._beginCalled)
            {
                saved = new BatchState
                {
                    sort = sb._sortMode,
                    blend = sb._blendState,
                    depth = sb._depthStencilState,
                    raster = sb._rasterizerState,
                    effect = sb._effect,
                    matrix = sb._hasMatrix ? new GameNullable<Matrix>(sb._renderMatrix) : default,
                    deferred = sb.BatchDeffered,
                };
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, saved.blend, SamplerState.PointClamp, saved.depth, saved.raster, saved.effect, saved.matrix, false);
                restarted = true;
            }
            else
            {
                sb.Flush();
            }
            return;
        }
        if (!restarted)
        {
            sb.Flush();
            return;
        }
        restarted = false;
        sb.End();
        sb.Begin(saved.sort, saved.blend, Main.DefaultSamplerState, saved.depth, saved.raster, saved.effect, saved.matrix, saved.deferred);
    }

    public static void Reset() => Batch.Flush();

    public static void Fill(float x, float y, float w, float h, Color color)
    {
        if (w <= 0 || h <= 0)
            return;
        Batch.Draw(Pixel, new Rectangle(Round(x), Round(y), Round(w), Round(h)), color);
    }

    static void Border(float x, float y, float w, float h, Color color, float t)
    {
        Fill(x, y, w, t, color);
        Fill(x, y + h - t, w, t, color);
        Fill(x, y, t, h, color);
        Fill(x + w - t, y, t, h, color);
    }

    public static void Panel(float x, float y, float w, float h, Color back, Color border, float t = 2)
    {
        Fill(x, y, w, h, back);
        Border(x, y, w, h, border, t);
    }

    static readonly Dictionary<string, Vector2> measureCache = new();

    static Vector2 Raw(string text)
    {
        if (!measureCache.TryGetValue(text, out Vector2 size))
        {
            size = Font.MeasureString(text);
            if (measureCache.Count > 4000)
                measureCache.Clear();
            measureCache[text] = size;
        }
        return size;
    }

    static float norm;

    public static float Norm
    {
        get
        {
            if (norm == 0)
            {
                float h = Raw("Ag").Y;
                norm = h > 0 ? Math.Clamp(22f / h, 0.4f, 1.2f) : 1f;
            }
            return norm;
        }
    }

    public static Vector2 Measure(string text, float scale = 1)
    {
        Vector2 size = Raw(text);
        float k = scale * Norm;
        return new Vector2(size.X * k, size.Y * k);
    }

    public static void Text(string? text, float x, float y, Color color, float scale = 1)
    {
        if (string.IsNullOrEmpty(text))
            return;
        Utils.DrawBorderStringFourWay(Batch, Font, text, x, y, color, C.Black, Vector2.Zero, scale * Norm);
    }

    public static string Truncate(string text, float maxWidth, float scale = 1)
    {
        if (maxWidth <= 0)
            return "";
        if (Measure(text, scale).X <= maxWidth)
            return text;
        int lo = 0, hi = text.Length;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (Measure(text[..mid] + "...", scale).X <= maxWidth)
                lo = mid;
            else
                hi = mid - 1;
        }
        return lo <= 0 ? "..." : text[..lo] + "...";
    }

    public static bool ItemFrame(int type, out Texture2D texture, out Rectangle frame)
    {
        texture = Tracker.ItemTexture(type);
        frame = default;
        if (texture.IsNull)
            return false;
        DrawAnimation animation = type < Main.itemAnimations.Length ? Main.itemAnimations[type] : default;
        frame = !animation.IsNull ? animation.GetFrame(texture, -1) : new Rectangle(0, 0, texture.Width, texture.Height);
        return true;
    }

    public static void ItemIcon(int type, float cx, float cy, float size, float maxScale, Color color)
    {
        if (!ItemFrame(type, out Texture2D texture, out Rectangle frame))
            return;
        float scale = Math.Min(maxScale, Math.Min(size / frame.Width, size / frame.Height));
        Batch.Draw(texture, new Vector2(cx, cy), new GameNullable<Rectangle>(frame), color, 0f, new Vector2(frame.Width / 2f, frame.Height / 2f), scale, SpriteEffects.None, 0f);
    }

    public static void Sprite(Texture2D texture, float x, float y, float w, float h, Color? color = null, Rectangle? src = null, bool flip = false)
    {
        if (texture.IsNull || !(w > 0) || !(h > 0))
            return;
        int tw = texture.Width, th = texture.Height;
        if (tw <= 0 || th <= 0)
            return;
        Rectangle full = src ?? new Rectangle(0, 0, tw, th);
        Batch.Draw(texture, new Vector2(x, y), new GameNullable<Rectangle>(full), color ?? C.White, 0f, Vector2.Zero,
            new Vector2(w / full.Width, h / full.Height), flip ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }
}
