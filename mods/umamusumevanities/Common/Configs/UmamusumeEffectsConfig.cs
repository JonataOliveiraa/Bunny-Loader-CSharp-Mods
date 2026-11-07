using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace umamusumevanities.Common.Configs;

public enum EffectQuality { Economy, Balanced, High, Cinematic }

public class UmamusumeEffectsConfig : ModConfig
{
    public static UmamusumeEffectsConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("EffectsHeader")]
    [DefaultValue(EffectQuality.High)]
    public EffectQuality Quality;

    [Range(0, 200)]
    [Increment(25)]
    [DefaultValue(100)]
    public int Amount;

    [DefaultValue(true)]
    public bool ExtraGlow;
}
