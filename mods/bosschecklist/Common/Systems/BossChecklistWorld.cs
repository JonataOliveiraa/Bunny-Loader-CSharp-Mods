using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BossChecklistMod.Common.Players;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static BossChecklistMod.Common.ModText;

namespace BossChecklistMod.Common.Systems;

public static class WorldData
{
    public static void SetHidden(string key, bool hide)
    {
        if (hide)
            World.hidden.Add(key);
        else
            World.hidden.Remove(key);
        BossChecklistWorld.Changed();
        if (Util.IsClient)
            Net.ToServer(PacketKind.Hide, w => WriteToggle(w, key, hide));
        else
            BossChecklistWorld.Broadcast();
    }

    public static void ClearHidden()
    {
        World.hidden.Clear();
        BossChecklistWorld.Changed();
        if (Util.IsClient)
            Net.ToServer(PacketKind.Hide, w => WriteToggle(w, null, false));
        else
            BossChecklistWorld.Broadcast();
    }

    public static void SetMarked(string key, bool mark)
    {
        if (mark)
            World.marked.Add(key);
        else
            World.marked.Remove(key);
        BossChecklistWorld.Changed();
        if (Util.IsClient)
            Net.ToServer(PacketKind.Mark, w => WriteToggle(w, key, mark));
        else
            BossChecklistWorld.Broadcast();
    }

    public static void ClearMarked()
    {
        World.marked.Clear();
        BossChecklistWorld.Changed();
        if (Util.IsClient)
            Net.ToServer(PacketKind.Mark, w => WriteToggle(w, null, false));
        else
            BossChecklistWorld.Broadcast();
    }

    // A null key clears the whole list.
    static void WriteToggle(BinaryWriter w, string? key, bool value)
    {
        w.Write(key == null);
        if (key == null)
            return;
        w.Write(key);
        w.Write(value);
    }
}

public class BossChecklistWorld : ModSystem
{
    public const byte ChatMoon = 0, ChatDespawn = 1, ChatLimb = 2;

    struct MoonSnapshot
    {
        public bool set, day, blood, snow, pumpkin, eclipse;
    }

    static readonly List<Action> changeListeners = new();
    static MoonSnapshot moon;

    public static void OnWorldDataChanged(Action fn) => changeListeners.Add(fn);

    public static void Changed()
    {
        Tracker.Invalidate();
        foreach (Action fn in changeListeners)
            Util.Safe("world change", fn);
    }

    static void WriteSnapshot(BinaryWriter w)
    {
        Net.WriteStrings(w, World.hidden);
        Net.WriteStrings(w, World.marked);
        Net.WriteStrings(w, World.downed);
    }

    public static void Broadcast()
    {
        if (Util.IsServer)
            Net.ToAll(PacketKind.World, WriteSnapshot);
    }

    public static void WriteChat(BinaryWriter w, byte kind, int type, string text)
    {
        w.Write(kind);
        w.Write(type);
        w.Write(text);
    }

    static void ReadToggle(BinaryReader r, HashSet<string> set)
    {
        if (r.ReadBoolean())
        {
            set.Clear();
        }
        else
        {
            string key = r.ReadString();
            if (r.ReadBoolean())
                set.Add(key);
            else
                set.Remove(key);
        }
        Changed();
        Broadcast();
    }

    public override void OnModLoad()
    {
        BossChecklistPlayer.InstallPlayerNet();
        Net.On(PacketKind.World, (r, _, _) =>
        {
            if (!Util.IsClient)
                return;
            World.hidden = new HashSet<string>(Net.ReadStrings(r));
            World.marked = new HashSet<string>(Net.ReadStrings(r));
            World.downed = new HashSet<string>(Net.ReadStrings(r));
            Changed();
        });
        Net.On(PacketKind.Hide, (r, _, _) =>
        {
            if (Util.IsServer)
                ReadToggle(r, World.hidden);
        });
        Net.On(PacketKind.Mark, (r, _, _) =>
        {
            if (Util.IsServer)
                ReadToggle(r, World.marked);
        });
        Net.On(PacketKind.Chat, (r, _, _) =>
        {
            if (!Util.IsClient)
                return;
            byte kind = r.ReadByte();
            int type = r.ReadInt32();
            string text = r.ReadString();
            if (kind == ChatMoon)
            {
                Fight.MoonMessage(text);
            }
            else if (kind == ChatDespawn)
            {
                string? msg = Tracker.FindBossEntryByNPC(type)?.DespawnMessage(new ShownNpc(type, text));
                if (msg != null)
                    Util.Say(Format(msg, text), Data.RarityPurple);
            }
            else if (kind == ChatLimb)
            {
                string? msg = Tracker.IsEntryLimb(type)?.LimbMessage(new ShownNpc(type, text));
                if (msg != null)
                    Util.Say(Format(msg, text), Data.RarityGreen);
            }
        });
        BossChecklistPlayer.OnJoin(who => Net.ToClient(who, PacketKind.World, WriteSnapshot));
        Cfg.OnConfigChanged(key =>
        {
            if (key == nameof(FeatureConfiguration.RecordTrackingEnabled) || key == nameof(FeatureConfiguration.AllowNewRecords))
                BossChecklistPlayer.SendAllowTracking();
            Tracker.Invalidate();
        });
        Fight.WorldChanged = () =>
        {
            Changed();
            Broadcast();
        };
    }

    public override void AddRecipes() => Util.Safe("finalize", Tracker.Finalize_);

    public override void PostAddRecipes()
    {
        if (!Tracker.finalized)
            Util.Safe("finalize", Tracker.Finalize_);
    }

    public override void ClearWorld() => Clear();

    public override void OnWorldLoad() => Clear();

    static void Clear()
    {
        World.hidden = new HashSet<string>();
        World.marked = new HashSet<string>();
        World.downed = new HashSet<string>();
        EntryUI.hiddenMode = false;
        Fight.Reset();
        Array.Clear(State.serverRecords);
        Fight.InitWorldRecords(Array.Empty<TagCompound>());
        moon = default;
        Changed();
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (Util.IsClient)
            return;
        tag["HiddenBossesList"] = World.hidden.ToList();
        tag["downed_Forced"] = World.marked.ToList();
        tag["downed"] = Data.DOWNED_FLAGS.Where(World.downed.Contains).ToList();
        List<TagCompound> records = State.worldRecords.Where(r => r.CanBeSaved).Select(r => r.Save()).ToList();
        HashSet<string> keys = records.Select(r => r.GetString("BossKey")).ToHashSet();
        foreach (WorldRecord r in State.worldRecordsUnloaded)
        {
            if (!keys.Contains(r.bossKey))
                records.Add(r.Save());
        }
        tag["World_Record_Data"] = records;
    }

    static HashSet<string> StringSet(TagCompound tag, string key)
    {
        var set = new HashSet<string>();
        if (tag.ContainsKey(key) && tag[key] is IList list)
        {
            foreach (object? value in list)
                set.Add(Convert.ToString(value) ?? "");
        }
        return set;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        World.hidden = StringSet(tag, "HiddenBossesList");
        World.marked = StringSet(tag, "downed_Forced");
        World.downed = StringSet(tag, "downed");
        World.downed.IntersectWith(Data.DOWNED_FLAGS);

        // A list when this mod saved it, but a keyed object is accepted too, as in the original.
        var saved = new List<TagCompound>();
        if (tag.ContainsKey("World_Record_Data"))
        {
            object records = tag["World_Record_Data"];
            if (records is IList list)
                saved.AddRange(list.OfType<TagCompound>());
            else if (records is TagCompound compound)
                saved.AddRange(compound.Select(entry => entry.Value).OfType<TagCompound>());
        }
        Fight.InitWorldRecords(saved);
        Changed();
    }

    public override void PostWorldLoad()
    {
        if (!Util.IsServer)
            return;
        for (int i = 0; i < 255; i++)
            Fight.ResetServerSlot(i);
    }

    public override void OnWorldUnload() => Fight.Reset();

    public override void PostUpdateNPCs() => Fight.Scan();

    public override void PreUpdateTime()
    {
        if (Util.IsClient)
            return;
        moon = new MoonSnapshot { set = true, day = Main.dayTime, blood = Main.bloodMoon, snow = Main.snowMoon, pumpkin = Main.pumpkinMoon, eclipse = Main.eclipse };
    }

    public override void PostUpdateTime()
    {
        if (Util.IsClient || !moon.set)
            return;
        bool day = Main.dayTime;
        MoonSnapshot before = moon;
        moon = default;
        if (day == before.day)
            return;
        var ended = new List<(string moonEvent, string flag)>();
        if (day)
        {
            if (before.blood)
                ended.Add(("BloodMoon", "bloodmoon"));
            if (before.snow)
                ended.Add(("FrostMoon", "frostmoon"));
            if (before.pumpkin)
                ended.Add(("PumpkinMoon", "pumpkinmoon"));
        }
        else if (before.eclipse)
        {
            ended.Add(("Eclipse", "solareclipse"));
        }
        if (ended.Count == 0)
            return;
        foreach ((string moonEvent, string flag) in ended)
        {
            Util.Safe("moon", () => Fight.AnnounceMoon(moonEvent));
            World.downed.Add(flag);
        }
        Changed();
        Broadcast();
    }
}
