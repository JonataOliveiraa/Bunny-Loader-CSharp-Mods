using System;
using System.Collections.Generic;
using BunnyLoader;
using BunnyLoader.Interop;
using CoolerItemVisualEffectMod.Common.Configs;
using CoolerItemVisualEffectMod.Content.Effects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CoolerItemVisualEffectMod.Common.Players;

public enum MoveKind { Swoosh, Slash, Chop, ChopStrong, Cut, Stab, StabStrong, RapidStab }

public sealed record Preset(float Len, float Grow, int Halo, float Jitter, float Pulse, float Sparkle, float Light);

// An entry of a combo recipe. Size and DamageScale default to "not set": 1 and NaN.
public readonly record struct MoveDef(MoveKind Kind, int Count, float Size = 1f, float DamageScale = float.NaN);

// The move being played: a recipe entry plus its place in the repeats.
public struct ActiveMove
{
    public MoveKind Kind;
    public float Size;
    public float DamageScale;
    public int RepeatIndex;
    public int RepeatNumber;
}

// A texture with its first-frame size, cached by item type.
public sealed class ItemTexture
{
    public Texture2D Tex;
    public int W, H;
    public Rectangle Rect;
    public int Fails;
    public bool Ready;
}

// One tick of a swing. The objects live in SwingHistory and are reused.
public sealed class SwingEntry
{
    public int T;
    public float X, Y, Rot;
    public int Dir, Grav;
    public float Scale;
    public int Type;
    public float MoveSize;
    public MoveKind Move;
    public int ComboRepeat;
    public bool Brk;
}

// Newest first, at most 64 entries. A ring, so swinging does not allocate.
public sealed class SwingHistory
{
    const int Capacity = 64;
    readonly SwingEntry[] entries = new SwingEntry[Capacity];
    int newest;

    public int Count { get; private set; }

    public SwingEntry this[int index] => entries[(newest + index) % Capacity];

    public SwingEntry Push()
    {
        newest = (newest + Capacity - 1) % Capacity;
        if (Count < Capacity)
            Count++;
        return entries[newest] ??= new SwingEntry();
    }

    public void RemoveOldest() => Count--;
}

public struct HitCandidate
{
    public int Index, W, H, Type;
}

// What the attack pose remembers between ticks.
public sealed class AttackState
{
    public bool Active;
    public int Type = -1;
    public int PrevAnim;
    public int MaxAnim;
    public float AimAngle = float.NaN;
    public int AimDirection = 1;
    public float AimDistance;
    public bool AimCaptured;
    public bool ArmOverridden;
    public bool BackArmOverridden;
    public bool HasMove;
    public ActiveMove Move;
    public bool HasComboSequence;
    public AttackSequence ComboSequence;
    public int ComboItemType = -1;
    public List<MoveDef>? ComboRecipe;
    public int ComboActionIndex;
    public int ComboActionUse;
    public int ComboCycle;
    public long LastAttackTick = -1;
    // A handle does not keep its object alive between frames: this one does.
    public GameHandle? NoMeleeItem;
    public readonly int[] Hits = new int[CoolerItemCore.MaxTrackedNpcs];
    public bool HitsReady;
    public int HitCount;
    public readonly List<HitCandidate> Cand = new();
    public bool CandReady;
    public long CandTick;
}

// The swoosh the shader draws, in world coordinates. Filled once per tick by UpdateFx.
public static class CoolerItemFx
{
    public static float Alpha, Head, Span, Len, Cx, Cy;
    public static float VerticalScale = 1f, XScale = 1f, Chroma = 0.10f;
    public static float[] Ramp = { 0.2f, 0.05f, 0.02f, 1f, 0.5f, 0.15f, 1f, 0.85f, 0.55f };
    public static float Strength = 1f, Distort = 0.02f, Halo = 1f, Grow = 0.2f;
    public static int Style = 5;
    public static float Progress;
    public static bool Stab, Cut, RapidStab;
    public static float Squeeze;
    public static bool Prismatic, SpriteColor = true;
    public static float FadeTicks;

    internal static bool IdleDone, InSwing;
    internal static float PrevProg, StartAng, U, LastU, Speed, Vis;
    internal static int SwSign;
    internal static int SpeedT = -1;
    internal static int RampKey = -1;
}

public static class CoolerItemCore
{
    public const int MaxTrackedNpcs = 400;

    public static bool AttackHookEnabled;

    // Other mods may add swords here that use the vanilla slash treatment.
    public static readonly HashSet<int> ExtraSlashSwords = new();

    // Same order as AttackSequence.
    static readonly Preset[] Presets =
    {
        new(1.0f, 0.10f, 1, 0f, 0f, 1.0f, 1.0f),     // PureSlash
        new(1.2f, 0.28f, 2, 0f, 0f, 1.2f, 1.25f),    // HeavyChop
        new(1.0f, 0.12f, 2, 0.07f, 0f, 1.0f, 1.0f),  // StormySlash
        new(1.1f, 0.10f, 1, 0f, 0f, 1.0f, 1.0f),     // TripleSlash
        new(0.8f, 0.04f, 1, 0f, 0f, 1.4f, 1.0f),     // PureStab
        new(1.3f, 0.18f, 2, 0.03f, 0f, 1.3f, 1.15f), // CIVESword
        new(1.15f, 0.16f, 2, 0.04f, 0f, 1.2f, 1.1f), // ComplexExample
    };

    public static Preset GetPreset(AttackSequence sequence) => Presets[(int)sequence];

    // JS Math.round: halves go up. MathF.Round would send them to the even number.
    public static float JsRound(float v) => MathF.Floor(v + 0.5f);

    static readonly Dictionary<int, ItemTexture> textureCache = new();

    public static ItemTexture? GetItemTexture(int type)
    {
        textureCache.TryGetValue(type, out ItemTexture? entry);
        if (entry is { Ready: true })
            return entry;
        if (entry is { Fails: >= 40 })
            return null;

        if ((uint)type < (uint)TextureAssets.Item.Length)
        {
            // The indexer asks the game to load the texture when it is not loaded yet.
            Texture2D tex = TextureAssets.Item[type].Value;
            if (!tex.IsNull)
            {
                int w = tex.Width, h = tex.Height;
                if ((uint)type < (uint)Main.itemAnimations.Length)
                {
                    var anim = Main.itemAnimations[type];
                    int frames = anim.IsNull ? 1 : anim.FrameCount;
                    if (frames > 1)
                        h /= frames;
                }
                if (w > 0 && h > 0)
                {
                    entry = new ItemTexture { Tex = tex, W = w, H = h, Rect = new Rectangle(0, 0, w, h), Ready = true };
                    textureCache[type] = entry;
                    return entry;
                }
            }
        }

        entry ??= new ItemTexture();
        entry.Fails++;
        textureCache[type] = entry;
        return null;
    }

    public static bool IsVanillaSlashSword(Item item)
    {
        int type = item.type;
        if (type <= 0)
            return false;
        return type == ItemID.Excalibur || type == ItemID.TrueExcalibur || type == ItemID.NightsEdge
            || type == ItemID.TrueNightsEdge || type == ItemID.TerraBlade || ExtraSlashSwords.Contains(type);
    }

    static int forcedNoMeleeType = -1;

    public static bool IsSwingWeapon(Item item)
    {
        if (item.IsNull || item.type <= 0)
            return false;
        if (item.damage <= 0)
            return false;
        int style = item.useStyle;
        if (style != 1 && style != 3)
            return false;
        if (item.pick > 0 || item.axe > 0 || item.hammer > 0)
            return false;
        if (IsVanillaSlashSword(item))
            return true;
        if (item.noMelee && forcedNoMeleeType != item.type)
            return false;
        return !item.noUseGraphic;
    }

    internal static float SlashScaleOf(Player player, Item item)
    {
        float sc = item.scale;
        if (IsVanillaSlashSword(item))
        {
            float adj = player.GetAdjustedItemScale(item);
            if (adj > 0f)
                sc = adj;
        }
        return MathF.Max(0.1f, sc);
    }

    public static Vector2 TipOffset(int w, int h, int dir, int grav, float scale, float rot)
    {
        float lx = (dir < 0 ? -w : w) * scale;
        float ly = (grav < 0 ? h : -h) * scale;
        float c = MathF.Cos(rot), s = MathF.Sin(rot);
        return new Vector2(lx * c - ly * s, lx * s + ly * c);
    }

    static (int R, int G, int B) RarityRgb(int rare) => rare switch
    {
        -1 => (150, 160, 190),
        1 => (120, 130, 255),
        2 => (120, 255, 120),
        3 => (255, 150, 60),
        4 => (255, 110, 110),
        5 => (255, 120, 255),
        6 => (200, 130, 255),
        7 => (150, 255, 20),
        8 => (255, 230, 40),
        9 => (20, 210, 255),
        10 => (255, 50, 110),
        11 => (190, 60, 255),
        _ => (150, 185, 255), // rarity 0, and any other
    };

    static bool PaletteRgb(SwooshPalette palette, out (int R, int G, int B) rgb)
    {
        rgb = palette switch
        {
            SwooshPalette.Fire => (255, 120, 40),
            SwooshPalette.Ice => (90, 200, 255),
            SwooshPalette.Holy => (255, 225, 140),
            SwooshPalette.Violet => (190, 90, 255),
            SwooshPalette.Nature => (110, 255, 120),
            SwooshPalette.Blood => (255, 40, 70),
            SwooshPalette.Silver => (190, 200, 225),
            _ => default,
        };
        return palette != SwooshPalette.Auto && palette != SwooshPalette.Prismatic;
    }

    static void Rgb2Hsl(float r, float g, float b, out float h, out float s, out float l)
    {
        r /= 255f; g /= 255f; b /= 255f;
        float mx = MathF.Max(r, MathF.Max(g, b)), mn = MathF.Min(r, MathF.Min(g, b));
        l = (mx + mn) / 2f;
        h = 0f;
        s = 0f;
        if (mx != mn)
        {
            float d = mx - mn;
            s = l > 0.5f ? d / (2f - mx - mn) : d / (mx + mn);
            h = mx == r ? (g - b) / d + (g < b ? 6f : 0f) : mx == g ? (b - r) / d + 2f : (r - g) / d + 4f;
            h /= 6f;
        }
    }

    static float Wrap01(float v) => ((v % 1f) + 1f) % 1f;

    static float HueToChannel(float p, float q, float t)
    {
        t = Wrap01(t);
        if (t < 1f / 6f) return p + (q - p) * 6f * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
        return p;
    }

    static void Hsl2Rgb(float h, float s, float l, float[] output, int at)
    {
        h = Wrap01(h);
        if (s == 0f)
        {
            output[at] = l; output[at + 1] = l; output[at + 2] = l;
            return;
        }
        float q = l < 0.5f ? l * (1f + s) : l + s - l * s, p = 2f * l - q;
        output[at] = HueToChannel(p, q, h + 1f / 3f);
        output[at + 1] = HueToChannel(p, q, h);
        output[at + 2] = HueToChannel(p, q, h - 1f / 3f);
    }

    static void MakeRamp(int r, int g, int b, float[] ramp)
    {
        Rgb2Hsl(r, g, b, out float h, out float s, out float l);
        Hsl2Rgb(h + 0.03f, MathF.Min(1f, s * 1.05f), MathF.Max(0.08f, l * 0.32f), ramp, 0);
        Hsl2Rgb(h, MathF.Min(1f, s), MathF.Min(0.62f, MathF.Max(0.4f, l)), ramp, 3);
        Hsl2Rgb(h - 0.05f, s * 0.85f, MathF.Min(0.9f, MathF.Max(l, 0.5f) + 0.3f), ramp, 6);
    }

    public static float WrapPi(float a) => a - 2f * MathF.PI * MathF.Floor((a + MathF.PI) / (2f * MathF.PI));

    static bool TipAngle(SwingEntry e, out float ang, out float len)
    {
        ang = 0f;
        len = 0f;
        ItemTexture? tex = GetItemTexture(e.Type);
        if (tex == null)
            return false;
        Vector2 o = TipOffset(tex.W, tex.H, e.Dir, e.Grav, e.Scale, e.Rot);
        ang = MathF.Atan2(-o.Y, o.X);
        len = MathF.Sqrt(o.X * o.X + o.Y * o.Y);
        return true;
    }

    static float Clamp(float v, float min, float max) => MathF.Max(min, MathF.Min(max, v));

    internal static void UpdateFx(Player player, CoolerItemConfig cfg, SwingEntry? cur, bool swinging, Item item, int steps = 1)
    {
        int k = steps;
        if (!swinging && CoolerItemFx.IdleDone && CoolerItemFx.Alpha == 0f)
            return;
        if (player.whoAmI != Main.myPlayer)
            return;
        if (swinging)
            CoolerItemFx.IdleDone = false;
        AttackSequence sequence = cfg.Sequence;
        Preset pre = GetPreset(sequence);
        CoolerItemFx.Style = (int)sequence;

        if (swinging && cur != null)
        {
            if (TipAngle(cur, out float headAng, out float headLen))
            {
                float animMax = Math.Max(1, player.itemAnimationMax);
                float prog0 = Clamp(1f - player.itemAnimation / animMax, 0f, 1f);
                bool customPose = attackStates.TryGetValue(player.whoAmI, out AttackState? atk) && atk.Active && cfg.EnableCustomAttackAnimation;
                int dirNow = player.direction < 0 ? -1 : 1;
                float tailPad = Clamp(cfg.SwooshTailBackDegrees * MathF.PI / 180f, 0f, 1.6f);
                MoveKind curKind = cur.Move;
                int repeatIndex = Math.Max(0, cur.ComboRepeat - 1);
                float travelled = 0f;
                if (customPose)
                {
                    float dBase = GetCustomAttackAngle(0.62f, sequence, curKind, repeatIndex) - GetCustomAttackAngle(0.38f, sequence, curKind, repeatIndex);
                    if (MathF.Abs(dBase) > 0.05f)
                    {
                        int sgn = dBase > 0f ? 1 : -1;
                        CoolerItemFx.SwSign = dirNow * sgn;
                        float pStart = curKind == MoveKind.Chop || curKind == MoveKind.ChopStrong ? 0.25f : 0f;
                        travelled = MathF.Max(0f, (GetCustomAttackAngle(prog0, sequence, curKind, repeatIndex) - GetCustomAttackAngle(pStart, sequence, curKind, repeatIndex)) * sgn);
                    }
                    else if (CoolerItemFx.SwSign == 0)
                        CoolerItemFx.SwSign = dirNow;
                }
                else
                {
                    bool fresh = !CoolerItemFx.InSwing || cur.Brk || prog0 < CoolerItemFx.PrevProg - 0.5f;
                    if (fresh)
                    {
                        CoolerItemFx.StartAng = headAng;
                        CoolerItemFx.U = 0f;
                        CoolerItemFx.SwSign = dirNow;
                    }
                    float u = headAng - CoolerItemFx.StartAng;
                    u = CoolerItemFx.U + WrapPi(u - CoolerItemFx.U);
                    CoolerItemFx.U = u;
                    if (MathF.Abs(u) > 0.3f)
                        CoolerItemFx.SwSign = u > 0f ? -1 : 1;
                    travelled = MathF.Abs(u);
                }
                CoolerItemFx.InSwing = true;
                CoolerItemFx.PrevProg = prog0;

                float speed;
                if (customPose)
                {
                    float pa = MathF.Min(1f, prog0 + 0.03f), pb = MathF.Max(0f, prog0 - 0.03f);
                    speed = MathF.Abs(GetCustomAttackAngle(pa, sequence, curKind, repeatIndex) - GetCustomAttackAngle(pb, sequence, curKind, repeatIndex)) / (MathF.Max(0.01f, pa - pb) * animMax);
                }
                else
                {
                    if (CoolerItemFx.SpeedT != cur.T)
                    {
                        CoolerItemFx.SpeedT = cur.T;
                        float du = MathF.Abs(CoolerItemFx.U - CoolerItemFx.LastU);
                        CoolerItemFx.Speed = CoolerItemFx.Speed * 0.5f + du * 0.5f;
                        CoolerItemFx.LastU = CoolerItemFx.U;
                    }
                    speed = CoolerItemFx.Speed;
                }
                float lifeTicks = Clamp(cfg.SwooshFadeTime, 4f, 14f) * 1.8f;
                float windowRad = MathF.Max(0.5f, speed * lifeTicks);
                float targetVis = MathF.Max(0.22f, MathF.Min(travelled + tailPad, windowRad));
                CoolerItemFx.Vis += (targetVis - CoolerItemFx.Vis) * (k == 1 ? 0.6f : 1f - MathF.Pow(0.4f, k));
                float spanMag = MathF.Max(0.22f, CoolerItemFx.Vis);

                CoolerItemFx.Progress = prog0;
                bool stabKind = curKind == MoveKind.Stab || curKind == MoveKind.StabStrong || curKind == MoveKind.RapidStab;
                CoolerItemFx.Stab = stabKind || item.useStyle == 3;
                CoolerItemFx.Cut = curKind == MoveKind.Cut;
                CoolerItemFx.RapidStab = curKind == MoveKind.RapidStab;

                CoolerItemFx.Squeeze = 0f;
                CoolerItemFx.Head = headAng;
                CoolerItemFx.Cx = cur.X + (cur.Dir < 0 ? -2f : 2f);
                CoolerItemFx.Cy = cur.Y + cfg.SwooshShiftY;
                CoolerItemFx.Span = CoolerItemFx.Stab ? 0f : (CoolerItemFx.SwSign != 0 ? CoolerItemFx.SwSign : 1) * spanMag;
                CoolerItemFx.XScale = Clamp(cfg.SwooshWidthScale, 1f, 1.6f);
                CoolerItemFx.VerticalScale = Clamp(cfg.SwooshHeightScale, 0.7f, 1.4f);
                float baseStretch = Clamp(cfg.SwooshReach, 0.8f, 1.6f);
                float actionSize = Clamp(cur.MoveSize, 0.5f, 3f);
                float stretchScale = Clamp(baseStretch * (1f + (actionSize - 1f) * 0.3f), 0.8f, 2f);
                float swingPulse = 1f + 0.12f * MathF.Sin(MathF.PI * CoolerItemFx.Progress);
                CoolerItemFx.Grow = Clamp(0.28f + pre.Grow * 0.85f + 0.04f * MathF.Sin(MathF.PI * CoolerItemFx.Progress) - (CoolerItemFx.Cut ? 0.04f : 0f), 0.24f, 0.78f);
                CoolerItemFx.Len = headLen * stretchScale * swingPulse;
                if (CoolerItemFx.Stab)
                    CoolerItemFx.Len = headLen * baseStretch;
                if (IsVanillaSlashSword(item))
                {
                    float sizeK = Clamp(headLen / 90f, 0.85f, 1.35f);
                    CoolerItemFx.Len = headLen * stretchScale * swingPulse * (0.92f + 0.12f * sizeK);
                    CoolerItemFx.VerticalScale = Clamp(CoolerItemFx.VerticalScale * sizeK, 0.6f, 1.8f);
                }
                CoolerItemFx.FadeTicks = Clamp(cfg.SwooshFadeTime, 4f, 14f);

                SwooshPalette pal = cfg.Palette;
                CoolerItemFx.Prismatic = pal == SwooshPalette.Prismatic;
                CoolerItemFx.SpriteColor = cfg.SpriteColor && pal == SwooshPalette.Auto && cfg.UseWeaponColor;
                (int R, int G, int B) rgb;
                if (!PaletteRgb(pal, out rgb))
                    rgb = cfg.UseWeaponColor ? RarityRgb(item.rare) : (cfg.TrailColor.R, cfg.TrailColor.G, cfg.TrailColor.B);
                int key = (int)pal << 24 | rgb.R << 16 | rgb.G << 8 | rgb.B;
                if (CoolerItemFx.RampKey != key)
                {
                    CoolerItemFx.RampKey = key;
                    MakeRamp(rgb.R, rgb.G, rgb.B, CoolerItemFx.Ramp);
                }
                CoolerItemFx.Alpha = MathF.Min(1f, CoolerItemFx.Alpha + 0.4f * k);
            }
        }
        else
        {
            float fadeTicks = Clamp(cfg.SwooshFadeTime, 4f, 14f);
            CoolerItemFx.FadeTicks = fadeTicks;
            CoolerItemFx.Alpha = MathF.Max(0f, CoolerItemFx.Alpha - 1f / fadeTicks);
            CoolerItemFx.Squeeze = 0f;
            CoolerItemFx.InSwing = false;
            CoolerItemFx.Vis = MathF.Max(0f, CoolerItemFx.Vis * (1f - 1f / MathF.Max(3f, fadeTicks * 0.7f)));
            if (CoolerItemFx.Span != 0f)
                CoolerItemFx.Span = (CoolerItemFx.Span < 0f ? -1f : 1f) * CoolerItemFx.Vis;
            if (CoolerItemFx.Alpha <= 0.005f)
            {
                CoolerItemFx.Alpha = 0f;
                CoolerItemFx.Cut = false;
                CoolerItemFx.Stab = false;
                CoolerItemFx.RapidStab = false;
                CoolerItemFx.Span = 0f;
                CoolerItemFx.IdleDone = true;
            }
        }
        CoolerItemFx.Strength = MathF.Max(0f, cfg.ShaderStrength) * pre.Light;
        CoolerItemFx.Distort = MathF.Max(0f, cfg.ShaderDistortion);
        CoolerItemFx.Chroma = Clamp(cfg.ShaderChromatic, 0f, 0.5f);
        CoolerItemFx.Halo = MathF.Max(0f, cfg.TrailIntensity) * 1.10f;
    }

    static readonly MoveDef[] PureStabRecipe =
    {
        new(MoveKind.Stab, 4, DamageScale: 0.5f),
        new(MoveKind.RapidStab, 1),
        new(MoveKind.StabStrong, 2, Size: 1.2f),
    };

    static readonly MoveDef[] StormySlashRecipe =
    {
        new(MoveKind.Chop, 3, DamageScale: 1.1f),
        new(MoveKind.Slash, 4, DamageScale: 0.7f),
        new(MoveKind.ChopStrong, 1, DamageScale: 2.1f),
    };

    static readonly MoveDef[] CiveSwordRecipe =
    {
        new(MoveKind.Chop, 3),
        new(MoveKind.Slash, 4, DamageScale: 0.7f),
        new(MoveKind.ChopStrong, 1, Size: 2f),
    };

    // The JS read Main.notTheBees and Main.worldNotTheBees, which the mobile game does not have
    // (its flag is Main.notTheBeesWorld), so this branch of ComplexExample never ran there.
    static bool NotTheBeesBranch() => false;

    static List<MoveDef> BuildSequenceRecipe(AttackSequence sequence)
    {
        var recipe = new List<MoveDef>();
        switch (sequence)
        {
            case AttackSequence.PureSlash:
                recipe.Add(new(MoveKind.Slash, 2));
                break;
            case AttackSequence.HeavyChop:
                recipe.Add(new(MoveKind.Chop, 1));
                break;
            case AttackSequence.StormySlash:
                recipe.AddRange(StormySlashRecipe);
                break;
            case AttackSequence.TripleSlash:
                recipe.Add(new(MoveKind.Chop, 1));
                recipe.Add(new(MoveKind.Cut, 1));
                recipe.Add(new(MoveKind.Stab, 1));
                break;
            case AttackSequence.PureStab:
                recipe.AddRange(PureStabRecipe);
                break;
            case AttackSequence.CIVESword:
                recipe.AddRange(CiveSwordRecipe);
                if (Main.dayTime)
                    recipe.Add(new(MoveKind.Stab, 2, Size: 1.5f));
                else
                    recipe.Add(new(MoveKind.RapidStab, 1));
                break;
            case AttackSequence.ComplexExample:
                if (Main.dayTime)
                    recipe.Add(new(MoveKind.Chop, 1));
                else
                    recipe.Add(new(MoveKind.Slash, 2));
                if (Main.getGoodWorld)
                    recipe.Add(new(MoveKind.Stab, 1, Size: 3f));
                else if (NotTheBeesBranch())
                {
                    recipe.Add(new(MoveKind.Slash, 2, Size: 1.5f));
                    recipe.Add(new(MoveKind.Stab, 3));
                }
                else
                    recipe.AddRange(PureStabRecipe);
                recipe.AddRange(StormySlashRecipe);
                break;
        }
        return recipe;
    }

    static ActiveMove NextComboMove(AttackState state, AttackSequence sequence, int itemType)
    {
        long tick = Main.GameUpdateCount;
        bool changed = !state.HasComboSequence || state.ComboSequence != sequence || state.ComboItemType != itemType;
        bool expired = state.LastAttackTick >= 0 && (tick < state.LastAttackTick || tick - state.LastAttackTick > 120);
        if (changed || expired || state.ComboRecipe == null || state.ComboRecipe.Count == 0)
        {
            state.HasComboSequence = true;
            state.ComboSequence = sequence;
            state.ComboItemType = itemType;
            state.ComboRecipe = BuildSequenceRecipe(sequence);
            state.ComboActionIndex = 0;
            state.ComboActionUse = 0;
            state.ComboCycle = 0;
        }

        List<MoveDef> recipe = state.ComboRecipe;
        if (state.ComboActionIndex >= recipe.Count)
            state.ComboActionIndex = 0;
        MoveDef move = recipe[state.ComboActionIndex];
        int repeatIndex = state.ComboActionUse;
        int repeatMax = Math.Max(1, move.Count);
        var result = new ActiveMove
        {
            Kind = move.Kind,
            Size = move.Size,
            DamageScale = move.DamageScale,
            RepeatIndex = repeatIndex,
            RepeatNumber = repeatIndex + 1,
        };

        state.ComboActionUse = repeatIndex + 1;
        if (state.ComboActionUse >= repeatMax)
        {
            state.ComboActionUse = 0;
            state.ComboActionIndex++;
            if (state.ComboActionIndex >= recipe.Count)
            {
                state.ComboActionIndex = 0;
                state.ComboCycle++;
            }
        }
        state.LastAttackTick = tick;
        return result;
    }

    static float Smooth3(float v)
    {
        float x = Clamp(v, 0f, 1f);
        return x * x * (3f - 2f * x);
    }

    public static float GetCustomAttackAngle(float progress, AttackSequence style, MoveKind kind, int repeatIndex)
    {
        float p = Clamp(progress, 0f, 1f);
        float ease = Smooth3(p);
        float angle;

        if (kind == MoveKind.Stab || kind == MoveKind.StabStrong)
        {
            float amp = kind == MoveKind.StabStrong ? 0.10f : 0.065f;
            angle = amp * MathF.Sin(p * MathF.PI * 2f) * MathF.Sin(MathF.PI * p);
        }
        else if (kind == MoveKind.RapidStab)
        {
            angle = 0.085f * MathF.Sin(p * MathF.PI * 6f) * MathF.Sin(MathF.PI * p);
        }
        else if (kind == MoveKind.Cut)
        {
            angle = -0.30f - 1.18f * MathF.Cos(MathF.PI * p);
            angle += 0.035f * MathF.Sin(2f * MathF.PI * p);
        }
        else if (kind == MoveKind.Chop || kind == MoveKind.ChopStrong)
        {
            if (p < 0.25f)
            {
                angle = -2.05f - 0.18f * MathF.Sin((p / 0.25f) * MathF.PI / 2f);
            }
            else if (p < 0.77f)
            {
                float q = (p - 0.25f) / 0.52f;
                angle = -2.23f + 3.65f * Smooth3(q);
            }
            else
            {
                float r = (p - 0.77f) / 0.23f;
                angle = 1.42f - 0.16f * r;
            }
            if (kind == MoveKind.ChopStrong)
                angle += 0.08f * MathF.Sin(MathF.PI * p);
        }
        else
        {
            bool reverse = kind == MoveKind.Slash && repeatIndex % 2 == 1;
            angle = reverse ? 1.362f - 3.325f * ease : -1.963f + 3.325f * ease;
        }

        if (style == AttackSequence.StormySlash || style == AttackSequence.CIVESword)
        {
            float jitter = style == AttackSequence.StormySlash ? 0.055f : 0.025f;
            angle += jitter * MathF.Sin(p * MathF.PI * 8f) * MathF.Sin(MathF.PI * p);
        }
        return angle;
    }

    static bool ReadAimData(Player player, out float angle, out int direction, out float distance)
    {
        angle = float.NaN;
        direction = 1;
        distance = 0f;
        Vector2 mouse = Main.MouseWorld;
        Vector2 center = player.Center;
        float dx = mouse.X - center.X, dy = mouse.Y - center.Y;
        float dist = MathF.Sqrt(dx * dx + dy * dy);
        if (!float.IsFinite(dist) || dist < 40f)
            return false;
        int previousDir = player.direction < 0 ? -1 : 1;
        direction = MathF.Abs(dx) < 12f ? previousDir : (dx < 0f ? -1 : 1);
        angle = MathF.Atan2(dy, dx);
        distance = dist;
        return true;
    }

    static float GetSpriteLocalTipAngle(Item item, int dir, int grav)
    {
        int w = 1, h = 1;
        ItemTexture? tex = GetItemTexture(item.type);
        if (tex != null)
        {
            w = tex.W;
            h = tex.H;
        }
        return MathF.Atan2(grav < 0 ? h : -h, dir < 0 ? -w : w);
    }

    static float GetAimOffset(float aimAngle, Player player, Item item, int dir, AttackSequence style, in ActiveMove move)
    {
        if (!float.IsFinite(aimAngle))
            return 0f;
        int grav = player.gravDir < 0f ? -1 : 1;
        float localTipAngle = GetSpriteLocalTipAngle(item, dir, grav);
        float targetItemRotation = WrapPi(aimAngle - localTipAngle);
        float center = GetCustomAttackAngle(0.5f, style, move.Kind, move.RepeatIndex) * dir;
        return WrapPi(targetItemRotation - center);
    }

    static void ClearCustomArm(Player player, AttackState state)
    {
        RestoreNoMelee(state);
        if (state.ArmOverridden)
        {
            player.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);
            state.ArmOverridden = false;
        }
        if (state.BackArmOverridden)
        {
            player.SetCompositeArmBack(false, Player.CompositeArmStretchAmount.None, 0f);
            state.BackArmOverridden = false;
        }
    }

    static void ApplyCustomArmPose(Player player, float angle, int dir, AttackState state)
    {
        float armRotation = WrapPi(angle - MathF.PI / 2f * dir);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);
        // The item's grip follows the hand.
        player.itemLocation = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);

        float freeArmAngle = dir >= 0 ? 2.18f : MathF.PI - 2.18f;
        player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.None, freeArmAngle);
        state.ArmOverridden = true;
        state.BackArmOverridden = true;
    }

    static readonly Dictionary<int, AttackState> attackStates = new();

    public static AttackState GetAttackState(int whoAmI)
    {
        if (!attackStates.TryGetValue(whoAmI, out AttackState? state))
        {
            state = new AttackState();
            attackStates[whoAmI] = state;
        }
        return state;
    }

    public static bool TryGetAttackState(int whoAmI, out AttackState? state) => attackStates.TryGetValue(whoAmI, out state);

    static void ResetAttack(AttackState state)
    {
        state.Active = false;
        state.PrevAnim = 0;
        state.AimAngle = float.NaN;
        state.AimCaptured = false;
    }

    internal static void ApplyCustomAttackPose(Player player, int index)
    {
        if (index != Main.myPlayer)
            return;

        AttackState state = GetAttackState(player.whoAmI);
        CoolerItemConfig cfg = CoolerItemConfig.Instance;
        if (!cfg.Enabled || !cfg.EnableCustomAttackAnimation || player.dead)
        {
            ClearCustomArm(player, state);
            ResetAttack(state);
            return;
        }

        int anim = player.itemAnimation;
        if (anim <= 0)
        {
            if (state.Active || state.ArmOverridden || state.BackArmOverridden || state.NoMeleeItem != null)
                ClearCustomArm(player, state);
            ResetAttack(state);
            state.MaxAnim = 0;
            return;
        }

        Item item = player.HeldItem;
        if (!IsSwingWeapon(item))
        {
            ClearCustomArm(player, state);
            ResetAttack(state);
            return;
        }

        AttackSequence sequence = cfg.Sequence;
        int maxAnim = Math.Max(1, player.itemAnimationMax);
        int type = item.type;
        bool restarted = !state.Active || state.Type != type || (state.PrevAnim <= 2 && anim >= maxAnim - 1);

        if (restarted)
        {
            RestoreNoMelee(state);
            state.HitsReady = false;
            state.HitCount = 0;
            state.Active = true;
            state.Type = type;
            state.MaxAnim = maxAnim;
            state.Move = NextComboMove(state, sequence, type);
            state.HasMove = true;
            float aimAngle = float.NaN, aimDistance = 0f;
            int aimDirection = 1;
            bool aimed = cfg.EnableMouseAim && ReadAimData(player, out aimAngle, out aimDirection, out aimDistance);
            state.AimAngle = aimed ? aimAngle : float.NaN;
            state.AimDirection = aimed ? aimDirection : (player.direction < 0 ? -1 : 1);
            state.AimDistance = aimed ? aimDistance : 0f;
            state.AimCaptured = aimed;
        }

        state.PrevAnim = anim;
        state.MaxAnim = maxAnim;

        float progress = Clamp(1f - (float)anim / maxAnim, 0f, 1f);
        int dir = cfg.EnableMouseAim && state.AimCaptured ? state.AimDirection : (player.direction < 0 ? -1 : 1);

        if (cfg.EnableMouseAim && state.AimCaptured)
            player.direction = dir;

        ref readonly ActiveMove move = ref state.Move;
        float baseAngle = GetCustomAttackAngle(progress, sequence, move.Kind, move.RepeatIndex);
        float aimOffset = cfg.EnableMouseAim ? GetAimOffset(state.AimAngle, player, item, dir, sequence, move) : 0f;
        float angle = WrapPi(baseAngle * dir + aimOffset);

        player.itemRotation = angle;

        if (cfg.EnableArmAnimation)
            ApplyCustomArmPose(player, angle, dir, state);
        else
            ClearCustomArm(player, state);

        bool hitboxOn = cfg.EnableEffectHitbox;
        if (hitboxOn && cfg.ReplaceVanillaHit)
            ForceNoMelee(state, item);
        else
            RestoreNoMelee(state);

        if (move.Kind == MoveKind.Stab || move.Kind == MoveKind.StabStrong || move.Kind == MoveKind.RapidStab)
        {
            ItemTexture? tex = GetItemTexture(type);
            if (tex != null)
            {
                Vector2 loc0 = player.itemLocation;
                int grav0 = player.gravDir < 0f ? -1 : 1;
                Vector2 o = TipOffset(tex.W, tex.H, dir, grav0, MathF.Max(0.1f, item.scale), angle);
                float l0 = MathF.Sqrt(o.X * o.X + o.Y * o.Y);
                float ext = move.Kind == MoveKind.RapidStab
                    ? MathF.Abs(MathF.Sin(MathF.PI * 3f * progress))
                    : Smooth3(progress / 0.40f) * (1f - Smooth3((progress - 0.62f) / 0.38f));
                float pushPx = 0.30f * l0 * ext;
                if (l0 > 1f && pushPx > 0.1f)
                    player.itemLocation = new Vector2(loc0.X + o.X / l0 * pushPx, loc0.Y + o.Y / l0 * pushPx);
            }
        }

        CoolerItemPlayer mp = player.GetModPlayer<CoolerItemPlayer>();
        SwingEntry? entry = mp.Current;
        if (entry != null && entry.Type == type)
        {
            // entry is also the newest history entry, so the trail sees these values too.
            Vector2 visualLoc = player.itemLocation;
            entry.X = visualLoc.X;
            entry.Y = visualLoc.Y;
            entry.Rot = angle;
            entry.Dir = dir;
            entry.Move = move.Kind;
            entry.MoveSize = Clamp(move.Size, 0.5f, 3f);
            entry.ComboRepeat = move.RepeatNumber;
            if (restarted)
                entry.Brk = true;

            UpdateFx(player, cfg, entry, true, item, mp.FxSkipTick == mp.Tick ? 2 : 1);
            if (cfg.EnableHitDust)
                mp.EmitDust(cfg, entry);
            if (hitboxOn)
                RunEffectHitboxes(player, item, cfg, state, move, restarted);
        }
    }

    internal static void RestoreAllNoMelee()
    {
        foreach (AttackState state in attackStates.Values)
            RestoreNoMelee(state);
    }

    static void ForceNoMelee(AttackState state, Item item)
    {
        if (!item.IsNull && state.NoMeleeItem == null && !item.noMelee)
        {
            item.noMelee = true;
            state.NoMeleeItem = new GameHandle(item.Pointer);
            forcedNoMeleeType = item.type;
        }
    }

    static void RestoreNoMelee(AttackState state)
    {
        if (state.NoMeleeItem == null)
            return;
        new Item(state.NoMeleeItem.Target).noMelee = false;
        state.NoMeleeItem.Dispose();
        state.NoMeleeItem = null;
        forcedNoMeleeType = -1;
    }

    static float StabExtend()
    {
        float p = Clamp(CoolerItemFx.Progress, 0f, 1f);
        return CoolerItemFx.RapidStab
            ? MathF.Abs(MathF.Sin(MathF.PI * 3f * p))
            : Smooth3(p / 0.40f) * (1f - Smooth3((p - 0.62f) / 0.38f));
    }

    // The shape of the swoosh as seen by the hit test, frozen for this tick.
    readonly struct HitTest
    {
        readonly bool stab;
        readonly float cx, cy, r;
        // slash
        readonly float xs, ys, headMetric, spanAbs, grow10, innerHead;
        readonly bool neg;
        readonly float loPx, hiPx;
        // stab
        readonly float c, sn, tipAlong, maxHW, minHW;

        public HitTest(bool stab)
        {
            this.stab = stab;
            cx = CoolerItemFx.Cx;
            cy = CoolerItemFx.Cy;
            r = MathF.Max(1f, CoolerItemFx.Len);
            if (stab)
            {
                c = MathF.Cos(CoolerItemFx.Head);
                sn = MathF.Sin(CoolerItemFx.Head);
                float ext = StabExtend();
                tipAlong = 1f + 0.50f * ext;
                maxHW = 0.085f * (0.35f + 0.65f * ext);
                minHW = 7f / r;
                float m = MathF.Max(maxHW, minHW);
                hiPx = r * MathF.Sqrt(tipAlong * tipAlong + m * m) + 1f;
            }
            else
            {
                spanAbs = MathF.Abs(CoolerItemFx.Span);
                xs = MathF.Max(0.4f, CoolerItemFx.XScale);
                ys = MathF.Max(0.4f, CoolerItemFx.VerticalScale);
                neg = CoolerItemFx.Span < 0f;
                headMetric = MathF.Atan2(MathF.Sin(CoolerItemFx.Head) / ys, MathF.Cos(CoolerItemFx.Head) / xs);
                grow10 = 0.10f * CoolerItemFx.Grow;
                float prog = Clamp(CoolerItemFx.Progress, 0f, 1f);
                innerHead = 0.46f + (0.08f - 0.46f) * Smooth3((prog - 0.02f) / 0.30f);
                if (CoolerItemFx.Cut)
                    innerHead = MathF.Max(innerHead, 0.50f);
                loPx = MathF.Max(0.05f, 0.92f * innerHead - 0.001f) * r;
                hiPx = MathF.Min(1.25f, MathF.Max(1.04f, 1.04f * (1f + grow10)) + 0.001f) * r;
            }
        }

        public bool Contains(float px, float py) => stab ? ContainsStab(px, py) : ContainsSlash(px, py);

        bool ContainsSlash(float px, float py)
        {
            float dxm = (px - cx) / xs, dym = -(py - cy) / ys;
            float rad = MathF.Sqrt(dxm * dxm + dym * dym) / r;
            if (rad > 1.25f || rad < 0.05f)
                return false;
            float rel = WrapPi(MathF.Atan2(dym, dxm) - headMetric);
            float travel;
            if (neg)
            {
                if (rel > 0f)
                    rel -= 2f * MathF.PI;
                travel = -rel;
            }
            else
            {
                if (rel < 0f)
                    rel += 2f * MathF.PI;
                travel = rel;
            }
            if (travel > spanAbs)
                return false;
            float tt = travel / spanAbs;
            if (tt > 0.88f)
                return false;
            float outer = 1.04f * (1f + grow10 * tt);
            float inner = MathF.Min(innerHead + (0.66f - innerHead) * MathF.Pow(tt, 0.72f), outer - 0.06f);
            return rad >= inner * 0.92f && rad <= outer;
        }

        bool ContainsStab(float px, float py)
        {
            float dx = px - cx, dy = -(py - cy);
            float along = (dx * c + dy * sn) / r;
            float a = along / tipAlong;
            if (a < 0.02f || a > 1f)
                return false;
            float side = MathF.Abs(-dx * sn + dy * c) / r;
            float hw = MathF.Max(maxHW * MathF.Pow(MathF.Sin(MathF.PI * MathF.Pow(a, 0.62f)), 0.85f), minHW);
            return side <= hw;
        }

        // A cheap reject on the NPC's box before sampling points.
        public bool MayHit(float x, float y, float w, float h)
        {
            if (stab)
            {
                float nxs = cx < x ? x - cx : (cx > x + w ? cx - (x + w) : 0f);
                float nys = cy < y ? y - cy : (cy > y + h ? cy - (y + h) : 0f);
                return MathF.Sqrt(nxs * nxs + nys * nys) <= hiPx;
            }
            float x0 = (x - cx) / xs, x1 = (x + w - cx) / xs, y0 = (y - cy) / ys, y1 = (y + h - cy) / ys;
            float nx = x0 > 0f ? x0 : (x1 < 0f ? x1 : 0f), ny = y0 > 0f ? y0 : (y1 < 0f ? y1 : 0f);
            if (MathF.Sqrt(nx * nx + ny * ny) > hiPx)
                return false;
            float fx = MathF.Max(MathF.Abs(x0), MathF.Abs(x1)), fy = MathF.Max(MathF.Abs(y0), MathF.Abs(y1));
            return MathF.Sqrt(fx * fx + fy * fy) >= loPx;
        }
    }

    static bool NpcHitsRegion(float x, float y, int w, int h, in HitTest test)
    {
        if (!float.IsFinite(x) || !float.IsFinite(y) || w <= 0 || h <= 0)
            return false;
        int nx = Math.Clamp((int)MathF.Ceiling(w / 12f), 2, 6), ny = Math.Clamp((int)MathF.Ceiling(h / 12f), 2, 6);
        for (int i = 0; i <= nx; i++)
        {
            for (int j = 0; j <= ny; j++)
            {
                if (test.Contains(x + (float)w * i / nx, y + (float)h * j / ny))
                    return true;
            }
        }
        return false;
    }

    static void DebugDust(float x, float y, Color color)
    {
        int idx = Dust.NewDust(new Vector2(x - 2f, y - 2f), 4, 4, 278, 0f, 0f, 0, color, 0.6f);
        if (idx < Main.dust.Length)
        {
            Dust d = Main.dust[idx];
            d.noGravity = true;
            d.velocity = Vector2.Zero;
        }
    }

    // Marks the hitbox's outline with dust.
    static void EmitHitboxDebug(bool stab)
    {
        float r = CoolerItemFx.Len;
        var color = new Color(255, 255, 255, 0);
        if (stab)
        {
            float ext = StabExtend(), tipA = 1f + 0.5f * ext, maxHW = 0.085f * (0.35f + 0.65f * ext);
            float cos = MathF.Cos(CoolerItemFx.Head), sin = MathF.Sin(CoolerItemFx.Head);
            for (int k = 1; k <= 10; k++)
            {
                float a = k / 10f, hw = MathF.Max(maxHW * MathF.Pow(MathF.Sin(MathF.PI * MathF.Pow(a, 0.62f)), 0.85f), 7f / r);
                for (int sg = -1; sg <= 1; sg += 2)
                {
                    float al = a * tipA * r, sd = sg * hw * r;
                    DebugDust(CoolerItemFx.Cx + cos * al - sin * sd, CoolerItemFx.Cy - (sin * al + cos * sd), color);
                }
            }
        }
        else
        {
            float sg = CoolerItemFx.Span < 0f ? -1f : 1f, spanAbs = MathF.Abs(CoolerItemFx.Span);
            for (int k = 0; k <= 8; k++)
            {
                float ang = CoolerItemFx.Head + sg * spanAbs * 0.88f * k / 8f;
                float cos = MathF.Cos(ang), sin = MathF.Sin(ang);
                DebugDust(CoolerItemFx.Cx + cos * r * 0.55f * CoolerItemFx.XScale, CoolerItemFx.Cy - sin * r * 0.55f * CoolerItemFx.VerticalScale, color);
                DebugDust(CoolerItemFx.Cx + cos * r * CoolerItemFx.XScale, CoolerItemFx.Cy - sin * r * CoolerItemFx.VerticalScale, color);
            }
        }
    }

    internal static void RunEffectHitboxes(Player player, Item item, CoolerItemConfig cfg, AttackState state, in ActiveMove move, bool newAttack)
    {
        if (IsVanillaSlashSword(item))
            return;
        if (player.whoAmI != Main.myPlayer)
            return;
        if (!(CoolerItemFx.Alpha > 0.05f) || !(CoolerItemFx.Len > 4f))
            return;
        if (newAttack || !state.HitsReady)
        {
            Array.Fill(state.Hits, -1);
            state.HitsReady = true;
            state.HitCount = 0;
        }
        bool stab = CoolerItemFx.Stab;
        if (!stab && MathF.Abs(CoolerItemFx.Span) < 0.035f)
            return;

        // A rapid stab has 3 pulses, each of which can hit the same NPC once.
        int pulse = CoolerItemFx.RapidStab ? Math.Min(2, (int)MathF.Floor(Clamp(CoolerItemFx.Progress, 0f, 0.999f) * 3f)) : 0;
        long tick = Main.GameUpdateCount;
        if (cfg.ShowHitbox && tick % 3 == 0)
            EmitHitboxDebug(stab);

        if (stab && StabExtend() < 0.08f)
            return;

        var test = new HitTest(stab);
        float kbBase = item.knockBack;
        float scaleMove = float.IsFinite(move.DamageScale) ? move.DamageScale : (move.Kind == MoveKind.RapidStab ? 0.5f : 1f);
        float dmgScale = MathF.Max(0.1f, cfg.HitboxDamageScale) * scaleMove;
        int pw = player.whoAmI;
        float pcx = player.Center.X;
        float reach = CoolerItemFx.Len * 1.8f + 80f;

        // The list of nearby NPCs is rebuilt every 4 ticks.
        List<HitCandidate> cand = state.Cand;
        if (!state.CandReady || newAttack || tick - state.CandTick >= 4 || tick < state.CandTick)
        {
            cand.Clear();
            int maxN = Math.Clamp(Main.maxNPCs, 1, MaxTrackedNpcs);
            float scan = reach + 240f;
            for (int i = 0; i < maxN; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active)
                    continue;
                Vector2 ctr = npc.Center;
                int w = npc.width, h = npc.height;
                float ox = ctr.X - CoolerItemFx.Cx, oy = ctr.Y - CoolerItemFx.Cy;
                if (MathF.Sqrt(ox * ox + oy * oy) > scan + Math.Max(w, h))
                    continue;
                int type = npc.type;
                bool dummy = type == NPCID.TargetDummy;
                if (!dummy && (npc.friendly || npc.dontTakeDamage || npc.immortal))
                    continue;
                if (dummy && npc.dontTakeDamage)
                    continue;
                cand.Add(new HitCandidate { Index = i, W = w, H = h, Type = type });
            }
            state.CandReady = true;
            state.CandTick = tick;
        }

        bool statsReady = false;
        int baseDamage = 0, critChance = 0;
        float knockback = 0f;
        for (int c = 0; c < cand.Count; c++)
        {
            HitCandidate ce = cand[c];
            int i = ce.Index;
            if (state.Hits[i] == pulse)
                continue;
            NPC npc = Main.npc[i];
            if (!npc.active || npc.life <= 0 || npc.type != ce.Type)
                continue;
            Vector2 pos = npc.position;
            if (!test.MayHit(pos.X, pos.Y, ce.W, ce.H))
                continue;
            if (npc.immune[pw] > 0)
                continue;
            if (!NpcHitsRegion(pos.X, pos.Y, ce.W, ce.H, test))
                continue;

            if (!statsReady)
            {
                baseDamage = player.GetWeaponDamage(item);
                critChance = player.GetWeaponCrit(item);
                knockback = player.GetWeaponKnockback(item, kbBase);
                statsReady = true;
            }
            bool crit = Random.Shared.NextSingle() * 100f < critChance;
            int dmg = (int)MathF.Max(1f, JsRound(baseDamage * dmgScale));
            int dir = pos.X + ce.W / 2f >= pcx ? 1 : -1;
            npc.StrikeNPC(dmg, knockback, dir, crit, false, pw);
            state.Hits[i] = pulse;
            state.HitCount++;
            npc.immune[pw] = Math.Max(npc.immune[pw], 4);
        }
    }
}

public class CoolerItemPlayer : ModPlayer
{
    public readonly SwingHistory History = new();
    public int Tick;
    public SwingEntry? Current;
    public bool WasSwinging;
    public CoolerSwooshEffect? Fx;
    public int FxSkipTick = -1;

    public override void PostItemCheck()
    {
        Player player = Player;
        Tick++;
        CoolerItemConfig cfg = CoolerItemConfig.Instance;
        float maxAge = Math.Clamp(cfg.TrailLength, 4, 30) * 3.2f;

        SwingHistory hist = History;
        while (hist.Count > 0 && Tick - hist[hist.Count - 1].T > maxAge)
            hist.RemoveOldest();

        Current = null;
        if (!cfg.Enabled || player.dead)
        {
            WasSwinging = false;
            CoolerItemCore.UpdateFx(player, cfg, null, false, default);
            return;
        }

        bool swinging = player.itemAnimation > 0;
        Item item = swinging ? player.HeldItem : default;
        if (!swinging || !CoolerItemCore.IsSwingWeapon(item))
        {
            WasSwinging = false;
            CoolerItemCore.UpdateFx(player, cfg, null, false, item);
            return;
        }

        Vector2 loc = player.itemLocation;
        ActiveMove activeMove = default;
        bool hasMove = CoolerItemCore.TryGetAttackState(player.whoAmI, out AttackState? attackState) && attackState!.HasMove;
        if (hasMove)
            activeMove = attackState!.Move;

        SwingEntry entry = hist.Push();
        entry.T = Tick;
        entry.X = loc.X;
        entry.Y = loc.Y;
        entry.Rot = player.itemRotation;
        entry.Dir = player.direction < 0 ? -1 : 1;
        entry.Grav = player.gravDir < 0f ? -1 : 1;
        entry.Scale = CoolerItemCore.SlashScaleOf(player, item);
        entry.Type = item.type;
        entry.MoveSize = hasMove ? MathF.Max(0.5f, MathF.Min(3f, activeMove.Size)) : 1f;
        entry.Move = hasMove ? activeMove.Kind : MoveKind.Swoosh;
        entry.ComboRepeat = 1;
        entry.Brk = !WasSwinging;
        WasSwinging = true;
        Current = entry;

        bool mine = player.whoAmI == Main.myPlayer;
        bool customPoseOn = cfg.EnableCustomAttackAnimation && (item.useStyle == 1 || item.useStyle == 3);
        bool hookWillRun = customPoseOn && CoolerItemCore.AttackHookEnabled && mine;
        if (hookWillRun)
            FxSkipTick = Tick;
        else
            CoolerItemCore.UpdateFx(player, cfg, entry, true, item);
        if (cfg.EnableHitDust && !customPoseOn)
            EmitDust(cfg, entry);
        if (!customPoseOn && cfg.EnableEffectHitbox && mine)
        {
            var move = new ActiveMove { Kind = item.useStyle == 3 ? MoveKind.Stab : MoveKind.Swoosh, Size = 1f, DamageScale = float.NaN };
            CoolerItemCore.RunEffectHitboxes(player, item, cfg, CoolerItemCore.GetAttackState(player.whoAmI), move, entry.Brk);
        }
    }

    public override void PostUpdate()
    {
        Player player = Player;
        if (player.whoAmI != Main.myPlayer)
            return;
        CoolerItemConfig cfg = CoolerItemConfig.Instance;
        bool want = cfg.Enabled && cfg.EnableShader;
        bool alive = Fx != null && Fx.Active;
        if (want && !alive)
        {
            Fx = ScreenEffects.Spawn<CoolerSwooshEffect>(player, static fx => fx.Key = CoolerSwooshEffect.EffectKey);
        }
        else if (!want && alive)
        {
            Fx!.Kill();
            Fx = null;
        }
    }

    public void EmitDust(CoolerItemConfig cfg, SwingEntry e)
    {
        float q = Math.Clamp(cfg.DustQuantity, 0f, 1f);
        if (q <= 0.001f)
            return;
        ItemTexture? tex = CoolerItemCore.GetItemTexture(e.Type);
        if (tex == null)
            return;
        Vector2 tip = CoolerItemCore.TipOffset(tex.W, tex.H, e.Dir, e.Grav, e.Scale, e.Rot);
        Color color = cfg.UseWeaponColor
            ? new Color(255, 255, 255, 0)
            : new Color((int)cfg.TrailColor.R, cfg.TrailColor.G, cfg.TrailColor.B, 0);
        int count = (int)CoolerItemCore.JsRound(q * 2.4f);
        for (int i = 0; i < count; i++)
        {
            float along = 0.55f + Random.Shared.NextSingle() * 0.45f;
            float px = e.X + tip.X * along - 4f;
            float py = e.Y + tip.Y * along - 4f;
            int idx = Dust.NewDust(new Vector2(px, py), 8, 8, 278, 0f, 0f, 60, color, 0.6f + q * 0.6f);
            if (idx < Main.dust.Length)
                Main.dust[idx].noGravity = true;
        }
    }
}
