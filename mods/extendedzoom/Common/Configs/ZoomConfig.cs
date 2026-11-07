using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ExtendedZoomMod.Common.Configs;

public class ZoomConfig : ModConfig
{
    public static ZoomConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(true)]
    public bool FartherZoom;

    [DefaultValue(true)]
    public bool SmoothZoom;

    [DefaultValue(true)]
    public bool NoPinchOpenMap;

    [DefaultValue(true)]
    public bool SmoothMapZoom;
}
