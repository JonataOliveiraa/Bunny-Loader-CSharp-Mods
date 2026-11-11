using System;

namespace DevToolMod.Core.Systems;

// The game's zoom lives in XNAWorldCameraSettings (a DeviceVPPICalculator).
public static class WorldCameraHooks
{
    static float current = 1;

    public static void Install()
    {
        On_XNAWorldCameraSettings.RecalculateVirtualScreen += RecalculateVirtualScreen;
        On_XNAWorldCameraSettings.ApplyCamera += ApplyCamera;
    }

    static void ApplyCamera(On_XNAWorldCameraSettings.orig_ApplyCamera orig, XNAWorldCameraSettings self)
    {
        orig(self);
        self._zoomScale = Math.Max(CameraSystem.MinZoom, Math.Min(CameraSystem.MaxZoom, CameraSystem.ZoomMultiplier));
    }

    // The game's own zoom input moves _zoomScale. Here that only says which way to go: the
    // multiplier takes a small step and stays the value in charge.
    static void RecalculateVirtualScreen(On_XNAWorldCameraSettings.orig_RecalculateVirtualScreen orig, XNAWorldCameraSettings self)
    {
        float old = self._zoomScale;
        orig(self);
        bool zoomIn = old > current;
        bool zoomOut = old < current;
        const float num = 0.02f;
        if (zoomIn) CameraSystem.ZoomMultiplier += num;
        if (zoomOut) CameraSystem.ZoomMultiplier -= num;
        if (CameraSystem.ZoomMultiplier > CameraSystem.MaxZoom) CameraSystem.ZoomMultiplier = CameraSystem.MaxZoom;
        if (CameraSystem.ZoomMultiplier < CameraSystem.MinZoom) CameraSystem.ZoomMultiplier = CameraSystem.MinZoom;
        self._zoomScale = CameraSystem.ZoomMultiplier;
        current = CameraSystem.ZoomMultiplier;
    }
}
