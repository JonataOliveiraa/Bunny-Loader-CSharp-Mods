using System;
using System.Runtime.CompilerServices;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace WikithisMod.Common.UI;

// UnityEngine.Application has no C# proxy, so OpenURL is bound by name at run time, the way
// the JavaScript version reached it. That name only fails when the game runs.
public static unsafe class GameBindings
{
    static readonly GameClass ApplicationClass = new("UnityEngine", "Application");
    static readonly int[] OpenUrlLayout = { -1, -1 };

    static MethodRef openUrl;
    static bool errorReported;

    public static void InstallSplitStackHook(Action afterDraw)
    {
        On_GUIInventorySplitStack.Draw += (orig, self) =>
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Report(e);
            }

            try
            {
                afterDraw();
            }
            catch (Exception e)
            {
                Report(e);
            }
        };
    }

    static void Report(Exception e)
    {
        if (errorReported)
            return;
        errorReported = true;
        ModContent.GetInstance<Wikithis>().Logger.Error("The wiki slot failed: " + e);
    }

    // InventorySplitStack_Layout.Instance.MainButton, as a position on the screen.
    public static Vector2 MainButtonPosition()
    {
        InventorySplitStack_Layout layout = InventorySplitStack_Layout.Instance;
        if (layout.IsNull)
            throw new InvalidOperationException("InventorySplitStack_Layout.Instance is null");
        ref TransactionButton_Layout button = ref layout.MainButton;
        if (button.IsNull)
            throw new InvalidOperationException("InventorySplitStack_Layout.MainButton is null");
        return LayoutCalculator.GetAnchoredPosition(button.AnchorControl, button.Anchor, button.Location);
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
