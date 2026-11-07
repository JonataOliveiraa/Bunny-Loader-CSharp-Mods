using System;
using CoolerItemVisualEffectMod.Common.Configs;
using CoolerItemVisualEffectMod.Common.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CoolerItemVisualEffectMod.Common.Layers;

static class CoolerDraw
{
    public const int MaxTrailCopies = 36;

    static ItemTexture? sparkle;

    public static ItemTexture? GetSparkle()
    {
        if (sparkle != null)
            return sparkle;
        Texture2D tex = TextureAssets.Extra[98].Value;
        if (!tex.IsNull && tex.Width > 0)
            sparkle = new ItemTexture { Tex = tex, W = tex.Width, H = tex.Height, Rect = new Rectangle(0, 0, tex.Width, tex.Height), Ready = true };
        return sparkle;
    }

    // Alpha 0: the sprite adds light instead of covering what is behind it.
    public static void PushSprite(ref PlayerDrawSet drawInfo, Vector2 sp, ItemTexture tex, float x, float y, float rot, float originX, float originY, float scale, SpriteEffects effect, int r, int g, int b)
    {
        var color = new Color(Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255), 0);
        drawInfo.DrawDataCache.Add(new DrawData(tex.Tex, new Vector2(x - sp.X, y - sp.Y), tex.Rect, color, rot, new Vector2(originX, originY), scale, effect, 0));
    }

    public static Color TintOf(CoolerItemConfig cfg) => cfg.UseWeaponColor ? new Color(255, 255, 255) : cfg.TrailColor;

    public static SpriteEffects EffectOf(int dir, int grav) => (dir < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None) | (grav < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);

    public static int Scale(int value, byte channel) => (int)CoolerItemCore.JsRound(value * channel / 255f);
}

// Afterimages of the weapon behind the held item, one per stored tick and some in between.
public class CoolerItemTrailLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.HeldItem);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (drawInfo.shadow > 0f || player.dead)
            return false;
        CoolerItemConfig cfg = CoolerItemConfig.Instance;
        if (!cfg.Enabled || !cfg.EnableTrail)
            return false;
        return player.GetModPlayer<CoolerItemPlayer>().History.Count > 0;
    }

    // Set at the start of Draw, for DrawCopy. Drawing is single-threaded.
    Preset pre = null!;
    float gain;
    Color tint;
    Vector2 sp;
    int tick;
    int pushed;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        CoolerItemConfig cfg = CoolerItemConfig.Instance;
        CoolerItemPlayer mp = drawInfo.drawPlayer.GetModPlayer<CoolerItemPlayer>();
        SwingHistory hist = mp.History;
        if (hist.Count == 0)
            return;

        pre = CoolerItemCore.GetPreset(cfg.Sequence);
        float life = Math.Clamp(cfg.TrailLength * pre.Len, 3f, 40f);
        gain = Math.Clamp(cfg.TrailIntensity, 0f, 1f) * Math.Clamp(cfg.AlphaFactor, 0f, 3f) * pre.Light;
        if (gain <= 0.01f)
            return;
        tint = CoolerDraw.TintOf(cfg);
        sp = Main.screenPosition;
        tick = mp.Tick;

        int alive = 0;
        for (int i = 0; i < hist.Count; i++)
        {
            if ((tick - hist[i].T) / life < 1f)
                alive++;
        }
        if (alive == 0)
            return;
        int quality = Math.Clamp(cfg.TrailQuality, 1, 5);
        int perStep = pre.Halo >= 2 ? 2 : 1;
        int sub = Math.Clamp(CoolerDraw.MaxTrailCopies / (alive * perStep), 1, quality);

        pushed = 0;
        for (int i = 0; i < hist.Count; i++)
        {
            SwingEntry a = hist[i];
            float ageA = (tick - a.T) / life;
            if (ageA >= 1f)
                break;
            SwingEntry? b = i + 1 < hist.Count ? hist[i + 1] : null;
            if (b == null || a.Brk || a.Type != b.Type || a.Dir != b.Dir || a.Grav != b.Grav)
            {
                DrawCopy(ref drawInfo, a, null, 0f, ageA, ageA, i);
                continue;
            }
            float ageB = (tick - b.T) / life;
            for (int s = 0; s < sub; s++)
                DrawCopy(ref drawInfo, a, b, (float)s / sub, ageA, ageB, i * 7 + s);
        }
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    static float LerpAngle(float a, float b, float t)
    {
        const float Tau = MathF.PI * 2f;
        float d = ((b - a + MathF.PI) % Tau + Tau) % Tau - MathF.PI;
        return a + d * t;
    }

    void DrawCopy(ref PlayerDrawSet drawInfo, SwingEntry a, SwingEntry? b, float t, float ageA, float ageB, int seed)
    {
        float age = Lerp(ageA, ageB, t);
        if (age >= 1f || pushed >= CoolerDraw.MaxTrailCopies)
            return;
        float w = MathF.Pow(1f - age, 1.7f);
        if (pre.Pulse > 0f)
            w *= 0.25f + 0.75f * MathF.Abs(MathF.Cos(age * MathF.PI * pre.Pulse));
        int v = (int)MathF.Min(255f, CoolerItemCore.JsRound(255f * gain * w));
        if (v < 4)
            return;

        float x = b != null ? Lerp(a.X, b.X, t) : a.X;
        float y = b != null ? Lerp(a.Y, b.Y, t) : a.Y;
        float rot = b != null ? LerpAngle(a.Rot, b.Rot, t) : a.Rot;
        ItemTexture? tex = CoolerItemCore.GetItemTexture(a.Type);
        if (tex == null)
            return;
        SpriteEffects effect = CoolerDraw.EffectOf(a.Dir, a.Grav);
        float ox = a.Dir < 0 ? tex.W : 0f;
        float oy = a.Grav < 0 ? 0f : tex.H;
        float stretch = 1f + pre.Grow * age;

        CoolerDraw.PushSprite(ref drawInfo, sp, tex, x, y, rot, ox, oy, a.Scale * stretch, effect,
            CoolerDraw.Scale(v, tint.R), CoolerDraw.Scale(v, tint.G), CoolerDraw.Scale(v, tint.B));
        pushed++;

        if (pre.Halo >= 2 && pushed < CoolerDraw.MaxTrailCopies)
        {
            float shake = pre.Jitter > 0f ? MathF.Sin(seed * 12.9898f + tick * 0.7f) * pre.Jitter * (0.4f + age) : 0f;
            int hv = (int)CoolerItemCore.JsRound(v * 0.45f);
            if (hv >= 4)
            {
                CoolerDraw.PushSprite(ref drawInfo, sp, tex, x, y, rot + shake, ox, oy, a.Scale * (stretch + pre.Grow * 0.9f + 0.06f), effect,
                    CoolerDraw.Scale(hv, tint.R), CoolerDraw.Scale(hv, tint.G), CoolerDraw.Scale(hv, tint.B));
                pushed++;
            }
        }
    }
}

// The weapon drawn again in additive light, and a star at its tip.
public class CoolerItemGlowLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.HeldItem);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (drawInfo.shadow > 0f || player.dead)
            return false;
        CoolerItemConfig cfg = CoolerItemConfig.Instance;
        if (!cfg.Enabled || !(cfg.EnableGlow || cfg.EnableSparkle))
            return false;
        return player.GetModPlayer<CoolerItemPlayer>().Current != null;
    }

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        CoolerItemConfig cfg = CoolerItemConfig.Instance;
        Player player = drawInfo.drawPlayer;
        SwingEntry? cur = player.GetModPlayer<CoolerItemPlayer>().Current;
        if (cur == null)
            return;
        ItemTexture? tex = CoolerItemCore.GetItemTexture(cur.Type);
        if (tex == null)
            return;

        Preset pre = CoolerItemCore.GetPreset(cfg.Sequence);
        Color tint = CoolerDraw.TintOf(cfg);
        Vector2 sp = Main.screenPosition;
        SpriteEffects effect = CoolerDraw.EffectOf(cur.Dir, cur.Grav);
        float ox = cur.Dir < 0 ? tex.W : 0f;
        float oy = cur.Grav < 0 ? 0f : tex.H;

        if (cfg.EnableGlow)
        {
            float extra = Math.Clamp(cfg.WeaponExtraLight, 0f, 1f) * pre.Light;
            int v = (int)CoolerItemCore.JsRound(255f * extra);
            if (v >= 4)
            {
                int r = CoolerDraw.Scale(v, tint.R), g = CoolerDraw.Scale(v, tint.G), b = CoolerDraw.Scale(v, tint.B);
                CoolerDraw.PushSprite(ref drawInfo, sp, tex, cur.X, cur.Y, cur.Rot, ox, oy, cur.Scale, effect, r, g, b);
                CoolerDraw.PushSprite(ref drawInfo, sp, tex, cur.X, cur.Y, cur.Rot, ox, oy, cur.Scale * 1.06f, effect,
                    (int)CoolerItemCore.JsRound(r * 0.5f), (int)CoolerItemCore.JsRound(g * 0.5f), (int)CoolerItemCore.JsRound(b * 0.5f));
            }
        }

        if (cfg.EnableSparkle)
        {
            ItemTexture? star = CoolerDraw.GetSparkle();
            if (star == null)
                return;
            float max = Math.Max(1, player.itemAnimationMax);
            float prog = Math.Clamp(1f - player.itemAnimation / max, 0f, 1f);
            float pulse = cur.Move == MoveKind.RapidStab
                ? MathF.Abs(MathF.Sin(prog * MathF.PI * 3f))
                : MathF.Sin(prog * MathF.PI);
            float k = pre.Sparkle * pulse * Math.Clamp(cfg.TrailIntensity, 0f, 1f);
            if (k < 0.05f)
                return;
            Vector2 off = CoolerItemCore.TipOffset(tex.W, tex.H, cur.Dir, cur.Grav, cur.Scale, cur.Rot);
            float tx = cur.X + off.X, ty = cur.Y + off.Y;
            int v = (int)CoolerItemCore.JsRound(255f * MathF.Min(1f, 0.5f + k * 0.4f));
            int r = CoolerDraw.Scale(v, tint.R), g = CoolerDraw.Scale(v, tint.G), b = CoolerDraw.Scale(v, tint.B);
            float size = 0.35f + 0.45f * k;
            float originX = star.W / 2f, originY = star.H / 2f;
            CoolerDraw.PushSprite(ref drawInfo, sp, star, tx, ty, 0f, originX, originY, size, SpriteEffects.None, r, g, b);
            CoolerDraw.PushSprite(ref drawInfo, sp, star, tx, ty, MathF.PI / 2f, originX, originY, size, SpriteEffects.None, r, g, b);
            CoolerDraw.PushSprite(ref drawInfo, sp, star, tx, ty, MathF.PI / 4f, originX, originY, size * 0.6f, SpriteEffects.None,
                (int)CoolerItemCore.JsRound(r * 0.6f), (int)CoolerItemCore.JsRound(g * 0.6f), (int)CoolerItemCore.JsRound(b * 0.6f));
        }
    }
}
