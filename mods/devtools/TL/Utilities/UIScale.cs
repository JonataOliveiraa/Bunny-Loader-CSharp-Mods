using DevToolMod.Common.Configs;
using Terraria;

namespace DevToolMod.TL.Utilities;

// The overall scale of the mod's interface, from UIScale in the mod config. Sizes used to be
// "texture pixels x Main.UIScale", while panels used the raw texture size. A single factor on
// top scales panels, buttons, spacing and text together, so the proportions hold: scaling
// only the buttons would misplace them inside the panels.
public static class UIScaleHelper
{
    // Fallback when the config holds no usable value (the 2/3 of an earlier version).
    public const float DEFAULT_SCALE = 2f / 3f;

    public static float MenuScale()
    {
        float raw = DevToolConfig.Instance.UIScale;
        return raw > 0 ? raw : DEFAULT_SCALE;
    }

    // Sizes that follow the game's UI scale, such as buttons and spacing.
    public static float UIScale() => Main.UIScale * MenuScale();

    // Sizes that do not follow it: raw texture sizes of panels, hard-coded sizes, text scale.
    public static float S(float value) => value * MenuScale();
}
