using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace AtmosphericTorchesMod.Common;

public class TorchConfig : ModConfig
{
    public static TorchConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(false)]
    public bool DisableTorchFlickering;

    [Range(0.0037f, 0.005f)]
    [Increment(0.0001f)]
    [DefaultValue(0.0045f)]
    public float brightnessOffset;
}
