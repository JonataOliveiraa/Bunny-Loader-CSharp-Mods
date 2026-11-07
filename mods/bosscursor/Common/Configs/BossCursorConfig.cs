using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace BossCursorMod.Common.Configs;

public enum CursorMode
{
    Outside,
    Always,
    Never,
}

public class BossCursorConfig : ModConfig
{
    public static BossCursorConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("DisplayHeader")]
    [DefaultValue(CursorMode.Outside)]
    public CursorMode DisplayMode;
}
