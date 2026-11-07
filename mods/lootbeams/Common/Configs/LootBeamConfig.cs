using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace LootBeamsMod.Common.Configs;

public enum BeamStyleMode { Off, Simple, Arrow, AnimatedArrow }

public enum GlowStyleMode { Off, Simple, Pixel }

public class LootBeamConfig : ModConfig
{
    public static LootBeamConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Filters")]
    [DefaultValue(true)]
    public bool Enabled;

    [Range(-2, 11)]
    [Increment(1)]
    [DefaultValue(1)]
    public int MinRarity;

    // The JavaScript version showed these coin labels over values in copper. A string
    // option shows its values as they are, so the labels are the values: see MinCopper.
    [OptionStrings(new[] { "0", "1c", "1s", "5s", "10s", "50s", "1g", "5g", "10g", "50g", "1p" })]
    [DefaultValue("1c")]
    public string MinValue = "1c";

    [DefaultValue(true)]
    public bool HighlightQuest;

    [DefaultValue(true)]
    public bool HighlightExpert;

    [DefaultValue(true)]
    public bool HighlightMaster;

    [DefaultValue(true)]
    public bool UseDefaultLists;

    [Header("Visuals")]
    [DefaultValue(BeamStyleMode.Simple)]
    public BeamStyleMode BeamStyle;

    [DefaultValue(GlowStyleMode.Simple)]
    public GlowStyleMode GlowStyle;

    [Range(0.5f, 2f)]
    [Increment(0.05f)]
    [DefaultValue(0.75f)]
    public float BeamScale;

    [Range(0f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float BeamOpacity;

    [Range(0.5f, 2f)]
    [Increment(0.05f)]
    [DefaultValue(0.75f)]
    public float GlowScale;

    [Range(0f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float GlowOpacity;

    [Range(1f, 10f)]
    [Increment(0.05f)]
    [DefaultValue(7.5f)]
    public float FlashScale;

    [DefaultValue(true)]
    public bool DrawAdditive;

    [DefaultValue(true)]
    public bool Pulse;

    [Header("Extras")]
    [Range(0f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(0.35f)]
    public float EmitLight;

    [DefaultValue(true)]
    public bool FadeOnPickup;

    // MinValue in copper coins.
    public int MinCopper() => MinValue switch
    {
        "0" => 0,
        "1s" => 100,
        "5s" => 500,
        "10s" => 1000,
        "50s" => 5000,
        "1g" => 10000,
        "5g" => 50000,
        "10g" => 100000,
        "50g" => 500000,
        "1p" => 1000000,
        _ => 1,
    };
}
