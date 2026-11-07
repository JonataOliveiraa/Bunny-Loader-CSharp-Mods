using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using static BossChecklistMod.Common.ModText;

namespace BossChecklistMod.Common.Players;

public class BossChecklistPlayer : ModPlayer
{
    const int ScanEvery = 30;

    public bool hasOpenedTheBossLog, enteredWorldReset;
    public HashSet<int> obtained = new();
    public Dictionary<string, int> miniKills = new();
    public bool[] hasNewRecord = Array.Empty<bool>();
    public int newRecordState;

    List<object> pendingKeys = new();
    Dictionary<string, List<PersonalRecords>> stored = new();
    List<PersonalRecords>? current;
    int scan;

    static Dictionary<string, int>? keyMap;

    static object ItemKey(int type)
    {
        if (type < ItemID.Count)
            return type;
        ModItem? m = ModContent.GetModItem(type);
        return m != null ? m.Mod.Name + "/" + m.Name : type;
    }

    static int TypeOfKey(object key)
    {
        if (key is not string text)
        {
            int type = Convert.ToInt32(key);
            return type < ItemID.Count ? type : -1;
        }
        if (keyMap == null)
        {
            keyMap = new Dictionary<string, int>();
            foreach (int t in Tracker.lootCache)
            {
                if (t >= ItemID.Count)
                    keyMap[Convert.ToString(ItemKey(t)) ?? ""] = t;
            }
        }
        return keyMap.TryGetValue(text, out int found) ? found : -1;
    }

    public override void Initialize()
    {
        hasOpenedTheBossLog = false;
        enteredWorldReset = false;
        obtained = new HashSet<int>();
        pendingKeys = new List<object>();
        stored = new Dictionary<string, List<PersonalRecords>>();
        current = null;
        miniKills = new Dictionary<string, int>();
        hasNewRecord = Array.Empty<bool>();
        newRecordState = 0;
        scan = 0;
    }

    // Filled when entering a world, so the per-frame hooks do not look it up by name.
    public List<PersonalRecords>? RecordsForWorld => current;

    public override void SaveData(TagCompound tag)
    {
        ResolvePending();
        tag.Add("BossLogPrompt", hasOpenedTheBossLog);
        var loot = new List<object>();
        foreach (int type in obtained)
            loot.Add(ItemKey(type));
        loot.AddRange(pendingKeys);
        tag.Add("BossLootObtained", loot);
        var records = new TagCompound();
        foreach ((string guid, List<PersonalRecords> list) in stored)
        {
            var world = new TagCompound();
            foreach (PersonalRecords r in list)
            {
                if (r.CanBeSaved)
                    world[r.bossKey] = r.Save();
            }
            records[guid] = world;
        }
        tag.Add("Record_Data", records);
        var kills = new TagCompound();
        foreach ((string key, int count) in miniKills)
            kills[key] = count;
        tag.Add("MiniBossKillCount", kills);
    }

    public override void LoadData(TagCompound tag)
    {
        hasOpenedTheBossLog = tag.GetBool("BossLogPrompt");
        obtained = new HashSet<int>();
        pendingKeys = new List<object>();
        foreach (object key in tag.GetList<object>("BossLootObtained"))
        {
            if (key is string)
                pendingKeys.Add(key);
            else
                obtained.Add(Convert.ToInt32(key));
        }
        stored = new Dictionary<string, List<PersonalRecords>>();
        foreach ((string guid, object value) in tag.GetCompound("Record_Data"))
        {
            var list = new List<PersonalRecords>();
            if (value is TagCompound world)
            {
                foreach ((string bossKey, object record) in world)
                {
                    if (record is TagCompound data)
                        list.Add(PersonalRecords.From(bossKey, data));
                }
            }
            stored[guid] = list;
        }
        miniKills = new Dictionary<string, int>();
        TagCompound kills = tag.GetCompound("MiniBossKillCount");
        foreach ((string key, object _) in kills)
            miniKills[key] = kills.Get<int>(key);
    }

    void ResolvePending()
    {
        if (pendingKeys.Count == 0 || !Tracker.finalized)
            return;
        var rest = new List<object>();
        foreach (object key in pendingKeys)
        {
            int type = TypeOfKey(key);
            if (type > 0)
                obtained.Add(type);
            else
                rest.Add(key);
        }
        pendingKeys = rest;
    }

    bool IsLocal => Player.whoAmI == Main.myPlayer && !Util.IsServer;

    public override void OnEnterWorld()
    {
        if (Player.whoAmI != Main.myPlayer)
            return;
        Util.Safe("enter world", EnterWorld);
    }

    void EnterWorld()
    {
        ResolvePending();
        string guid = Records.WorldGuid();
        // The original fell back to the world's number when it could not read the GUID.
        if (!stored.TryGetValue(guid, out List<PersonalRecords>? saved))
            stored.Remove(Main.worldID.ToString(), out saved);
        List<PersonalRecords> known = Tracker.recordKeys
            .Select(k => saved?.Find(r => r.bossKey == k) ?? new PersonalRecords(k))
            .ToList();
        foreach (PersonalRecords r in known)
            r.Abort();
        // Records of bosses from mods that are off stay after the known ones, so indexes match recordKeys.
        List<PersonalRecords> all = known.ToList();
        if (saved != null)
            all.AddRange(saved.Where(r => !Tracker.recordKeys.Contains(r.bossKey)));
        stored[guid] = all;
        current = all;
        hasNewRecord = new bool[Tracker.recordKeys.Count];
        newRecordState = 0;
        enteredWorldReset = true;
        if (Util.IsClient)
        {
            bool track = Cfg.Tracking, allow = Cfg.NewRecords;
            Net.ToServer(PacketKind.Join, w =>
            {
                Net.WriteStrings(w, Tracker.recordKeys);
                foreach (PersonalRecords r in known)
                {
                    w.Write(r.kills);
                    w.Write(r.durationBest);
                    w.Write(r.hitsTakenBest);
                }
                w.Write(track);
                w.Write(allow);
            });
        }
        State.allowTracking[Main.myPlayer] = Cfg.Tracking;
        State.allowNew[Main.myPlayer] = Cfg.NewRecords;
        foreach (Action fn in enterListeners)
            Util.Safe("enter", fn);
    }

    List<PersonalRecords>? ListFor()
    {
        if (Util.IsServer)
            return State.serverRecords[Player.whoAmI];
        if (Util.IsClient && Player.whoAmI != Main.myPlayer)
            return null;
        return RecordsForWorld;
    }

    public override void PreUpdate()
    {
        List<PersonalRecords>? list = ListFor();
        if (list != null)
        {
            foreach (PersonalRecords r in list)
            {
                if (r.tracking)
                    r.trackDuration++;
            }
        }
        if (IsLocal && ++scan % ScanEvery == 0)
            ScanInventorySafely();
    }

    void ScanInventorySafely()
    {
        try
        {
            ScanInventory();
        }
        catch (Exception e)
        {
            Util.LogOnce("obtained scan", "obtained scan failed: " + Util.Message(e));
        }
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        List<PersonalRecords>? list = ListFor();
        if (list == null)
            return;
        foreach (PersonalRecords r in list)
        {
            if (r.tracking)
                r.trackHits++;
        }
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        List<PersonalRecords>? list = ListFor();
        if (list == null)
            return;
        foreach (PersonalRecords r in list)
        {
            if (r.tracking)
                r.trackDeaths++;
        }
    }

    public override void UpdateDead()
    {
        if (!IsLocal || !Cfg.Feature.TimerSounds)
            return;
        int t = Player.respawnTimer;
        if (t > 0 && t <= 180 && t % 60 == 0)
            Util.PlaySound(SoundID.MaxMana);
    }

    public override bool OnPickup(WorldItem item)
    {
        if (IsLocal)
            Obtain(item.type);
        return true;
    }

    public override void PlayerDisconnect()
    {
        if (!Util.IsServer)
            return;
        List<PersonalRecords>? list = State.serverRecords[Player.whoAmI];
        if (list == null)
            return;
        foreach (PersonalRecords r in list)
            r.Abort();
    }

    bool Obtain(int type) => type > 0 && Tracker.lootCache.Contains(type) && obtained.Add(type);

    void ScanInventory()
    {
        if (!Tracker.finalized)
            return;
        Player player = Player;
        for (int i = 0; i < player.inventory.Length; i++)
            ScanItem(player.inventory[i]);
        for (int i = 0; i < player.armor.Length; i++)
            ScanItem(player.armor[i]);
        for (int i = 0; i < player.miscEquips.Length; i++)
            ScanItem(player.miscEquips[i]);
        Item mouse = Main.mouseItem;
        if (!mouse.IsNull && mouse.type > 0)
            Obtain(mouse.type);
    }

    void ScanItem(Item item)
    {
        if (item.IsNull)
            return;
        int type = item.type;
        if (type > 0 && item.stack > 0)
            Obtain(type);
    }

    public void AddMiniKill(string key)
    {
        miniKills.TryGetValue(key, out int count);
        miniKills[key] = count + 1;
    }

    public void NewRecord(int ri, int state)
    {
        newRecordState = Math.Max(newRecordState, state);
        SubmitCombatText(ri);
    }

    public void SubmitCombatText(int ri)
    {
        Player p = Util.Local;
        string? text = newRecordState == 1 ? L("Log.Records.NewRecord") : newRecordState == 2 ? L("Log.Records.NewWorldRecord") : null;
        if (!p.IsNull && text != null)
            CombatText.NewText(p.getRect(), Data.LightYellow, text, true, false);
        if (newRecordState != 0 && ri >= 0 && ri < hasNewRecord.Length)
            hasNewRecord[ri] = true;
        newRecordState = 0;
    }

    static readonly List<Action> enterListeners = new();

    public static void OnEnter(Action fn) => enterListeners.Add(fn);

    static readonly List<Action<int>> joinListeners = new();

    public static void OnJoin(Action<int> fn) => joinListeners.Add(fn);

    public static void InstallPlayerNet()
    {
        Net.On(PacketKind.Join, (r, from, _) =>
        {
            if (!Util.IsServer)
                return;
            Fight.ResetServerSlot(from);
            List<string> keys = Net.ReadStrings(r);
            List<PersonalRecords> list = State.serverRecords[from]!;
            foreach (string key in keys)
            {
                int kills = r.ReadInt32(), durationBest = r.ReadInt32(), hitsTakenBest = r.ReadInt32();
                PersonalRecords? record = list.Find(x => x.bossKey == key);
                if (record == null)
                    continue;
                record.kills = kills;
                if (record.kills > 0)
                    record.playTimeFirst = 1;
                record.durationBest = durationBest;
                record.hitsTakenBest = hitsTakenBest;
            }
            State.allowTracking[from] = r.ReadBoolean();
            State.allowNew[from] = r.ReadBoolean();
            Net.ToClient(from, PacketKind.WorldRecords, w =>
            {
                w.Write(State.worldRecords.Count);
                foreach (WorldRecord wr in State.worldRecords)
                    wr.WriteFull(w);
            });
            foreach (Action<int> fn in joinListeners)
                Util.Safe("join", () => fn(from));
        });
        Net.On(PacketKind.Allow, (r, from, _) =>
        {
            if (!Util.IsServer)
                return;
            State.allowTracking[from] = r.ReadBoolean();
            State.allowNew[from] = r.ReadBoolean();
        });
        Net.On(PacketKind.WorldRecords, (r, _, _) =>
        {
            if (!Util.IsClient)
                return;
            int count = r.ReadInt32();
            var known = new Dictionary<string, WorldRecord>();
            for (int i = 0; i < count; i++)
            {
                WorldRecord wr = WorldRecord.ReadFull(r);
                known[wr.bossKey] = wr;
            }
            State.worldRecords = Tracker.recordKeys.Select(k => known.TryGetValue(k, out WorldRecord? wr) ? wr : new WorldRecord(k)).ToList();
        });
        Net.On(PacketKind.Track, (r, _, _) =>
        {
            int i = r.ReadInt32();
            List<PersonalRecords>? list = State.Me?.RecordsForWorld;
            if (list != null && i >= 0 && i < list.Count)
                list[i].StartTracking();
        });
        Net.On(PacketKind.Record, (r, _, _) =>
        {
            BossChecklistPlayer? me = State.Me;
            List<PersonalRecords>? list = me?.RecordsForWorld;
            int i = r.ReadInt32();
            if (me == null || list == null || i < 0 || i >= list.Count)
                return;
            PersonalRecords record = list[i];
            record.tracking = false;
            if (record.Receive(r, Records.PlayTimeNow()))
                me.newRecordState = 1;
        });
        Net.On(PacketKind.WorldRecord, (r, _, _) =>
        {
            if (!Util.IsClient)
                return;
            int i = r.ReadInt32();
            WorldRecord? wr = i >= 0 && i < State.worldRecords.Count ? State.worldRecords[i] : null;
            if (wr == null)
                return;
            int f = wr.Receive(r);
            bool holder = r.ReadBoolean();
            if ((f & RecordFlags.WR_OnlyDeaths) != 0)
                return;
            BossChecklistPlayer? me = State.Me;
            if (me == null)
                return;
            if (holder && me.newRecordState == 1)
                me.newRecordState = 2;
            me.SubmitCombatText(i);
        });
        Net.On(PacketKind.MiniKill, (r, _, _) =>
        {
            List<string> keys = Net.ReadStrings(r);
            BossChecklistPlayer? me = State.Me;
            if (me == null)
                return;
            foreach (string key in keys)
                me.AddMiniKill(key);
        });
        Net.On(PacketKind.Reset, (r, from, _) =>
        {
            if (!Util.IsServer)
                return;
            int i = r.ReadInt32(), f = r.ReadInt32();
            Fight.ServerRecord(from, i)?.ApplyReset(f);
        });
    }

    public static void SendAllowTracking()
    {
        bool track = Cfg.Tracking, allow = Cfg.NewRecords;
        if (Util.IsClient)
        {
            Net.ToServer(PacketKind.Allow, w =>
            {
                w.Write(track);
                w.Write(allow);
            });
        }
        int my = Main.myPlayer;
        if (my >= 0 && !Util.IsServer)
        {
            State.allowTracking[my] = track;
            State.allowNew[my] = allow;
        }
    }

    public static bool IsResearched(int type) =>
        GameBindings.TryGetSacrificeNumbers(Util.Local, type, out int have, out int need) && have >= need;
}
