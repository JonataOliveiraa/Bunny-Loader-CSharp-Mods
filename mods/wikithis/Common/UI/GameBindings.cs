using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BunnyLoader;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace WikithisMod.Common.UI;

// Game members the C# API does not expose, bound by name at run time, the way the
// JavaScript version reached them. These names only fail when the game runs, so the
// try/catch here is real: nothing the C# compiler checks stands in for it.
public static unsafe class GameBindings
{
    static readonly GameClass LayoutClass = new("", "InventorySplitStack_Layout");
    static readonly GameClass ApplicationClass = new("UnityEngine", "Application");
    static readonly int[] DrawLayout = { -1 };
    static readonly int[] OpenUrlLayout = { -1, -1 };

    static MethodRef splitStackDraw, openUrl, getLayoutInstance;
    static int mainButtonOffset = -1, anchorControlOffset = -1, anchorOffset = -1, locationOffset = -1;
    static GameClass? buttonClass;
    static Hook? splitStackHook;
    static Action afterSplitStackDraw = null!;
    static bool errorReported;

    public static void InstallSplitStackHook(Action afterDraw)
    {
        afterSplitStackDraw = afterDraw;
        ref readonly MethodRef method = ref GameInterop.Method(ref splitStackDraw, GUIInventorySplitStack.Class, "void Draw()", DrawLayout);
        splitStackHook = Hook.Install(method.Info, (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&SplitStackDraw);
    }

    static void Report(Exception e)
    {
        if (errorReported)
            return;
        errorReported = true;
        ModContent.GetInstance<Wikithis>().Logger.Error("The wiki slot failed: " + e);
    }

    // An exception must never leave this method: the game's frames are above it.
    [UnmanagedCallersOnly]
    static void SplitStackDraw(IntPtr self, IntPtr methodInfo)
    {
        try
        {
            CallFrame frame;
            Unsafe.SkipInit(out frame);
            frame.X[0] = (long)self;
            frame.X[1] = (long)splitStackDraw.Info;
            frame.Ret = RetKind.Int;
            GameInterop.Call(*splitStackHook!.OriginalSlot, &frame);
        }
        catch (Exception e)
        {
            Report(e);
        }

        try
        {
            afterSplitStackDraw();
        }
        catch (Exception e)
        {
            Report(e);
        }
    }

    // InventorySplitStack_Layout.Instance.MainButton, as a position on the screen.
    public static Vector2 MainButtonPosition()
    {
        // Instance is a property on this class, not a field as on the classes the API exposes.
        ref readonly MethodRef instanceGetter = ref GameInterop.Method(ref getLayoutInstance, LayoutClass, "InventorySplitStack_Layout get_Instance()", DrawLayout);
        CallFrame getterFrame;
        Unsafe.SkipInit(out getterFrame);
        getterFrame.X[0] = (long)instanceGetter.Info;
        getterFrame.Ret = RetKind.Int;
        GameInterop.Call(instanceGetter.Fn, &getterFrame);
        IntPtr layout = (IntPtr)(long)getterFrame.Out[0];
        if (layout == IntPtr.Zero)
            throw new InvalidOperationException("InventorySplitStack_Layout.Instance is null");
        IntPtr button = *(IntPtr*)(layout + GameInterop.Offset(ref mainButtonOffset, LayoutClass, "MainButton"));
        if (button == IntPtr.Zero)
            throw new InvalidOperationException("InventorySplitStack_Layout.MainButton is null");

        buttonClass ??= new GameClass("", GameInterop.ClassNameOf(button));
        var anchorControl = *(ControlAnchor.ControlId*)(button + GameInterop.Offset(ref anchorControlOffset, buttonClass, "AnchorControl"));
        var anchor = *(LayoutCalculator.AnchorType*)(button + GameInterop.Offset(ref anchorOffset, buttonClass, "Anchor"));
        Vector2 location = *(Vector2*)(button + GameInterop.Offset(ref locationOffset, buttonClass, "Location"));
        return LayoutCalculator.GetAnchoredPosition(anchorControl, anchor, location);
    }

    // UnityEngine.Application.OpenURL.
    public static void OpenUrl(string url)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref openUrl, ApplicationClass, "void OpenURL(string url)", OpenUrlLayout);
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)GameString.New(url);
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
    }
}
