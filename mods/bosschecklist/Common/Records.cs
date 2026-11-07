using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.IO;
using static BossChecklistMod.Common.ModText;

namespace BossChecklistMod.Common;

public static class RecordFlags
{
    public const int None = 0, PreviousAttempt = 1, FirstVictory = 2, SuccessfulAttempt = 4, PB_Duration = 8, PB_HitsTaken = 0x10,
        WR_Duration = 0x20, WR_HitsTaken = 0x40, WR_OnlyDeaths = 0x80, PB_Reset = 0x100, FV_Reset = 0x200;

    public static bool NewPersonalBest(int f) => (f & (PB_Duration | PB_HitsTaken)) != 0;
}

public static class RecordCategory
{
    public const int PREVIOUS = 0, FIRST = 1, BEST = 2, WORLD = 3, NONE = 4;
    public static readonly string[] Names = { "PreviousAttempt", "FirstVictory", "PersonalBest", "WorldRecord" };
}

public sealed class PersonalRecords
{
    public string bossKey;
    public int kills, deaths, attempts;
    public long playTimeFirst = -1;
    public int durationPrev = -1, durationBest = -1, durationPrevBest = -1, durationFirst = -1;
    public int hitsTakenPrev = -1, hitsTakenBest = -1, hitsTakenPrevBest = -1, hitsTakenFirst = -1;
    public int trackDuration, trackHits, trackDeaths;
    public bool tracking;

    public PersonalRecords(string key) => bossKey = key;

    static void Read(TagCompound tag, string key, ref int field)
    {
        if (tag.ContainsKey(key) && tag[key] is byte or short or int or long or float or double)
            field = tag.Get<int>(key);
    }

    public static PersonalRecords From(string key, TagCompound data)
    {
        string stored = data.GetString("BossKey");
        var r = new PersonalRecords(stored.Length > 0 ? stored : key);
        Read(data, "kills", ref r.kills);
        Read(data, "deaths", ref r.deaths);
        Read(data, "attempts", ref r.attempts);
        if (data.ContainsKey("playTimeFirst") && data["playTimeFirst"] is byte or short or int or long or float or double)
            r.playTimeFirst = data.Get<long>("playTimeFirst");
        Read(data, "durationPrev", ref r.durationPrev);
        Read(data, "durationBest", ref r.durationBest);
        Read(data, "durationPrevBest", ref r.durationPrevBest);
        Read(data, "durationFirst", ref r.durationFirst);
        Read(data, "hitsTakenPrev", ref r.hitsTakenPrev);
        Read(data, "hitsTakenBest", ref r.hitsTakenBest);
        Read(data, "hitsTakenPrevBest", ref r.hitsTakenPrevBest);
        Read(data, "hitsTakenFirst", ref r.hitsTakenFirst);
        return r;
    }

    public TagCompound Save() => new()
    {
        ["BossKey"] = bossKey,
        ["kills"] = kills,
        ["deaths"] = deaths,
        ["attempts"] = attempts,
        ["playTimeFirst"] = playTimeFirst,
        ["durationPrev"] = durationPrev,
        ["durationBest"] = durationBest,
        ["durationPrevBest"] = durationPrevBest,
        ["durationFirst"] = durationFirst,
        ["hitsTakenPrev"] = hitsTakenPrev,
        ["hitsTakenBest"] = hitsTakenBest,
        ["hitsTakenPrevBest"] = hitsTakenPrevBest,
        ["hitsTakenFirst"] = hitsTakenFirst,
    };

    public int RecordIndex => Tracker.recordKeys.IndexOf(bossKey);
    public bool CanBeSaved => attempts > 0;
    public bool UnlockedFirstVictory => playTimeFirst > 0;
    public bool UnlockedPersonalBest => kills >= 2;
    public bool IsTracking => tracking;

    public int Stat(int category, bool duration = true) => category switch
    {
        RecordCategory.PREVIOUS => duration ? durationPrev : hitsTakenPrev,
        RecordCategory.FIRST => duration ? durationFirst : hitsTakenFirst,
        RecordCategory.BEST => duration ? durationBest : hitsTakenBest,
        _ => -1,
    };

    public bool StartTracking()
    {
        if (tracking || !Cfg.Tracking)
            return false;
        tracking = true;
        trackDuration = trackHits = trackDeaths = 0;
        return true;
    }

    // Null when nothing was being tracked.
    public int? StopTracking(bool allowSaving, bool savePrevious, long playTime)
    {
        if (!tracking || !Cfg.Tracking)
        {
            tracking = false;
            return null;
        }
        int f = RecordFlags.None;
        tracking = false;
        attempts++;
        deaths += trackDeaths;
        if (savePrevious)
        {
            durationPrev = trackDuration;
            hitsTakenPrev = trackHits;
            f |= RecordFlags.PreviousAttempt;
        }
        if (allowSaving)
        {
            kills++;
            f |= RecordFlags.SuccessfulAttempt;
            if (!UnlockedFirstVictory)
            {
                playTimeFirst = playTime > 0 ? playTime : 1;
                durationFirst = durationBest = trackDuration;
                hitsTakenFirst = hitsTakenBest = trackHits;
                f |= RecordFlags.FirstVictory | RecordFlags.PB_Duration | RecordFlags.PB_HitsTaken;
            }
            else
            {
                if (durationBest == -1 || durationBest > trackDuration)
                {
                    durationPrevBest = durationBest;
                    durationBest = trackDuration;
                    f |= RecordFlags.PB_Duration;
                }
                if (hitsTakenBest == -1 || hitsTakenBest > trackHits)
                {
                    hitsTakenPrevBest = hitsTakenBest;
                    hitsTakenBest = trackHits;
                    f |= RecordFlags.PB_HitsTaken;
                }
            }
        }
        return f;
    }

    public void Abort() => tracking = false;

    public int Reset(int category)
    {
        if (category == RecordCategory.FIRST)
        {
            playTimeFirst = -1;
            durationFirst = hitsTakenFirst = -1;
            return RecordFlags.FV_Reset;
        }
        if (category == RecordCategory.BEST)
        {
            kills = deaths = 0;
            durationBest = hitsTakenBest = durationPrevBest = hitsTakenPrevBest = -1;
            return RecordFlags.PB_Reset;
        }
        return RecordFlags.None;
    }

    public void ApplyReset(int flag)
    {
        if ((flag & RecordFlags.FV_Reset) != 0)
            Reset(RecordCategory.FIRST);
        if ((flag & RecordFlags.PB_Reset) != 0)
            Reset(RecordCategory.BEST);
    }

    public void WritePacket(BinaryWriter w, int f)
    {
        w.Write(f);
        w.Write(trackDeaths);
        if ((f & RecordFlags.PreviousAttempt) != 0)
        {
            w.Write(durationPrev);
            w.Write(hitsTakenPrev);
        }
        if ((f & RecordFlags.FirstVictory) != 0)
        {
            w.Write(durationFirst);
            w.Write(hitsTakenFirst);
        }
        if ((f & RecordFlags.PB_Duration) != 0)
        {
            w.Write(durationBest);
            w.Write(durationPrevBest);
        }
        if ((f & RecordFlags.PB_HitsTaken) != 0)
        {
            w.Write(hitsTakenBest);
            w.Write(hitsTakenPrevBest);
        }
    }

    public bool Receive(BinaryReader r, long playTime)
    {
        int f = r.ReadInt32();
        attempts++;
        deaths += r.ReadInt32();
        bool hadFirst = UnlockedFirstVictory;
        bool fresh = false;
        if ((f & RecordFlags.SuccessfulAttempt) != 0)
            kills++;
        if ((f & RecordFlags.PreviousAttempt) != 0)
        {
            durationPrev = r.ReadInt32();
            hitsTakenPrev = r.ReadInt32();
        }
        if ((f & RecordFlags.FirstVictory) != 0)
        {
            durationFirst = r.ReadInt32();
            hitsTakenFirst = r.ReadInt32();
            playTimeFirst = playTime > 0 ? playTime : 1;
        }
        if ((f & RecordFlags.PB_Duration) != 0)
        {
            durationBest = r.ReadInt32();
            durationPrevBest = r.ReadInt32();
            if (hadFirst)
                fresh = true;
        }
        if ((f & RecordFlags.PB_HitsTaken) != 0)
        {
            hitsTakenBest = r.ReadInt32();
            hitsTakenPrevBest = r.ReadInt32();
            if (hadFirst)
                fresh = true;
        }
        return fresh;
    }

    public string KDR() => kills == 0 && deaths == 0 ? L("Log.Records.Unchallenged") : L("Log.Records.KDR", kills, deaths);

    public string PlayTime() => !UnlockedFirstVictory ? L("Log.Records.Unchallenged") : Records.PlayTimeToString(playTimeFirst);
}

public sealed class WorldRecord
{
    public string bossKey;
    public int totalKills, totalDeaths;
    public List<string> durationHolder = new();
    public int durationWorld = -1;
    public List<string> hitsTakenHolder = new();
    public int hitsTakenWorld = -1;

    public WorldRecord(string key) => bossKey = key;

    static List<string> Names(TagCompound tag, string key)
    {
        var names = new List<string>();
        if (tag.ContainsKey(key) && tag[key] is IList list)
        {
            foreach (object? name in list)
                names.Add(Convert.ToString(name) ?? "");
        }
        return names;
    }

    static int Number(TagCompound tag, string key, int fallback) =>
        tag.ContainsKey(key) && tag[key] is byte or short or int or long or float or double ? tag.Get<int>(key) : fallback;

    public static WorldRecord From(TagCompound d) => new(d.GetString("BossKey"))
    {
        totalKills = Number(d, "totalKills", 0),
        totalDeaths = Number(d, "totalDeaths", 0),
        durationHolder = Names(d, "durationHolder"),
        durationWorld = Number(d, "durationWorld", -1),
        hitsTakenHolder = Names(d, "hitsTakenHolder"),
        hitsTakenWorld = Number(d, "hitsTakenWorld", -1),
    };

    public TagCompound Save() => new()
    {
        ["BossKey"] = bossKey,
        ["totalKills"] = totalKills,
        ["totalDeaths"] = totalDeaths,
        ["durationHolder"] = new List<string>(durationHolder),
        ["durationWorld"] = durationWorld,
        ["hitsTakenHolder"] = new List<string>(hitsTakenHolder),
        ["hitsTakenWorld"] = hitsTakenWorld,
    };

    public int RecordIndex => Tracker.recordKeys.IndexOf(bossKey);
    public bool CanBeSaved => totalKills > 0 || totalDeaths > 0;
    public bool DurationNotRecorded => durationHolder.Count == 0 || durationWorld == -1;
    public bool HitsTakenNotRecorded => hitsTakenHolder.Count == 0 || hitsTakenWorld == -1;

    public (int f, HashSet<int> winners) CheckForWorldRecords(List<(string name, int who, PersonalRecords record)> entries)
    {
        int f = RecordFlags.None;
        totalKills++;
        var winners = new HashSet<int>();
        foreach ((string name, int who, PersonalRecords record) in entries)
        {
            totalDeaths += record.trackDeaths;
            bool faster = record.trackDuration < durationWorld, tie = record.trackDuration == durationWorld;
            if (DurationNotRecorded || faster || tie)
            {
                f |= RecordFlags.WR_Duration;
                if (faster || DurationNotRecorded)
                    durationHolder = new List<string>();
                durationWorld = record.trackDuration;
                if (!durationHolder.Contains(name))
                    durationHolder.Add(name);
                winners.Add(who);
            }
            bool fewer = record.trackHits < hitsTakenWorld, tieH = record.trackHits == hitsTakenWorld;
            if (HitsTakenNotRecorded || fewer || tieH)
            {
                f |= RecordFlags.WR_HitsTaken;
                if (fewer || HitsTakenNotRecorded)
                    hitsTakenHolder = new List<string>();
                hitsTakenWorld = record.trackHits;
                if (!hitsTakenHolder.Contains(name))
                    hitsTakenHolder.Add(name);
                winners.Add(who);
            }
        }
        return (f, winners);
    }

    public void WritePacket(BinaryWriter w, int f)
    {
        w.Write(f);
        w.Write(totalDeaths);
        w.Write(totalKills);
        if ((f & RecordFlags.WR_OnlyDeaths) != 0)
            return;
        if ((f & RecordFlags.WR_Duration) != 0)
        {
            w.Write(durationWorld);
            Net.WriteStrings(w, durationHolder);
        }
        if ((f & RecordFlags.WR_HitsTaken) != 0)
        {
            w.Write(hitsTakenWorld);
            Net.WriteStrings(w, hitsTakenHolder);
        }
    }

    // Returns the packet's flags.
    public int Receive(BinaryReader r)
    {
        int f = r.ReadInt32();
        totalDeaths = r.ReadInt32();
        totalKills = r.ReadInt32();
        if ((f & RecordFlags.WR_OnlyDeaths) != 0)
            return f;
        if ((f & RecordFlags.WR_Duration) != 0)
        {
            durationWorld = r.ReadInt32();
            durationHolder = Net.ReadStrings(r);
        }
        if ((f & RecordFlags.WR_HitsTaken) != 0)
        {
            hitsTakenWorld = r.ReadInt32();
            hitsTakenHolder = Net.ReadStrings(r);
        }
        return f;
    }

    public void WriteFull(BinaryWriter w)
    {
        w.Write(bossKey);
        w.Write(totalKills);
        w.Write(totalDeaths);
        Net.WriteStrings(w, durationHolder);
        w.Write(durationWorld);
        Net.WriteStrings(w, hitsTakenHolder);
        w.Write(hitsTakenWorld);
    }

    public static WorldRecord ReadFull(BinaryReader r) => new(r.ReadString())
    {
        totalKills = r.ReadInt32(),
        totalDeaths = r.ReadInt32(),
        durationHolder = Net.ReadStrings(r),
        durationWorld = r.ReadInt32(),
        hitsTakenHolder = Net.ReadStrings(r),
        hitsTakenWorld = r.ReadInt32(),
    };

    public string KDR() => totalKills == 0 && totalDeaths == 0 ? L("Log.Records.Unchallenged") : L("Log.Records.KDR", totalKills, totalDeaths);

    public string Holders(bool duration)
    {
        bool missing = duration ? DurationNotRecorded : HitsTakenNotRecorded;
        if (missing)
            return L("Log.Records.ClaimRecord");
        return L("Log.Records.RecordHolder") + "\n • " + string.Join("\n • ", duration ? durationHolder : hitsTakenHolder);
    }
}

public static class Records
{
    public static string Clock(double totalSeconds, string sign = "")
    {
        long s = Math.Max(0, (long)Math.Floor(totalSeconds));
        long d = s / 86400;
        s -= d * 86400;
        long h = s / 3600;
        s -= h * 3600;
        long m = s / 60;
        s -= m * 60;
        var parts = new List<string>();
        if (d != 0)
            parts.Add(L("Log.Records.Time.Days", d));
        if (d != 0 || h != 0)
            parts.Add(L("Log.Records.Time.Hours", h));
        if (d != 0 || h != 0 || m != 0)
            parts.Add(L("Log.Records.Time.Minutes", m));
        parts.Add(L("Log.Records.Time.Seconds", s));
        return sign + string.Join(" ", parts);
    }

    public static string TimeConversion(int ticks, string sign = "") =>
        ticks == -1 ? L("Log.Records.NoRecord") : Clock(ticks / 60.0, sign);

    static (int d, string sign, Color color)? Diff(int record, int compare)
    {
        if (record == -1 || compare == -1)
            return null;
        int d = record - compare;
        if (d > 0)
            return (d, "+", Data.RarityRed);
        if (d == 0)
            return (0, "±", Data.RarityYellow);
        return (-d, "-", Data.RarityGreen);
    }

    public static (string text, Color? color) TimeDiff(int record, int compare)
    {
        var r = Diff(record, compare);
        return r is { } v ? (TimeConversion(v.d, v.sign), v.color) : ("", null);
    }

    public static string HitCount(int n)
    {
        if (n == -1)
            return L("Log.Records.NoRecord");
        return n == 0 ? L("Log.Records.NoHit") : L("Log.Records.Hit", n);
    }

    public static (string text, Color? color) HitDiff(int record, int compare)
    {
        var r = Diff(record, compare);
        return r is { } v ? (v.sign + v.d, v.color) : ("", null);
    }

    public static string PlayTimeToString(long ticks) => Clock(ticks / 1e7);

    public static long PlayTimeNow() => GameBindings.PlayTimeTicks();

    public static string WorldGuid() => GameBindings.WorldGuid();
}
