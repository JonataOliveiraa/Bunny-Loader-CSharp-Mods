using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace GnorpBuilderMod.Common;

public enum AnchorPoint { BottomCenter, TopLeft, TopRight, BottomLeft, BottomRight }

public class GnorpConfig : ModConfig
{
    public static GnorpConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(true)]
    public bool ImagePreview;

    [DefaultValue(false)]
    public bool EnablePreviews;

    [Range(0, 100)]
    [Increment(5)]
    [DefaultValue(50)]
    public int PreviewOpacity;

    [DefaultValue(false)]
    public bool RequireDoubleClickPlacement;

    [Dropdown]
    [DefaultValue(AnchorPoint.BottomCenter)]
    public AnchorPoint Anchor;
}
