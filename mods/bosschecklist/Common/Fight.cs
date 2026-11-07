using System;
using System.Collections.Generic;
using System.Linq;
using BossChecklistMod.Common.Players;
using BossChecklistMod.Common.Systems;
using Terraria;
using static BossChecklistMod.Common.ModText;

namespace BossChecklistMod.Common;

public static class Fight
{
    const int Max = 200;

    static readonly int[] flags = NewFlags();
    static readonly int[] types = new int[Max];
    static readonly bool[] alive = new bool[Max];
    static readonly int[] lastLife = new int[Max];
    static readonly bool[] limbDone = new bool[Max];
    static readonly bool[] hitMe = new bool[Max];
    static readonly HashSet<int>?[] hitBy = new HashSet<int>?[Max];
    static readonly Dictionary<int, HashSet<int>> touched = new();
    static long coreDeadAt = -100000;

    public static Action WorldChanged = () => { };

    static int[] NewFlags()
    {
        var array = new int[Max];
        Array.Fill(array, -1);
        return array;
    }

    static void Touch(int index, int who)
    {
        if (!touched.TryGetValue(index, out HashSet<int>? set))
            touched[index] = set = new HashSet<int>();
        set.Add(who);
    }

    static long Now => Main.GameUpdateCount;

    public static string NpcName(NPC npc)
    {
        string? name = npc.FullName;
        if (!string.IsNullOrEmpty(name))
            return name;
        name = npc.GivenOrTypeName;
        return !string.IsNullOrEmpty(name) ? name : NPCName(npc.type);
    }

    static List<int> Interactions(NPC npc)
    {
        var who = new List<int>();
        foreach (Player p in Net.ActivePlayers())
        {
            if (npc.playerInteraction[p.whoAmI])
                who.Add(p.whoAmI);
        }
        return who;
    }

    static bool Interacted(int i, NPC npc, int who)
    {
        if (npc.playerInteraction[who])
            return true;
        if (Util.IsSP)
            return hitMe[i];
        return hitBy[i]?.Contains(who) == true;
    }

    static List<PersonalRecords>? Mine() => State.Me?.RecordsForWorld;

    public static void Reset()
    {
        Array.Fill(flags, -1);
        Array.Clear(types);
        Array.Clear(alive);
        Array.Clear(lastLife);
        Array.Clear(limbDone);
        Array.Clear(hitMe);
        Array.Clear(hitBy);
        touched.Clear();
        coreDeadAt = -100000;
    }

    static bool Flagged(int index)
    {
        for (int i = 0; i < Max; i++)
        {
            if (flags[i] == index)
                return true;
        }
        return false;
    }

    public static string? ActiveBossName()
    {
        for (int i = 0; i < Max; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.IsNull || !npc.active)
                continue;
            EntryInfo? e = Tracker.FindBossEntryByNPC(npc.type);
            if (e != null)
                return e.DisplayName;
        }
        return null;
    }

    public static void Scan()
    {
        if (Util.IsClient || !Tracker.finalized)
            return;
        int my = Main.myPlayer;
        for (int i = 0; i < Max; i++)
        {
            NPC npc = Main.npc[i];
            if (npc.IsNull)
                continue;
            if (!npc.active)
            {
                if (alive[i])
                {
                    alive[i] = false;
                    if (flags[i] >= 0)
                        Despawn(i, npc, npc.life);
                }
                continue;
            }
            int type = npc.type;
            if (!alive[i] || types[i] != type)
                NewOccupant(i, npc, type);
            int life = npc.life;
            lastLife[i] = life;
            if (flags[i] >= 0)
            {
                if (Util.IsSP)
                {
                    if (!hitMe[i] && npc.playerInteraction[my])
                    {
                        hitMe[i] = true;
                        Touch(flags[i], my);
                    }
                }
                else if ((Now & 7) == (i & 7))
                {
                    List<int> now = Interactions(npc);
                    if (now.Count > 0)
                    {
                        (hitBy[i] ??= new HashSet<int>()).UnionWith(now);
                        foreach (int who in now)
                            Touch(flags[i], who);
                    }
                }
            }
            if ((type == 396 || type == 397) && !limbDone[i])
            {
                if (life <= 0 || npc.ai[0] == -2)
                {
                    limbDone[i] = true;
                    if (!CoreDying())
                        SafeLimb(npc);
                }
            }
            if (type == 398 && (life <= 0 || npc.ai[0] == -2))
                coreDeadAt = Now;
        }
    }

    // Separate from Scan: a lambda there would allocate for every NPC slot on every frame.
    static void SafeLimb(NPC npc) => Util.Safe("limb", () => Limb(npc));

    // The slot has a new NPC, or its NPC changed type without dying.
    static void NewOccupant(int i, NPC npc, int type)
    {
        int was = alive[i] ? flags[i] : -1;
        EntryInfo? entry = Tracker.FindBossEntryByNPC(type);
        int index = entry?.GetIndex ?? -1;
        if (was >= 0 && was != index)
        {
            flags[i] = was;
            Despawn(i, npc, lastLife[i], types[i]);
        }
        types[i] = type;
        alive[i] = true;
        limbDone[i] = false;
        hitMe[i] = false;
        hitBy[i] = null;
        if (was != index || was < 0)
        {
            flags[i] = index;
            if (entry != null)
                Util.Safe("start", () => Start(entry));
        }
    }

    static bool CoreDying() => Now - coreDeadAt < 600;

    static void Start(EntryInfo entry)
    {
        int ri = entry.RecordIndex;
        if (ri < 0)
            return;
        if (Util.IsSP)
        {
            List<PersonalRecords>? list = Mine();
            if (list != null && ri < list.Count)
                list[ri].StartTracking();
            return;
        }
        foreach (Player p in Net.ActivePlayers())
        {
            int who = p.whoAmI;
            PersonalRecords? r = ServerRecord(who, ri);
            if (r != null && State.allowTracking[who] && r.StartTracking())
                Net.ToClient(who, PacketKind.Track, w => w.Write(ri));
        }
    }

    static void Despawn(int i, NPC npc, int life, int? oldType = null) =>
        Util.Safe("despawn", () => DespawnInner(i, npc, life, oldType));

    static void DespawnInner(int i, NPC npc, int life, int? oldType)
    {
        int index = flags[i];
        flags[i] = -1;
        if (index < 0 || index >= Tracker.sorted.Count || Flagged(index))
            return;
        EntryInfo entry = Tracker.sorted[index];
        bool moonLord = entry.Key == "Terraria MoonLord";
        if (moonLord && life <= 0)
        {
            Victory(entry, i, npc);
            return;
        }
        touched.Remove(index, out HashSet<int>? seen);
        bool Hit(int who) => Interacted(i, npc, who) || seen?.Contains(who) == true;
        int type = oldType ?? npc.type;
        var shown = new ShownNpc(type, NpcName(npc), npc);
        string? msg = entry.DespawnMessage(shown);
        if (msg != null && (!moonLord || life > 0))
        {
            if (Util.IsSP)
                Util.Say(Format(msg, shown.FullName), Data.RarityPurple);
            else
                Net.ToAll(PacketKind.Chat, w => BossChecklistWorld.WriteChat(w, BossChecklistWorld.ChatDespawn, type, shown.FullName));
        }
        if (entry.type != Data.BOSS)
            return;
        int ri = entry.RecordIndex;
        if (ri < 0)
            return;
        if (Util.IsSP)
        {
            List<PersonalRecords>? list = Mine();
            if (list != null && ri < list.Count)
                list[ri].StopTracking(false, Hit(Main.myPlayer), Records.PlayTimeNow());
            return;
        }
        var players = new List<int>();
        foreach (Player p in Net.ActivePlayers())
        {
            bool did = Hit(p.whoAmI);
            StopServer(p.whoAmI, ri, false, did);
            if (did)
                players.Add(p.whoAmI);
        }
        if (ri < State.worldRecords.Count)
        {
            WorldRecord wr = State.worldRecords[ri];
            foreach (int who in players)
            {
                PersonalRecords? r = ServerRecord(who, ri);
                if (r != null)
                    wr.totalDeaths += r.trackDeaths;
            }
            Net.ToAll(PacketKind.WorldRecord, w => WriteWorldRecord(w, ri, wr, RecordFlags.WR_OnlyDeaths, false));
            State.Touch();
        }
    }

    static void WriteWorldRecord(System.IO.BinaryWriter w, int ri, WorldRecord wr, int f, bool holder)
    {
        w.Write(ri);
        wr.WritePacket(w, f);
        w.Write(holder);
    }

    public static void OnKill(NPC npc)
    {
        if (Util.IsClient || !Tracker.finalized)
            return;
        int i = npc.whoAmI;
        int type = npc.type;
        Util.Safe("downed flags", () => DownedFlags(type));
        EntryInfo? limb = Tracker.IsEntryLimb(type);
        if (limb != null && type != 396 && type != 397)
            Util.Safe("limb", () => Limb(npc, limb));
        Util.Safe("mini kills", () => MiniKill(npc));
        if (type == 398)
            coreDeadAt = Now;
        if (i < 0 || i >= Max)
            return;
        alive[i] = false;
        flags[i] = -1;
        EntryInfo? entry = Tracker.FindBossEntryByNPC(type);
        if (entry == null || Flagged(entry.GetIndex))
            return;
        Victory(entry, i, npc);
    }

    static void DownedFlags(int type)
    {
        bool changed = Data.KILL_FLAGS.TryGetValue(type, out string? flag) && World.downed.Add(flag);
        switch (type)
        {
            case 422: NPC.downedTowerVortex = true; break;
            case 493: NPC.downedTowerStardust = true; break;
            case 507: NPC.downedTowerNebula = true; break;
            case 517: NPC.downedTowerSolar = true; break;
        }
        if (changed)
        {
            Tracker.Invalidate();
            WorldChanged();
        }
    }

    static void MiniKill(NPC npc)
    {
        int type = npc.type;
        List<string> keys = Tracker.sorted.Where(e => e.type == Data.MINIBOSS && e.npcIDs.Contains(type)).Select(e => e.Key).ToList();
        if (keys.Count == 0)
            return;
        if (Util.IsSP)
        {
            if (!npc.playerInteraction[Main.myPlayer])
                return;
            BossChecklistPlayer? me = State.Me;
            if (me != null)
            {
                foreach (string k in keys)
                    me.AddMiniKill(k);
            }
            return;
        }
        foreach (int who in Interactions(npc))
            Net.ToClient(who, PacketKind.MiniKill, w => Net.WriteStrings(w, keys));
    }

    static void Limb(NPC npc, EntryInfo? entry = null)
    {
        entry ??= Tracker.IsEntryLimb(npc.type);
        if (entry == null)
            return;
        string name = NpcName(npc);
        if (Util.IsSP)
        {
            string? msg = entry.LimbMessage(new ShownNpc(npc.type, name, npc));
            if (msg != null)
                Util.Say(Format(msg, name), Data.RarityGreen);
            return;
        }
        int type = npc.type;
        Net.ToAll(PacketKind.Chat, w => BossChecklistWorld.WriteChat(w, BossChecklistWorld.ChatLimb, type, name));
    }

    static void Victory(EntryInfo entry, int i, NPC npc)
    {
        int index = entry.GetIndex;
        touched.Remove(index, out HashSet<int>? seen);
        if (entry.type != Data.BOSS)
            return;
        int ri = entry.RecordIndex;
        if (ri < 0)
            return;
        bool Hit(int who) => Interacted(i, npc, who) || seen?.Contains(who) == true;
        if (Util.IsSP)
        {
            BossChecklistPlayer? me = State.Me;
            List<PersonalRecords>? list = Mine();
            if (list == null || ri >= list.Count)
                return;
            bool did = Hit(Main.myPlayer);
            int? f = list[ri].StopTracking(did && Cfg.NewRecords, did, Records.PlayTimeNow());
            if (f is { } flagsNow && RecordFlags.NewPersonalBest(flagsNow) && (flagsNow & RecordFlags.FirstVictory) == 0 && me != null)
                me.NewRecord(ri, 1);
            return;
        }
        var winners = new List<int>();
        bool any = false;
        foreach (Player p in Net.ActivePlayers())
        {
            int who = p.whoAmI;
            bool did = Hit(who);
            if (StopServer(who, ri, did && State.allowNew[who], did))
                any = true;
            if (did)
                winners.Add(who);
        }
        if (!any || ri >= State.worldRecords.Count)
            return;
        WorldRecord wr = State.worldRecords[ri];
        var entries = new List<(string name, int who, PersonalRecords record)>();
        foreach (int who in winners)
        {
            PersonalRecords? record = ServerRecord(who, ri);
            if (record != null)
                entries.Add((Main.player[who].name ?? "", who, record));
        }
        (int result, HashSet<int> holders) = wr.CheckForWorldRecords(entries);
        State.Touch();
        foreach (Player p in Net.ActivePlayers())
        {
            bool holder = holders.Contains(p.whoAmI);
            Net.ToClient(p.whoAmI, PacketKind.WorldRecord, w => WriteWorldRecord(w, ri, wr, result, holder));
        }
    }

    public static void AnnounceMoon(string moonEvent)
    {
        if (Util.IsServer)
        {
            Net.ToAll(PacketKind.Chat, w => BossChecklistWorld.WriteChat(w, BossChecklistWorld.ChatMoon, 0, moonEvent));
            return;
        }
        MoonMessage(moonEvent);
    }

    public static void MoonMessage(string moonEvent)
    {
        MessageMode mode = Cfg.Feature.MoonMessages;
        string text = "";
        if (mode == MessageMode.Generic)
        {
            string a = "Bestiary_Events." + moonEvent, b = "Bestiary_Invasions." + moonEvent;
            string name = Vanilla(a);
            if (name == a)
                name = Vanilla(b);
            text = L("ChatMessages.EventEnd.Generic", moonEvent == "Eclipse" ? name.ToLowerInvariant() : name);
        }
        else if (mode == MessageMode.Unique)
        {
            text = L("ChatMessages.EventEnd." + moonEvent);
        }
        if (text.Length > 0)
            Util.Say(text, Data.MoonEnd);
    }

    static List<PersonalRecords> NewRecordList() => Tracker.recordKeys.Select(k => new PersonalRecords(k)).ToList();

    public static PersonalRecords? ServerRecord(int who, int ri)
    {
        List<PersonalRecords> list = State.serverRecords[who] ??= NewRecordList();
        return ri >= 0 && ri < list.Count ? list[ri] : null;
    }

    static bool StopServer(int who, int ri, bool allow, bool previous)
    {
        if (!State.allowTracking[who])
            return false;
        PersonalRecords? r = ServerRecord(who, ri);
        if (r == null)
            return false;
        int? f = r.StopTracking(allow, previous, 1);
        if (f is not { } result)
            return false;
        Net.ToClient(who, PacketKind.Record, w =>
        {
            w.Write(ri);
            r.WritePacket(w, result);
        });
        return RecordFlags.NewPersonalBest(result);
    }

    public static void ResetServerSlot(int who) => State.serverRecords[who] = NewRecordList();

    public static void InitWorldRecords(IEnumerable<Terraria.ModLoader.IO.TagCompound> saved)
    {
        var known = new Dictionary<string, WorldRecord>();
        State.worldRecordsUnloaded = new List<WorldRecord>();
        foreach (Terraria.ModLoader.IO.TagCompound d in saved)
        {
            WorldRecord r = WorldRecord.From(d);
            if (r.bossKey.Length == 0)
                continue;
            if (Tracker.recordKeys.Contains(r.bossKey))
                known[r.bossKey] = r;
            else
                State.worldRecordsUnloaded.Add(r);
        }
        State.worldRecords = Tracker.recordKeys.Select(k => known.TryGetValue(k, out WorldRecord? r) ? r : new WorldRecord(k)).ToList();
    }

    public static PersonalRecords? CurrentTracker()
    {
        List<PersonalRecords>? list = Mine();
        if (list == null)
            return null;
        foreach (PersonalRecords r in list)
        {
            if (r.IsTracking)
                return r;
        }
        return null;
    }
}
