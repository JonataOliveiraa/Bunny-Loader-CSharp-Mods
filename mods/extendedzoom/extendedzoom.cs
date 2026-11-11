using System;
using ExtendedZoomMod.Lib;
using Terraria;
using Terraria.ModLoader;

public class ExtendedZoom : Mod
{
    public static ExtendedZoom Instance = null!;

    public override void Load()
    {
        Instance = this;
        MapSmooth.Install();

        // 1) Zoom out past the game's limit. The camera's MaxPixelScale limit is read-only, and
        // pinch and the slider use whatever it returns, so changing what the getter returns moves
        // the limit. MaxWorldWidth and MaxWorldHeight go up with it.
        On_XNAWorldCameraSettings.get_MaxPixelScale += (orig, self) =>
        {
            float value = orig(self);
            return Widened ? value * Math.Max(1f, ZoomState.Factor) : value;
        };
        On_XNAWorldCameraSettings.get_MaxWorldWidth += (orig, self) => RoundedWiden(orig(self));
        On_XNAWorldCameraSettings.get_MaxWorldHeight += (orig, self) => RoundedWiden(orig(self));

        // 2) Smooth zoom. Pinch and slider requests (ApplyPixelZoom) become a TARGET, and
        // ZoomSystem walks toward it a little every frame. With the option off, or outside a
        // world, the request goes straight through as in the game.
        On_GUIZoom.ApplyPixelZoom += (orig, self, scale, bump) =>
        {
            if (Main.mapFullscreen)
            {
                ZoomState.Target = null;
                orig(self, scale, bump);
                return;
            }
            if (!ZoomState.Smooth || ZoomState.Applying || Main.gameMenu)
            {
                orig(self, scale, bump);
                return;
            }
            float current = ZoomState.CurrentZoomScale();
            if (!(current > 0) || !float.IsFinite(scale))
            {
                orig(self, scale, bump);
                return;
            }
            ZoomState.Target = scale;
            ZoomState.Bump = bump;
        };

        Logger.Info("Extended Zoom Out: limit x" + ZoomState.Factor + ", smooth zoom " + ZoomState.Smooth);
    }

    // The title screen and the menus keep the game's own limits (widening them there shrinks the menu).
    static bool Widened => !Main.gameMenu && !Main.mapFullscreen;

    static int RoundedWiden(int value)
    {
        return Widened ? (int)Math.Floor(value * (double)Math.Max(1f, ZoomState.Factor) + 0.5) : value;
    }
}
