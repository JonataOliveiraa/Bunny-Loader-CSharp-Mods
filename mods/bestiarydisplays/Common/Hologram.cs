using System;
using BestiaryDisplaysMod.Content.TileEntities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace BestiaryDisplaysMod.Common;

public sealed class HologramView
{
    public readonly int netId;
    public Bestiary.Dummy? dummy;
    public long last = -1;

    public HologramView(int netId) => this.netId = netId;

    public void Release()
    {
        dummy?.Release();
        dummy = null;
    }
}

public static class Hologram
{
    const float Cull = 1400;
    const int Slices = 5;

    static int ToByte(double v) => Math.Max(0, Math.Min(255, Util.Round(v)));

    static Color Faded(int r, int g, int b, double k) => new(ToByte(r * k), ToByte(g * k), ToByte(b * k), ToByte(255 * k));

    static Color Glow(int r, int g, int b, double k) => new(ToByte(r * k), ToByte(g * k), ToByte(b * k), 0);

    static double Hash(int n)
    {
        unchecked
        {
            n = (n << 13) ^ n;
            int v = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
            return (1 - v / 1073741824.0) * 0.5 + 0.5;
        }
    }

    static HologramView ViewOf(HologramTileEntity entity, int netId)
    {
        HologramView? view = entity.view;
        if (view == null || view.netId != netId)
        {
            view?.Release();
            view = new HologramView(netId);
            entity.view = view;
        }
        return view;
    }

    static Rectangle? NormalFrame(HologramTileEntity entity, HologramView view)
    {
        view.dummy ??= new Bestiary.Dummy(view.netId);
        long now = Main.GameUpdateCount;
        int speed = Math.Max(1, entity.AnimationSpeed);
        if (view.last < 0 || (entity.Animate && now - view.last >= speed))
        {
            view.dummy.Advance();
            view.last = now;
        }
        Rectangle? f = view.dummy.Frame;
        return f is { Width: > 0, Height: > 0 } ? f : null;
    }

    static Rectangle CycleFrame(HologramTileEntity entity, int netId, Texture2D tex)
    {
        int frames = Bestiary.FrameCount(netId);
        int h = Math.Max(1, tex.Height / frames);
        int index = 0;
        if (entity.Animate && frames > 1)
            index = (int)(Main.GameUpdateCount / (uint)Math.Max(1, entity.AnimationSpeed) % (uint)frames);
        return new Rectangle(0, index * h, tex.Width, h);
    }

    readonly struct Painter
    {
        readonly SpriteBatch sb;
        readonly Texture2D tex;
        readonly Rectangle src;
        readonly float cx, cy, angle, scale, cos, sin;

        public Painter(SpriteBatch sb, Texture2D tex, Rectangle src, float cx, float cy, float angle, float scale)
        {
            this.sb = sb;
            this.tex = tex;
            this.src = src;
            this.cx = cx;
            this.cy = cy;
            this.angle = angle;
            this.scale = scale;
            cos = MathF.Cos(angle);
            sin = MathF.Sin(angle);
        }

        void Part(int sx, int sy, int sw, int sh, float lx, float ly, Color color)
        {
            if (sw <= 0 || sh <= 0)
                return;
            float dx = (lx - src.Width / 2f) * scale, dy = (ly - src.Height / 2f) * scale;
            float x = cx + dx * cos - dy * sin;
            float y = cy + dx * sin + dy * cos;
            sb.Draw(tex, new Vector2(x, y), new Rectangle(src.X + sx, src.Y + sy, sw, sh), color, angle, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        public void Whole(Color color) => Part(0, 0, src.Width, src.Height, 0, 0, color);

        public void Scanlines(Color color, int gap = 3, int thickness = 1)
        {
            for (int y = 0; y < src.Height; y += gap)
                Part(0, y, src.Width, Math.Min(thickness, src.Height - y), 0, y, color);
        }

        public void Glitch(Color color, double t)
        {
            int step = (int)Math.Floor(t * 12);
            int h = Math.Max(1, src.Height / Slices);
            for (int i = 0; i < Slices; i++)
            {
                if (Hash(i * 928371 + step) < 0.5)
                    continue;
                int y = i * h;
                if (y >= src.Height)
                    break;
                float shift = (float)((Hash(i * 7 + step) - 0.5) * 10);
                Part(0, y, src.Width, Math.Min(h, src.Height - y), shift, y, color);
            }
        }
    }

    public static void DrawHologram(HologramTileEntity entity, SpriteBatch sb)
    {
        int? id = Bestiary.NetIdOf(entity.BestiaryCreditId);
        if (id == null)
            return;
        int netId = id.Value;
        Texture2D tex = Bestiary.Texture(netId);
        if (tex.IsNull)
            return;
        HologramView view = ViewOf(entity, netId);
        Rectangle? normal = entity.AlternateRender ? null : NormalFrame(entity, view);
        Rectangle src = normal ?? CycleFrame(entity, netId, tex);
        Point size = Bestiary.FrameSize(netId);

        Vector2 sp = Main.screenPosition;
        float cx = (entity.Position.X + 1.5f) * 16 + entity.OffsetX - sp.X;
        float cy = entity.Position.Y * 16 - size.Y / 2f - 8 + entity.OffsetY - sp.Y;
        float reach = Cull + Math.Max(size.X, size.Y) * entity.Scale;
        if (cx < -reach || cy < -reach || cx > Main.screenWidth + reach || cy > Main.screenHeight + reach)
            return;

        var painter = new Painter(sb, tex, src, cx, cy, entity.Rotation * MathF.PI / 180f, Math.Max(0.01f, entity.Scale));
        double t = Main.GlobalTimeWrappedHourly;
        double flicker = 0.55 + 0.15 * Math.Sin(t * 20);

        if (entity.AlternateRender && entity.HologramFx)
        {
            painter.Whole(Faded(120, 200, 255, flicker));
            painter.Glitch(Faded(180, 120, 255, flicker * 0.5), t);
            painter.Scanlines(Faded(0, 25, 45, 0.5));
            return;
        }

        Color? tint = Bestiary.TintOf(netId);
        painter.Whole(tint ?? new Color(255, 255, 255, 255));
        if (!entity.HologramFx)
            return;
        painter.Scanlines(Glow(70, 150, 255, 0.7 + 0.2 * flicker), 3, 2);
        painter.Glitch(Glow(150, 80, 230, flicker * 0.7), t);
    }
}
