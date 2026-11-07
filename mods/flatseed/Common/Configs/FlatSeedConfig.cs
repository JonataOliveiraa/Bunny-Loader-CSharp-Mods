using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace FlatSeedMod.Common.Configs;

public class FlatSeedConfig : ModConfig
{
    public static FlatSeedConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Range(15, 35)]
    [Increment(1)]
    [DefaultValue(20)]
    public int SurfaceHeight;

    [DefaultValue(false)]
    public bool FillChasms;

    [DefaultValue(true)]
    public bool SmoothBeaches;
}
