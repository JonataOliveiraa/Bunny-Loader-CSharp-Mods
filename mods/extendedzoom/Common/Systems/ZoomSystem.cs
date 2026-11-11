using System;
using ExtendedZoomMod.Common.Configs;
using ExtendedZoomMod.Lib;
using Terraria;
using Terraria.ModLoader;

namespace ExtendedZoomMod.Common.Systems;

public class ZoomSystem : ModSystem
{
    const float Farther = 1.25f;   // zoom-out limit with "Farther Zoom" on (off = 1.0, the normal one)
    const double Speed = 12;       // the higher, the faster the zoom arrives (~90% in 0.2 s)
    const double Done = 0.002;     // close enough to snap to the target

    static int frames;

    // The game's "Pinch zoom map": the same switch makes pinching out open the map and pinching
    // inside the map zoom it. Here it follows the map: closed = off (pinching does not open the
    // map), opened with the button = on (pinching zooms the map).
    static bool? mapOriginal;   // the game's value before we touched it
    static bool mapManaged, mapBroken;

    static void ManagePinchMap()
    {
        if (Main.gameMenu || mapBroken)
            return;
        try
        {
            GUIInstance gui = GUIInstance.Active;
            if (gui.IsNull || gui.GUIZoom.IsNull)
                return;
            ref bool enabled = ref gui.GUIZoom.PinchZoomMapEnabled;
            if (!ZoomState.NoPinchMap)
            {
                if (mapManaged && mapOriginal != null)
                {
                    enabled = mapOriginal.Value;
                    mapManaged = false;
                }
                return;
            }
            if (mapOriginal == null)
            {
                mapOriginal = enabled;
                ExtendedZoom.Instance.Logger.Info("Extended Zoom Out: initial PinchZoomMapEnabled=" + mapOriginal + ", mapFullscreen=" + Main.mapFullscreen);
            }
            bool wanted = Main.mapFullscreen;
            if (enabled != wanted)
                enabled = wanted;
            mapManaged = true;
        }
        catch (Exception e)
        {
            mapBroken = true;   // once only: no per-frame logging
            ExtendedZoom.Instance.Logger.Error("Extended Zoom Out: map control turned off after an error: " + e.Message);
        }
    }

    // One step of the smooth zoom: walks a fraction of the way to the target, whatever the frame rate.
    static void Step()
    {
        long now = Environment.TickCount64;
        double dt = Math.Min(0.1, Math.Max(0, (now - ZoomState.Last) / 1000.0));
        ZoomState.Last = now;
        // Fullscreen map open: never animate the world camera (the map's pinch zoom uses its own scale).
        if (Main.mapFullscreen)
        {
            ZoomState.Target = null;
            return;
        }
        if (ZoomState.Target == null)
            return;

        float current = ZoomState.CurrentZoomScale();
        float target = ZoomState.Target.Value;
        if (!ZoomState.Smooth || Math.Abs(target - current) < Done)
        {
            ZoomState.Target = null;
            ZoomState.Apply(target, ZoomState.Bump);
            return;
        }
        ZoomState.Apply((float)(current + (target - current) * (1 - Math.Exp(-dt * Speed))), false);
        // The game limits the scale (limit, edge): if it did not move, the target cannot be reached.
        if (Math.Abs(ZoomState.CurrentZoomScale() - current) < 1e-5)
            ZoomState.Target = null;
    }

    public override void PostUpdateEverything()
    {
        Step();
        ManagePinchMap();
        if (frames++ % 20 != 0)
            return;
        ZoomConfig config = ZoomConfig.Instance;
        float factor = config.FartherZoom ? Farther : 1;
        if (factor != ZoomState.Factor)
        {
            ZoomState.Factor = factor;
            Mod.Logger.Info("Extended Zoom Out: limit x" + factor);
        }
        ZoomState.NoPinchMap = config.NoPinchOpenMap;
        ZoomState.SmoothMap = config.SmoothMapZoom;
        if (config.SmoothZoom != ZoomState.Smooth)
        {
            ZoomState.Smooth = config.SmoothZoom;
            Mod.Logger.Info("Extended Zoom Out: smooth zoom " + ZoomState.Smooth);
        }
    }
}
