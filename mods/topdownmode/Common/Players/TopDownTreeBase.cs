using System;
using System.Collections.Generic;
using System.Text;
using BunnyLoader.Interop;
using Terraria;
using Terraria.ID;
using topdownmode.Common.Systems;
using topdownmode.Common.Utils;

namespace topdownmode.Common.Players;

public sealed class TileBase
{
    public int I, J, T;
    public bool Soft, Virtual;
}

// A tree's invisible ground block stops the game's collision (it is marked inactive) and collides
// here instead, only against the feet, so the player walks behind trunks.
public static class TopDownTreeBase
{
    public static readonly TypeMark TreeMark = new();

    const int Tree = TileID.Trees;
    const int FeetH = 10;
    const int BoxH = 12;
    const int PadX = 2;
    const int ScanR = 10;
    const int ScanEvery = 6;
    const int WideR = 40, WideEvery = 180;

    public static readonly Dictionary<long, TileBase> Known = new();

    static bool enabled = true, repairOff;
    static List<TileBase> near = new();
    static long tick = -999, wideTick = long.MinValue / 2;
    static int softened, removed, errors, repaired, repairFails, virtualNear, wideFixed;
    static string lastErr = "";
    static readonly HashSet<string> logged = new();
    static readonly Dictionary<long, long> noRepair = new();
    static readonly List<int> found = new();
    static readonly List<TileRun> runsNow = new();
    static readonly HashSet<long> seen = new();
    static readonly List<long> keys = new();

    static TopDownTreeBase() => TreeMark.Set(Tree);

    static void LogOnce(string key, string message)
    {
        if (logged.Add(key))
            TopDownMode.Log(message);
    }

    static void Fail(string where, Exception e)
    {
        errors++;
        lastErr = where + ": " + e;
        if (errors <= 3)
            TopDownMode.Log("tree base (" + where + "): " + e);
        if (errors >= 8)
        {
            enabled = false;
            TopDownMode.Log("tree base collision off after repeated errors.");
        }
    }

    static void Soften(TileBase b)
    {
        if (b.Soft)
            return;
        try
        {
            Tile t = Main.tile[b.I, b.J];
            t.inActive(true);
            b.Soft = true;
            softened++;
            if (softened == 1)
                LogOnce("soft", "tree base: inactive block (no game collision); inActive now = " + t.inActive());
        }
        catch (Exception e)
        {
            Fail("deactivating the block", e);
        }
    }

    static void RemoveBase(TileBase b)
    {
        int i = b.I, j = b.J;
        Tile t = Main.tile[i, j];
        t.inActive(false);
        try
        {
            WorldGen.KillTile(i, j, false, false, true);
        }
        catch (GameException)
        {
        }
        if (TileScan.TypeAt(i, j) >= 0)
        {
            try
            {
                t.sTileHeader = (short)(t.sTileHeader & ~TileScan.ActiveBit);
            }
            catch (Exception e)
            {
                Fail("removing the block", e);
            }
        }
        Known.Remove(TileScan.Key(i, j));
        removed++;
        if (removed <= 3)
            TopDownMode.Log("tree cut: base block removed at " + i + "," + j);
    }

    static bool GiveUpRepair(long key, long nowT, int i, int j, string why)
    {
        noRepair[key] = nowT;
        repairFails++;
        if (repairFails <= 3)
            TopDownMode.Log("tree without a base: could not put the block back at " + i + "," + j + " (" + why + "); collision uses the virtual base");
        if (repairFails >= 40 && !repairOff)
        {
            repairOff = true;
            TopDownMode.Log("putting the base back failed 40 times: off (collision continues through the virtual base).");
        }
        return false;
    }

    static bool RepairBase(int i, int j)
    {
        if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY)
            return false;
        long key = TileScan.Key(i, j);
        long nowT = Main.GameUpdateCount;
        if (noRepair.TryGetValue(key, out long when) && nowT - when < 600)
            return false;
        try
        {
            var count = new Dictionary<int, int>();
            int best = 2, bn = 0;
            foreach (TileBase known in Known.Values)
            {
                count.TryGetValue(known.T, out int c);
                count[known.T] = ++c;
                if (c > bn)
                {
                    bn = c;
                    best = known.T;
                }
            }
            Tile t = Main.tile[i, j];
            t.type = (ushort)best;
            t.sTileHeader = (short)(t.sTileHeader | TileScan.ActiveBit);
            t.invisibleBlock(true);
            if (TileScan.TypeAt(i, j) < 0)
                return GiveUpRepair(key, nowT, i, j, "the block did not stay active");
            repaired++;
            if (repaired <= 5)
                TopDownMode.Log("tree without a base: block put back at " + i + "," + j + " (type " + best + ")");
            var b = new TileBase { I = i, J = j, T = TileScan.TypeAt(i, j) };
            Known[key] = b;
            Soften(b);
            return true;
        }
        catch (Exception e)
        {
            return GiveUpRepair(key, nowT, i, j, e.Message);
        }
    }

    static void WidePass(int tx, int ty)
    {
        if (repairOff)
            return;
        int x0 = tx - WideR, x1 = tx + WideR, y0 = ty - WideR, y1 = ty + WideR;
        int fixedNow = 0;
        var wideFound = new List<int>();
        var wideRuns = new List<TileRun>();
        for (int by = y0; by <= y1; by += 32)
        {
            for (int bx = x0; bx <= x1; bx += 32)
            {
                TileScan.FindTiles(TreeMark, bx, by, Math.Min(bx + 31, x1), Math.Min(by + 31, y1), wideFound);
                if (wideFound.Count == 0)
                    continue;
                TileScan.Runs(wideFound, wideRuns);
                foreach (TileRun rn in wideRuns)
                {
                    int i = rn.I, j = rn.Bot;
                    int below = TileScan.TypeAt(i, j + 1);
                    if (below == Tree || below >= 0)
                        continue;
                    if (TileScan.TypeAt(i, j - 1) != Tree || TileScan.TypeAt(i, j - 2) != Tree)
                        continue;
                    if (RepairBase(i, j + 1))
                        fixedNow++;
                }
            }
        }
        wideFixed += fixedNow;
    }

    public static void RefreshBases(Player player)
    {
        if (!enabled)
            return;
        long now = Main.GameUpdateCount;
        if (now - tick < ScanEvery)
            return;
        tick = now;
        try
        {
            int tx = (int)Math.Floor((player.position.X + player.width / 2.0) / 16);
            int ty = (int)Math.Floor(((double)player.position.Y + player.height) / 16);
            TileScan.FindTiles(TreeMark, tx - ScanR, ty - ScanR, tx + ScanR, ty + ScanR, found);
            TileScan.Runs(found, runsNow);

            var nearNow = new List<TileBase>();
            seen.Clear();
            int virt = 0;
            foreach (TileRun rn in runsNow)
            {
                int i = rn.I, j = rn.Bot;
                int below = TileScan.TypeAt(i, j + 1);
                if (below == Tree)
                    continue;
                if (below < 0)
                {
                    if (rn.N < 3)
                        continue;
                    if (!repairOff && RepairBase(i, j + 1))
                        below = TileScan.TypeAt(i, j + 1);
                    if (below < 0)
                    {
                        nearNow.Add(new TileBase { I = i, J = j + 1, Virtual = true });
                        virt++;
                        continue;
                    }
                }
                long key = TileScan.Key(i, j + 1);
                if (!Known.TryGetValue(key, out TileBase? b))
                {
                    b = new TileBase { I = i, J = j + 1, T = below };
                    Known[key] = b;
                }
                Soften(b);
                nearNow.Add(b);
                seen.Add(key);
            }
            virtualNear = virt;
            try
            {
                nearNow.AddRange(TopDownCactus.CactusBases(tx, ty, ScanR));
            }
            catch (Exception e)
            {
                Fail("cactus bases", e);
            }
            near = nearNow;
            if (now - wideTick >= WideEvery)
            {
                wideTick = now;
                WidePass(tx, ty);
            }

            keys.Clear();
            keys.AddRange(Known.Keys);
            foreach (long key in keys)
            {
                if (seen.Contains(key))
                    continue;
                TileBase b = Known[key];
                if (Math.Abs(b.I - tx) > ScanR || Math.Abs(b.J - ty) > ScanR)
                    continue;
                if (TileScan.TypeAt(b.I, b.J - 1) == Tree)
                    continue;
                int cur = TileScan.TypeAt(b.I, b.J);
                if (cur != b.T)
                {
                    Known.Remove(key);
                    continue;
                }
                Tile t = Main.tile[b.I, b.J];
                if (!t.invisibleBlock())
                {
                    Known.Remove(key);
                    continue;
                }
                RemoveBase(b);
            }
        }
        catch (Exception e)
        {
            Fail("searching for bases", e);
        }
    }

    static void RectOf(TileBase b, out double l, out double r, out double t, out double m)
    {
        l = b.I * 16 - PadX;
        r = b.I * 16 + 16 + PadX;
        t = b.J * 16;
        m = b.J * 16 + BoxH;
    }

    public static void AdjustVelocity(Player player, ref double vx, ref double vy)
    {
        if (!enabled || near.Count == 0)
            return;
        double px = player.position.X, py = player.position.Y;
        double w = player.width, h = player.height;
        double top = py + h - FeetH, bot = py + h;
        foreach (TileBase b in near)
        {
            RectOf(b, out double rl, out double rr, out double rt, out double rm);
            if (vx != 0 && top < rm && bot > rt)
            {
                if (vx > 0 && px + w <= rl + 0.01 && px + w + vx > rl)
                    vx = Math.Max(0, rl - (px + w));
                else if (vx < 0 && px >= rr - 0.01 && px + vx < rr)
                    vx = Math.Min(0, rr - px);
            }
            double nx = px + vx;
            if (vy != 0 && nx < rr && nx + w > rl)
            {
                if (vy > 0 && bot <= rt + 0.01 && bot + vy > rt)
                    vy = Math.Max(0, rt - bot);
                else if (vy < 0 && top >= rm - 0.01 && top + vy < rm)
                    vy = Math.Min(0, rm - top);
            }
        }
    }

    public static bool OverlapFix(Player player, out double fixX, out double fixY)
    {
        fixX = fixY = 0;
        if (!enabled || near.Count == 0)
            return false;
        double px = player.position.X, py = player.position.Y;
        double w = player.width, h = player.height;
        double top = py + h - FeetH, bot = py + h;
        foreach (TileBase b in near)
        {
            RectOf(b, out double rl, out double rr, out double rt, out double rm);
            if (!(px < rr && px + w > rl && top < rm && bot > rt))
                continue;
            double d = (px + w) - rl;
            int sx = -1, sy = 0;
            if (rr - px < d)
            {
                d = rr - px;
                sx = 1;
                sy = 0;
            }
            if (bot - rt < d)
            {
                d = bot - rt;
                sx = 0;
                sy = -1;
            }
            if (rm - top < d)
            {
                d = rm - top;
                sx = 0;
                sy = 1;
            }
            fixX = sx * d;
            fixY = sy * d;
            return true;
        }
        return false;
    }

    public static string Clean(int radius)
    {
        Player p = Main.player[Main.myPlayer];
        int R = Math.Min(Math.Max(radius > 0 ? radius : 40, 5), 120);
        int tx = (int)Math.Floor(p.position.X / 16.0), ty = (int)Math.Floor(p.position.Y / 16.0);
        int checkedTiles = 0, gone = 0;
        for (int i = tx - R; i <= tx + R; i++)
        {
            for (int j = ty - R; j <= ty + R; j++)
            {
                if (TileScan.TypeAt(i, j) < 0)
                    continue;
                Tile t = Main.tile[i, j];
                checkedTiles++;
                if (t.invisibleBlock() && TileScan.TypeAt(i, j - 1) != Tree)
                {
                    RemoveBase(new TileBase { I = i, J = j, T = TileScan.TypeAt(i, j) });
                    gone++;
                }
            }
        }
        return "base cleanup: " + checkedTiles + " tile(s) checked, " + gone + " removed";
    }

    public static string Status() =>
        "bases: active=" + enabled + " known=" + Known.Count + " near=" + near.Count + " softened=" + softened + " removed=" + removed
        + " put back=" + repaired + " wide pass fixed=" + wideFixed + " virtual(near)=" + virtualNear + " errors=" + errors
        + (lastErr != "" ? " last error: " + lastErr : "");

    public static string Audit(int radius)
    {
        Player p = Main.player[Main.myPlayer];
        int R = Math.Min(Math.Max(radius > 0 ? radius : 12, 4), 30);
        int tx = (int)Math.Floor(p.position.X / 16.0), ty = (int)Math.Floor(p.position.Y / 16.0);
        var list = new List<int>();
        TileScan.FindTiles(TreeMark, tx - R, ty - R, tx + R, ty + R, list);
        var have = new HashSet<long>();
        var low = new SortedDictionary<int, int>();
        var high = new Dictionary<int, int>();
        for (int k = 0; k < list.Count; k += 2)
        {
            int i = list[k], j = list[k + 1];
            have.Add(TileScan.Key(i, j));
            if (!low.TryGetValue(i, out int lo) || lo < j)
                low[i] = j;
            if (!high.TryGetValue(i, out int hi) || hi > j)
                high[i] = j;
        }
        var lines = new StringBuilder();
        foreach (KeyValuePair<int, int> entry in low)
        {
            int i = entry.Key, j = entry.Value;
            int n = 1;
            while (n < 40 && have.Contains(TileScan.Key(i, j - n)))
                n++;
            int below = TileScan.TypeAt(i, j + 1);
            string flags = "";
            if (below >= 0)
            {
                Tile t = Main.tile[i, j + 1];
                flags = " inActive=" + t.inActive() + " invisible=" + t.invisibleBlock();
            }
            bool virt = false, real = false;
            foreach (TileBase b in near)
            {
                if (b.I == i && b.J == j + 1)
                {
                    virt |= b.Virtual;
                    real |= !b.Virtual;
                }
            }
            lines.Append("\n  col ").Append(i).Append(" foot j=").Append(j).Append(" trunk run=").Append(n).Append(" (tiles in column: ").Append(j - high[i] + 1)
                .Append(") below=").Append(below).Append(flags)
                .Append(n >= 3 ? (real ? " collision=block" : (virt ? " collision=VIRTUAL" : " collision=NONE")) : " (not a trunk)");
        }
        string result = "trees near (" + low.Count + " columns, " + list.Count / 2 + " tree tiles):" + (lines.Length > 0 ? lines.ToString() : " none");
        TopDownMode.Log(result);
        return result;
    }
}
