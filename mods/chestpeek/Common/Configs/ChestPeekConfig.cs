using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ChestPeekMod.Common.Configs;

public class ChestPeekConfig : ModConfig
{
    public static ChestPeekConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(true)]
    public bool Enabled;

    [Range(100, 220)]
    [Increment(10)]
    [DefaultValue(130)]
    public int Scale;
}
