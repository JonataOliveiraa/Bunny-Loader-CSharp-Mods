using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace lightsandshadows.Common.Configs;

public enum DayColorMode { Classic, Alternate }

public enum ShadowQuality { High, Medium, Low }

public class LightsConfig : ModConfig
{
    public static LightsConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Lighting")]
    [DefaultValue(true)]
    public bool UseLight;

    [Range(0.4f, 1.5f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float LightIntensity;

    [Range(0.4f, 1.1f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float MoonLightIntensity;

    [Range(0.4f, 1.5f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float ShadowIntensity;

    [DefaultValue(DayColorMode.Classic)]
    public DayColorMode DayColor;

    [Header("Bloom")]
    [DefaultValue(true)]
    public bool UseBloom;

    [Range(0.2f, 1.25f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float BloomIntensity;

    [Range(1, 10)]
    [Increment(1)]
    [DefaultValue(4)]
    public int BloomCounts;

    [Header("Performance")]
    [DefaultValue(ShadowQuality.High)]
    public ShadowQuality Quality;
}
