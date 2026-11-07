using System;
using System.Runtime.CompilerServices;
using BunnyLoader.Interop;
using Terraria.ModLoader;

namespace BetterReforgeMod.Common.UI;

// The C# API has no Reforge_Layout, so its one property is bound by name at run time,
// the way the JavaScript version reached it. The names only fail at run time, so
// here the try/catch is needed: without the property, the panel stays on the left page.
public static unsafe class ReforgeLayout
{
    static readonly GameClass LayoutClass = new("", "Reforge_Layout");
    static readonly int[] GetterLayout = { -1 };

    static MethodRef getInstance, getRightPage;
    static bool failed;

    public static bool RightPageController
    {
        get
        {
            if (failed)
                return false;
            try
            {
                ref readonly MethodRef instanceGetter = ref GameInterop.Method(ref getInstance, LayoutClass, "Reforge_Layout get_Instance()", GetterLayout);
                CallFrame frame;
                Unsafe.SkipInit(out frame);
                frame.X[0] = (long)instanceGetter.Info;
                frame.Ret = RetKind.Int;
                GameInterop.Call(instanceGetter.Fn, &frame);
                long layout = (long)frame.Out[0];
                if (layout == 0)
                    return false;

                ref readonly MethodRef rightPageGetter = ref GameInterop.Method(ref getRightPage, LayoutClass, "bool get_RightPageController()", GetterLayout);
                frame.X[0] = layout;
                frame.X[1] = (long)rightPageGetter.Info;
                frame.Ret = RetKind.Int;
                GameInterop.Call(rightPageGetter.Fn, &frame);
                return (frame.Out[0] & 1) != 0;
            }
            catch (Exception e)
            {
                failed = true;
                ModContent.GetInstance<BetterReforge>().Logger.Warn("Reforge_Layout.RightPageController could not be read: " + e.Message);
                return false;
            }
        }
    }
}
