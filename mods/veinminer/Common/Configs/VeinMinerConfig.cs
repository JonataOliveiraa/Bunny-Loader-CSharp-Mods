using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace VeinMinerMod.Common.Configs;

public class VeinMinerConfig : ModConfig
{
    public static VeinMinerConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("General")]
    [DefaultValue(true)]
    public bool Enabled;

    [DefaultValue(true)]
    public bool AllowOres;

    [DefaultValue(true)]
    public bool UseDefaultList;

    [DefaultValue(true)]
    public bool UsePlayerList;

    [Range(16, 2048)]
    [Increment(16)]
    [DefaultValue(256)]
    public int MaxTiles;
}
