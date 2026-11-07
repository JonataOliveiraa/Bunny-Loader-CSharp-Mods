using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WorldGenPreviewerMod.Common.Configs;

public class WorldGenPreviewerConfig : ModConfig
{
    public static WorldGenPreviewerConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(false)]
    public bool StartWorldgenPaused;

    [DefaultValue(true)]
    public bool LivePreview;
}
