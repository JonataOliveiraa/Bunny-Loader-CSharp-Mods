using System;
using System.Runtime.CompilerServices;
using BunnyLoader.Interop;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.IO;

namespace BossChecklistMod.Common;

// Game members the C# API does not expose, bound by name at run time, the way the
// JavaScript version reached them. These names only fail when the game runs, so the
// try/catch here is real: nothing the C# compiler checks stands in for it.
public static unsafe class GameBindings
{
    static readonly GameClass SacrificesClass = new("Terraria.GameContent.Creative", "ItemsSacrificedUnlocksTracker");
    static readonly int[] PlayTimeLayout = { 8 };
    static readonly int[] SacrificeLayout = { -1, -1, -1, -1 };

    static int uniqueIdOffset = -1, sacrificesOffset = -1;
    static MethodRef getPlayTime, tryGetSacrificeNumbers;
    static bool uniqueIdMissing, playTimeFailed, sacrificesFailed;

    // WorldFileData.UniqueId is a System.Guid, a type without a proxy. Records are stored
    // under this text, so it has to stay what the JavaScript version wrote.
    public static string WorldGuid()
    {
        if (!uniqueIdMissing)
        {
            try
            {
                WorldFileData data = Main.ActiveWorldFileData;
                if (!data.IsNull)
                {
                    int offset = GameInterop.Offset(ref uniqueIdOffset, WorldFileData.Class, "UniqueId");
                    Guid id = *(Guid*)(data.Pointer + offset);
                    return id.ToString().ToLowerInvariant();
                }
            }
            catch (Exception e)
            {
                uniqueIdMissing = true;
                Util.LogOnce("guid", "WorldFileData.UniqueId is not readable, using Main.worldID: " + Util.Message(e));
            }
        }
        return Main.worldID.ToString();
    }

    // PlayerFileData.GetPlayTime returns a TimeSpan, a type without a proxy.
    public static long PlayTimeTicks()
    {
        if (playTimeFailed)
            return 0;
        try
        {
            PlayerFileData data = Main.ActivePlayerFileData;
            if (data.IsNull)
                return 0;
            ref readonly MethodRef method = ref GameInterop.Method(ref getPlayTime, PlayerFileData.Class, "TimeSpan GetPlayTime()", PlayTimeLayout);
            CallFrame frame;
            Unsafe.SkipInit(out frame);
            frame.X[0] = (long)data.Pointer;
            frame.X[1] = (long)method.Info;
            frame.Ret = RetKind.Int;
            GameInterop.Call(method.Fn, &frame);
            return (long)frame.Out[0];
        }
        catch (Exception e)
        {
            playTimeFailed = true;
            Util.LogOnce("playtime", "PlayerFileData.GetPlayTime failed: " + Util.Message(e));
            return 0;
        }
    }

    // Player.creativeTracker.ItemSacrifices has no proxy either.
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
            int offset = GameInterop.Offset(ref sacrificesOffset, CreativeUnlocksTracker.Class, "ItemSacrifices");
            IntPtr sacrifices = *(IntPtr*)(tracker.Pointer + offset);
            if (sacrifices == IntPtr.Zero)
                return false;
            ref readonly MethodRef method = ref GameInterop.Method(ref tryGetSacrificeNumbers, SacrificesClass,
                "bool TryGetSacrificeNumbers(int itemId, out int amountWeHave, out int amountNeededTotal)", SacrificeLayout);
            int amountWeHave = 0, amountNeeded = 0;
            CallFrame frame;
            Unsafe.SkipInit(out frame);
            frame.X[0] = (long)sacrifices;
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
