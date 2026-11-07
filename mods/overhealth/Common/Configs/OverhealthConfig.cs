using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace OverhealthMod.Common.Configs;

public class OverhealthConfig : ModConfig
{
    public static OverhealthConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Gameplay")]
    [DefaultValue(true)]
    public bool Enabled;

    [Range(10, 100)]
    [Increment(5)]
    [DefaultValue(100)]
    public int MaximumOverhealth;

    [Header("Decay")]
    [Range(10, 100)]
    [DefaultValue(13)]
    public int ConstantDecay;

    [Range(5, 20)]
    [DefaultValue(6)]
    public int ProgressiveDecay;

    [Range(10, 30)]
    [DefaultValue(10)]
    public int DecayMultiplier;

    [Header("Visuals")]
    [DefaultValue(true)]
    public bool ShowUI;

    [Range(0, 100)]
    [DefaultValue(83)]
    public int HudX;

    [Range(0, 100)]
    [DefaultValue(12)]
    public int HudY;

    [Range(50, 250)]
    [Increment(10)]
    [DefaultValue(100)]
    public int HudScale;

    [Range(20, 100)]
    [Increment(10)]
    [DefaultValue(90)]
    public int HudOpacity;

    [DefaultValue(false)]
    public bool DemoHud;
}
