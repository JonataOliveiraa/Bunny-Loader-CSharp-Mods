using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace MenuMediaPort.Common.Configs;

// Settings > Mod Configuration > Menu Media: the picture's size, position, icon size and GIF playback speed.
// Read live by Lib/Media.cs, so Apply takes effect right away on the menu.
public class MenuMediaConfig : ModConfig
{
    public static MenuMediaConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("PictureHeader")]
    [Range(50, 300)]
    [Increment(5)]
    [DefaultValue(115)]
    public int Size;

    [Range(-1000, 1000)]
    [Increment(10)]
    [DefaultValue(0)]
    public int OffsetX;

    [Range(-1000, 1000)]
    [Increment(10)]
    [DefaultValue(0)]
    public int OffsetY;

    [Range(10, 500)]
    [Increment(5)]
    [DefaultValue(100)]
    public int IconSize;

    [Range(0, 60)]
    [Increment(1)]
    [DefaultValue(0)]
    public int Fps;
}
