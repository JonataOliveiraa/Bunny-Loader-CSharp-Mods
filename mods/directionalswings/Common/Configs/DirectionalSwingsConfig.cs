using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DirectionalSwingsMod.Common.Configs;

public class DirectionalSwingsConfig : ModConfig
{
    public static DirectionalSwingsConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(true)]
    public bool Enabled;

    [DefaultValue(false)]
    public bool Tools;

    [DefaultValue(true)]
    public bool LockAim;

    [Range(40, 100)]
    [Increment(10)]
    [DefaultValue(100)]
    public int Arc;
}
