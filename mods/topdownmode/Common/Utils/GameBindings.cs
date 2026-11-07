using System;
using System.Runtime.CompilerServices;
using BunnyLoader.Interop;
using Terraria;
using Terraria.Map;

namespace topdownmode.Common.Utils;

// WorldMap has no members in the C# API. These two are bound by name when the game runs, so a
// wrong name fails only there; both names are in the game's metadata.
public static unsafe class GameBindings
{
    static readonly int[] NoArguments = { };

    static MethodRef mapClear, mapLoad;

    public static void MapClear() => Call(ref mapClear, "void Clear()");

    public static void MapLoad() => Call(ref mapLoad, "void Load()");

    static void Call(ref MethodRef slot, string signature)
    {
        WorldMap map = Main.Map;
        if (map.IsNull)
            throw new InvalidOperationException("Main.Map is null");
        ref readonly MethodRef method = ref GameInterop.Method(ref slot, WorldMap.Class, signature, NoArguments);
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)map.Pointer;
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
    }
}
