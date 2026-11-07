using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BunnyLoader.Interop;

namespace ExtendedZoomMod.Lib;

// The zoom's shared state.
public static unsafe class ZoomState
{
    // Multiplier of the zoom-out limit (1 = normal).
    public static float Factor = 1.25f;
    // Smooth zoom is on.
    public static bool Smooth = true;
    // Pinching does not open the map (but a map opened with the button accepts pinching).
    public static bool NoPinchMap = true;
    public static bool SmoothMap = true;
    // The scale pinch or the slider asked for, still being animated (null = none).
    public static float? Target;
    // The useBump of the last request.
    public static bool Bump;
    // True while WE call ApplyPixelZoom, so the hook lets the call through.
    public static bool Applying;
    public static long Last;

    static MethodRef getZoomScale, applyPixelZoom;
    static readonly HashSet<string> Reported = new();

    public static void ReportOnce(string what, Exception e)
    {
        if (Reported.Add(what))
            ExtendedZoom.Instance.Logger.Error("Extended Zoom Out: " + what + ": " + e.Message);
    }

    public static ref readonly MethodRef ApplyMethod() =>
        ref GameInterop.Method(ref applyPixelZoom, GUIZoom.Class, "void ApplyPixelZoom(float pixelScale, bool useBump)", new[] { -1, -1, -1 });

    // The world camera's current ZoomScale, or 0 if there is no camera yet.
    public static float CurrentZoomScale()
    {
        XNAWorldCameraSettings camera = XNAUnityRunner._instance.IsNull ? default : XNAUnityRunner._instance.WorldCameraSettings;
        if (camera.IsNull)
            return 0;
        // ZoomScale is declared by the camera's base class, which has no proxy members.
        ref readonly MethodRef method = ref GameInterop.Method(ref getZoomScale, DeviceVPPICalculator.Class, "float get_ZoomScale()", new[] { -1 });
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)camera.Pointer;
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.F32;
        GameInterop.Call(method.Fn, &frame);
        return BitConverter.UInt32BitsToSingle((uint)frame.Out[0]);
    }

    // Calls the game's ApplyPixelZoom. The hook lets it through because Applying is on.
    public static void Apply(float scale, bool bump)
    {
        GUIInstance gui = GUIInstance.Active;
        if (gui.IsNull || gui.GUIZoom.IsNull)
            return;
        ref readonly MethodRef method = ref ApplyMethod();
        Applying = true;
        try
        {
            CallFrame frame = default;
            frame.X[0] = (long)gui.GUIZoom.Pointer;
            frame.X[1] = bump ? 1 : 0;
            frame.X[2] = (long)method.Info;
            frame.D[0] = BitConverter.SingleToUInt32Bits(scale);
            frame.Ret = RetKind.Int;
            GameInterop.Call(method.Fn, &frame);
        }
        finally
        {
            Applying = false;
        }
    }
}
