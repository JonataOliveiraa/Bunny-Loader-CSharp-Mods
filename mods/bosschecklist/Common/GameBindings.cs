using System;
using System.Runtime.CompilerServices;
using BunnyLoader.Interop;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.IO;

namespace BossChecklistMod.Common;

public static unsafe class GameBindings
{
    static readonly int[] SacrificeLayout = { -1, -1, -1, -1 };

    static MethodRef tryGetSacrificeNumbers;
    static bool sacrificesFailed;

    // Records are stored under this text, so it has to stay what the JavaScript version wrote.
    public static string WorldGuid()
    {
        WorldFileData data = Main.ActiveWorldFileData;
        if (!data.IsNull)
            return data.UniqueId.ToString().ToLowerInvariant();
        return Main.worldID.ToString();
    }

    public static long PlayTimeTicks()
    {
        PlayerFileData data = Main.ActivePlayerFileData;
        return data.IsNull ? 0 : data.GetPlayTime().Ticks;
    }

    // ItemsSacrificedUnlocksTracker.TryGetSacrificeNumbers has no C# proxy and is bound by
    // name at run time. That name only fails when the game runs, so the try/catch is real.
    public static bool TryGetSacrificeNumbers(Player player, int type, out int have, out int need)
    {
        have = need = 0;
        if (sacrificesFailed || player.IsNull)
            return false;
        try
        {
            CreativeUnlocksTracker tracker = player.creativeTracker;
            if (tracker.IsNull)
                return false;
            ItemsSacrificedUnlocksTracker sacrifices = tracker.ItemSacrifices;
            if (sacrifices.IsNull)
                return false;
            ref readonly MethodRef method = ref GameInterop.Method(ref tryGetSacrificeNumbers, ItemsSacrificedUnlocksTracker.Class,
                "bool TryGetSacrificeNumbers(int itemId, out int amountWeHave, out int amountNeededTotal)", SacrificeLayout);
            int amountWeHave = 0, amountNeeded = 0;
            CallFrame frame;
            Unsafe.SkipInit(out frame);
            frame.X[0] = (long)sacrifices.Pointer;
            frame.X[1] = type;
            frame.X[2] = (long)&amountWeHave;
            frame.X[3] = (long)&amountNeeded;
            frame.X[4] = (long)method.Info;
            frame.Ret = RetKind.Int;
            GameInterop.Call(method.Fn, &frame);
            have = amountWeHave;
            need = amountNeeded;
            return (frame.Out[0] & 0xFF) != 0;
        }
        catch (Exception e)
        {
            sacrificesFailed = true;
            Util.LogOnce("sacrifices", "ItemsSacrificedUnlocksTracker failed: " + Util.Message(e));
            return false;
        }
    }
}
