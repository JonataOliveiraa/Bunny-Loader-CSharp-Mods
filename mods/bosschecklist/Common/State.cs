using System.Collections.Generic;
using BossChecklistMod.Common.Players;
using Terraria;

namespace BossChecklistMod.Common;

public static class State
{
    public static readonly List<PersonalRecords>?[] serverRecords = new List<PersonalRecords>?[256];
    public static List<WorldRecord> worldRecords = new();
    public static List<WorldRecord> worldRecordsUnloaded = new();
    public static readonly bool[] allowTracking = new bool[256];
    public static readonly bool[] allowNew = new bool[256];
    public static int dirty;

    public static void Touch() => dirty++;

    public static BossChecklistPlayer? Me
    {
        get
        {
            Player p = Util.Local;
            if (p.IsNull)
                return null;
            try
            {
                return p.GetModPlayer<BossChecklistPlayer>();
            }
            catch (System.Exception e)
            {
                Util.LogOnce("me", "the local player has no records yet: " + Util.Message(e));
                return null;
            }
        }
    }
}
