using System;
using System.Runtime.InteropServices;
using BunnyLoader;
using BunnyLoader.Interop;

namespace DevToolMod.Core.Systems;

// The game's zoom lives in XNAWorldCameraSettings (a DeviceVPPICalculator), a type that has no
// members or On_ hooks in the C# API. So these two hooks and the field go through the loader's
// low-level interop. If the API gains them, this file can become two On_ hooks.
public static unsafe class WorldCameraHooks
{
    static Hook? applyCamera;
    static Hook? recalculateVirtualScreen;
    static int zoomScaleOffset = -1;
    static float current = 1;

    public static void Install()
    {
        try
        {
            recalculateVirtualScreen = Hook.Install(XNAWorldCameraSettings.Class, "void RecalculateVirtualScreen()",
                (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&RecalculateVirtualScreen);
            applyCamera = Hook.Install(XNAWorldCameraSettings.Class, "void ApplyCamera()",
                (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&ApplyCamera);
        }
        catch (Exception e)
        {
            DevTool.Instance.Logger.Error("DevTool: the zoom hooks could not be installed: " + e.Message);
        }
    }

    static ref float ZoomScale(IntPtr self) =>
        ref *(float*)(self + GameInterop.Offset(ref zoomScaleOffset, XNAWorldCameraSettings.Class, "_zoomScale"));

    static void Original(Hook hook, IntPtr self, IntPtr method)
    {
        CallFrame frame = default;
        frame.X[0] = (long)self;
        frame.X[1] = (long)method;
        frame.Ret = RetKind.Int;
        GameInterop.Call(hook.Original, &frame);
    }

    // Exceptions must not leave these two: there is no managed caller to catch them.
    [UnmanagedCallersOnly]
    static void ApplyCamera(IntPtr self, IntPtr method)
    {
        try
        {
            Hook? hook = applyCamera;
            if (hook == null) return;
            Original(hook, self, method);
            ZoomScale(self) = Math.Max(CameraSystem.MinZoom, Math.Min(CameraSystem.MaxZoom, CameraSystem.ZoomMultiplier));
        }
        catch (Exception e)
        {
            DevTool.Instance.Logger.Error("DevTool: ApplyCamera: " + e.Message);
        }
    }

    // The game's own zoom input moves _zoomScale. Here that only says which way to go: the
    // multiplier takes a small step and stays the value in charge.
    [UnmanagedCallersOnly]
    static void RecalculateVirtualScreen(IntPtr self, IntPtr method)
    {
        try
        {
            Hook? hook = recalculateVirtualScreen;
            if (hook == null) return;
            float old = ZoomScale(self);
            Original(hook, self, method);
            bool zoomIn = old > current;
            bool zoomOut = old < current;
            const float num = 0.02f;
            if (zoomIn) CameraSystem.ZoomMultiplier += num;
            if (zoomOut) CameraSystem.ZoomMultiplier -= num;
            if (CameraSystem.ZoomMultiplier > CameraSystem.MaxZoom) CameraSystem.ZoomMultiplier = CameraSystem.MaxZoom;
            if (CameraSystem.ZoomMultiplier < CameraSystem.MinZoom) CameraSystem.ZoomMultiplier = CameraSystem.MinZoom;
            ZoomScale(self) = CameraSystem.ZoomMultiplier;
            current = CameraSystem.ZoomMultiplier;
        }
        catch (Exception e)
        {
            DevTool.Instance.Logger.Error("DevTool: RecalculateVirtualScreen: " + e.Message);
        }
    }
}
