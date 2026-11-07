using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace DevToolMod.Common.Configs;

public class DevToolConfig : ModConfig
{
    public static DevToolConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Range(0.4f, 1.5f)]
    [Increment(0.01f)]
    [DefaultValue(1f)]
    public float UIScale;
}
