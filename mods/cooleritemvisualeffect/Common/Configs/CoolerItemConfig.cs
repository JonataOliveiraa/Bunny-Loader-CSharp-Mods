using System.ComponentModel;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.Config;

namespace CoolerItemVisualEffectMod.Common.Configs;

// The order gives the shader's style id, so it must not change.
public enum AttackSequence { PureSlash, HeavyChop, StormySlash, TripleSlash, PureStab, CIVESword, ComplexExample }

public enum SwooshPalette { Auto, Fire, Ice, Holy, Violet, Nature, Blood, Silver, Prismatic }

public class CoolerItemConfig : ModConfig
{
    public static CoolerItemConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("SectionGeneral")]
    [DefaultValue(true)]
    public bool Enabled;

    [DefaultValue(true)]
    public bool EnableShader;

    [Range(0f, 2f)]
    [Increment(0.05f)]
    [DefaultValue(0.95f)]
    public float ShaderStrength;

    [Range(0f, 0.06f)]
    [Increment(0.002f)]
    [DefaultValue(0.022f)]
    public float ShaderDistortion;

    [Range(0f, 0.5f)]
    [Increment(0.02f)]
    [DefaultValue(0.12f)]
    public float ShaderChromatic;

    [DefaultValue(false)]
    public bool EnableTrail;

    [DefaultValue(true)]
    public bool EnableGlow;

    [DefaultValue(true)]
    public bool EnableSparkle;

    [DefaultValue(true)]
    public bool EnableHitDust;

    [Header("SectionHitbox")]
    [DefaultValue(true)]
    public bool EnableEffectHitbox;

    [DefaultValue(true)]
    public bool ReplaceVanillaHit;

    [Range(0.5f, 1.5f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float HitboxDamageScale;

    [DefaultValue(false)]
    public bool ShowHitbox;

    [Header("SectionSequence")]
    [DefaultValue(true)]
    public bool EnableCustomAttackAnimation;

    [DefaultValue(true)]
    public bool EnableMouseAim;

    [DefaultValue(true)]
    public bool EnableArmAnimation;

    [DefaultValue(AttackSequence.TripleSlash)]
    public AttackSequence Sequence;

    [Range(0.8f, 1.6f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float SwooshReach;

    [Range(1f, 1.6f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float SwooshWidthScale;

    [Range(0.7f, 1.4f)]
    [Increment(0.02f)]
    [DefaultValue(1f)]
    public float SwooshHeightScale;

    // Degrees.
    [Range(0, 90)]
    [Increment(2)]
    [DefaultValue(40)]
    public int SwooshTailBackDegrees;

    // Pixels.
    [Range(-16, 16)]
    [Increment(1)]
    [DefaultValue(0)]
    public int SwooshShiftY;

    // Ticks.
    [Range(4, 14)]
    [Increment(1)]
    [DefaultValue(10)]
    public int SwooshFadeTime;

    // Ticks.
    [Range(4, 30)]
    [Increment(1)]
    [DefaultValue(12)]
    public int TrailLength;

    [Range(1, 5)]
    [Increment(1)]
    [DefaultValue(3)]
    public int TrailQuality;

    [Range(0f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(0.8f)]
    public float TrailIntensity;

    [Range(0f, 3f)]
    [Increment(0.05f)]
    [DefaultValue(1.5f)]
    public float AlphaFactor;

    [Header("SectionColor")]
    [DefaultValue(SwooshPalette.Auto)]
    public SwooshPalette Palette;

    [DefaultValue(true)]
    public bool SpriteColor;

    [DefaultValue(true)]
    public bool UseWeaponColor;

    // #FFB066
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 176, 102, 255")]
    public Color TrailColor;

    [Header("SectionDetail")]
    [Range(0f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(0.35f)]
    public float WeaponExtraLight;

    [Range(0f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(0.35f)]
    public float DustQuantity;
}
