using System;
using System.Runtime.InteropServices;
using BunnyLoader;
using BunnyLoader.Interop;
using ExtendedZoomMod.Lib;
using Terraria;
using Terraria.ModLoader;

// The camera (XNAWorldCameraSettings) and GUIZoom have no usable members or On_ hooks in the
// C# API, so these hooks go through the loader's low-level interop with the signatures the
// JavaScript version used.
public unsafe class ExtendedZoom : Mod
{
    public static ExtendedZoom Instance = null!;

    static IntPtr* maxPixelScaleOriginal, maxWorldWidthOriginal, maxWorldHeightOriginal, applyOriginal;

    public override void Load()
    {
        Instance = this;
        MapSmooth.Install();

        // 1) Zoom out past the game's limit. The camera's MaxPixelScale limit is read-only, and
        // pinch and the slider use whatever it returns, so changing what the getter returns moves
        // the limit. MaxWorldWidth and MaxWorldHeight go up with it.
        maxPixelScaleOriginal = Widen("float get_MaxPixelScale()", (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, float>)&MaxPixelScale);
        maxWorldWidthOriginal = Widen("int get_MaxWorldWidth()", (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&MaxWorldWidth);
        maxWorldHeightOriginal = Widen("int get_MaxWorldHeight()", (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, int>)&MaxWorldHeight);

        // 2) Smooth zoom. Pinch and slider requests (ApplyPixelZoom) become a TARGET, and
        // ZoomSystem walks toward it a little every frame. With the option off, or outside a
        // world, the request goes straight through as in the game.
        try
        {
            applyOriginal = Hook.Install(ZoomState.ApplyMethod().Info,
                (IntPtr)(delegate* unmanaged<IntPtr, float, byte, IntPtr, void>)&ApplyPixelZoom).OriginalSlot;
        }
        catch (Exception e)
        {
            Logger.Error("Extended Zoom Out: could not hook ApplyPixelZoom: " + e.Message);
        }

        Logger.Info("Extended Zoom Out: limit x" + ZoomState.Factor + ", smooth zoom " + ZoomState.Smooth);
    }

    IntPtr* Widen(string signature, IntPtr replacement)
    {
        try
        {
            return Hook.Install(XNAWorldCameraSettings.Class, signature, replacement).OriginalSlot;
        }
        catch (Exception e)
        {
            Logger.Error("Extended Zoom Out: could not hook " + signature + ": " + e.Message);
            return null;
        }
    }

    static long CallGetter(IntPtr* original, IntPtr self, IntPtr method, RetKind kind)
    {
        CallFrame frame = default;
        frame.X[0] = (long)self;
        frame.X[1] = (long)method;
        frame.Ret = kind;
        GameInterop.Call(*original, &frame);
        return (long)frame.Out[0];
    }

    // The title screen and the menus keep the game's own limits (widening them there shrinks the menu).
    static bool Widened => !Main.gameMenu && !Main.mapFullscreen;

    static int RoundedWiden(int value)
    {
        return Widened ? (int)Math.Floor(value * (double)Math.Max(1f, ZoomState.Factor) + 0.5) : value;
    }

    // Exceptions must not leave these: there is no managed caller to catch them.
    [UnmanagedCallersOnly]
    static float MaxPixelScale(IntPtr self, IntPtr method)
    {
        float value = 0;
        try
        {
            value = BitConverter.UInt32BitsToSingle((uint)CallGetter(maxPixelScaleOriginal, self, method, RetKind.F32));
            return Widened ? value * Math.Max(1f, ZoomState.Factor) : value;
        }
        catch (Exception e)
        {
            ZoomState.ReportOnce("get_MaxPixelScale", e);
            return value;
        }
    }

    [UnmanagedCallersOnly]
    static int MaxWorldWidth(IntPtr self, IntPtr method)
    {
        int value = 0;
        try
        {
            value = (int)CallGetter(maxWorldWidthOriginal, self, method, RetKind.Int);
            return RoundedWiden(value);
        }
        catch (Exception e)
        {
            ZoomState.ReportOnce("get_MaxWorldWidth", e);
            return value;
        }
    }

    [UnmanagedCallersOnly]
    static int MaxWorldHeight(IntPtr self, IntPtr method)
    {
        int value = 0;
        try
        {
            value = (int)CallGetter(maxWorldHeightOriginal, self, method, RetKind.Int);
            return RoundedWiden(value);
        }
        catch (Exception e)
        {
            ZoomState.ReportOnce("get_MaxWorldHeight", e);
            return value;
        }
    }

    static void ApplyOriginal(IntPtr self, float scale, byte bump, IntPtr method)
    {
        CallFrame frame = default;
        frame.X[0] = (long)self;
        frame.X[1] = bump;
        frame.X[2] = (long)method;
        frame.D[0] = BitConverter.SingleToUInt32Bits(scale);
        frame.Ret = RetKind.Int;
        GameInterop.Call(*applyOriginal, &frame);
    }

    [UnmanagedCallersOnly]
    static void ApplyPixelZoom(IntPtr self, float scale, byte bump, IntPtr method)
    {
        try
        {
            if (Main.mapFullscreen)
            {
                ZoomState.Target = null;
                ApplyOriginal(self, scale, bump, method);
                return;
            }
            if (!ZoomState.Smooth || ZoomState.Applying || Main.gameMenu)
            {
                ApplyOriginal(self, scale, bump, method);
                return;
            }
            float current = ZoomState.CurrentZoomScale();
            if (!(current > 0) || !float.IsFinite(scale))
            {
                ApplyOriginal(self, scale, bump, method);
                return;
            }
            ZoomState.Target = scale;
            ZoomState.Bump = bump != 0;
        }
        catch (Exception e)
        {
            ZoomState.ReportOnce("ApplyPixelZoom", e);
        }
    }
}
