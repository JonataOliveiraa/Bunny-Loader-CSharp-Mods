using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using BossChecklistMod.Common;
using Terraria.ModLoader;
using static BossChecklistMod.Common.ModText;

public class BossChecklist : Mod
{
    static readonly Dictionary<string, int> LogTypes = new()
    {
        ["LogBoss"] = Data.BOSS,
        ["LogMiniBoss"] = Data.MINIBOSS,
        ["LogEvent"] = Data.EVENT,
    };
    static readonly string[] Submit = { "SubmitEntryLoot", "SubmitEntryCollectibles", "SubmitEntrySpawnItems", "SubmitEventNPCs" };
    static readonly string[] Old = { "AddBoss", "AddMiniBoss", "AddEvent", "AddToBossLoot", "AddToBossCollection", "AddToBossSpawnItems", "AddToEventNPCs" };
    static readonly Regex InternalName = new(@"^[\p{L}\p{N}]+\z");

    public override void Load()
    {
        Net.Install(this);
        Tracker.Init();
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI) => Util.Safe("packet", () => Net.Handle(reader, whoAmI));

    public override object? Call(params object?[] args)
    {
        try
        {
            return CallInner(args);
        }
        catch (Exception e)
        {
            Util.Log("Call Error: " + e.Message);
            return "Failure";
        }
    }

    static object? CallInner(object?[] args)
    {
        object? Arg(int i) => i < args.Length ? args[i] : null;

        string message = Arg(0) as string ?? "";
        if (message == "GetBossInfoDictionary")
        {
            if (Arg(1) is not Mod)
                throw new ArgumentException("The Mod argument for the attempted message, \"GetBossInfoDictionary\" has returned null.");
            if (!Tracker.finalized)
                Util.Log(L("LogMessage.LateCall", message));
            var all = new Dictionary<string, Dictionary<string, object?>>();
            foreach (EntryInfo e in Tracker.sorted)
                all[e.Key] = e.ToDictionary();
            return all;
        }
        if (message == "GetEntry")
        {
            EntryInfo? e = Tracker.FindEntryFromKey(Convert.ToString(Arg(1)) ?? "");
            return e?.ToDictionary();
        }
        if (message == "IsDowned")
        {
            EntryInfo? e = Tracker.FindEntryFromKey(Convert.ToString(Arg(1)) ?? "");
            return e != null && e.IsAutoDownedOrMarked;
        }
        if (Tracker.finalized)
            throw new InvalidOperationException("The attempted message, \"" + message + "\", was sent too late. BossChecklist expects Call messages up until before AddRecipes.");
        if (LogTypes.TryGetValue(message, out int type))
        {
            if (Arg(1) is not Mod mod)
            {
                Util.Log(L("LogMessage.MustContainMod", Convert.ToString(Arg(1)) ?? "null"));
                return "Failure";
            }
            string name = Convert.ToString(Arg(2)) ?? "";
            if (name.Length == 0 || !InternalName.IsMatch(name))
            {
                Util.Log(L("LogMessage.MustContainName").Replace("{internalName}", name));
                return "Failure";
            }
            Tracker.AddEntry(type, mod, name, EntryArgs.Number(Arg(3)), Arg(4), EntryArgs.ListOfInt(Arg(5)), EntryArgs.Dictionary(Arg(6)));
            return "Success";
        }
        if (message.StartsWith("Submit", StringComparison.Ordinal))
        {
            if (!Submit.Contains(message))
            {
                Util.Log("Call Error: Unknown Message: " + message);
                return "Failue";
            }
            if (Arg(1) is not Mod mod)
            {
                Util.Log(L("LogMessage.MustContainMod_Orphan", Convert.ToString(Arg(1)) ?? "null"));
                return "Failure";
            }
            Tracker.AddOrphan(message, mod, Arg(2) as IDictionary);
            return "Success";
        }
        Util.Log("Call Error: Unknown Message: " + message);
        if (Old.Any(o => message.Contains(o, StringComparison.Ordinal)))
            Util.Log(L("LogMessage.ModCallDocumentation"));
        return "Failure";
    }
}
