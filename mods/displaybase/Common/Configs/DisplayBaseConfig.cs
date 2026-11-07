using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DisplayBaseMod.Common.Configs;

public class DisplayBaseConfig : ModConfig
{
    public static DisplayBaseConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Effects")]
    [DefaultValue(true)]
    public bool UseBrightness;

    [DefaultValue(true)]
    public bool UseOutline;

    [DefaultValue(true)]
    public bool UseGlitter;

    [Range(0f, 2f)]
    [Increment(0.1f)]
    [DefaultValue(0.5f)]
    public float LightIntensity;
}
