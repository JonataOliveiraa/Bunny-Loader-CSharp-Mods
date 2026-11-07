using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using topdownmode.Common.Players;
using topdownmode.Common.Utils;

namespace topdownmode.Common.Systems;

// Cacti stop being solid; their sand base collides like a tree's, and it is switched off around
// the game's own collision of the local player so it makes no invisible wall.
public static class TopDownCactus
{
    public const int Cactus = TileID.Cactus;
    public static readonly TypeMark CactusMark = new();

    static bool enabled = true;
    static bool? savedSolid;
    static List<TileBase> nearList = new();
    static readonly Dictionary<long, TileBase> known = new();
    static readonly List<int> found = new();
    static readonly List<TileRun> runs = new();
    static readonly HashSet<long> seen = new();
    static readonly List<long> keys = new();
    static readonly List<(int I, int J)> tokens = new();
    static readonly HashSet<string> logged = new();
    static int suspended, removed, errors, lastNear;
    static string lastErr = "";

    static bool IsSand(int type) => type == TileID.Sand || type == TileID.Ebonsand || type == TileID.Pearlsand || type == TileID.Crimsand;

    static void LogOnce(string key, string message)
    {
        if (logged.Add(key))
            TopDownMode.Log("cactus: " + message);
    }

    static void Fail(string where, Exception e)
    {
        errors++;
        lastErr = where + ": " + e;
        if (errors <= 3)
            TopDownMode.Log("cactus (" + where + "): " + e);
        if (errors >= 8)
        {
            enabled = false;
            TopDownMode.Log("cactus: off after repeated errors (back to the old behavior).");
        }
    }

    static void CactusNotSolid()
    {
        savedSolid ??= Main.tileSolid[Cactus];
        if (Main.tileSolid[Cactus])
            Main.tileSolid[Cactus] = false;
        LogOnce("solid", "cactus tile (" + Cactus + "): solid before = " + savedSolid + ", now = " + Main.tileSolid[Cactus]);
    }

    public static void InstallCactus()
    {
        enabled = true;
        CactusMark.Set(Cactus);
    }

    public static void UninstallCactus()
    {
        enabled = false;
        if (savedSolid != null)
            Main.tileSolid[Cactus] = savedSolid.Value;
        savedSolid = null;
    }

    // True when the caller must call ResumeBases afterwards.
    public static bool SuspendBases(Vector2 position, int width, int height)
    {
        if (!enabled || nearList.Count == 0)
            return false;
        try
        {
            Player p = Main.player[Main.myPlayer];
            if (p.IsNull)
                return false;
            if (Math.Abs(position.X - p.position.X) > 0.01 || Math.Abs(position.Y - p.position.Y) > 0.01)
                return false;
            if (width != p.width || height != p.height)
                return false;
            int tx = (int)Math.Floor((p.position.X + p.width / 2.0) / 16), ty = (int)Math.Floor(((double)p.position.Y + p.height) / 16);
            tokens.Clear();
            foreach (TileBase b in nearList)
            {
                if (Math.Abs(b.I - tx) > 4 || b.J < ty - 6 || b.J > ty + 8)
                    continue;
                for (int j = b.J; j <= b.J + 1; j++)
                {
                    if (TileScan.TypeAt(b.I, j) < 0)
                        continue;
                    Tile t = Main.tile[b.I, j];
                    if (j != b.J && !t.invisibleBlock())
                        continue;
                    if (t.inActive())
                        continue;
                    t.inActive(true);
                    tokens.Add((b.I, j));
                }
            }
            suspended += tokens.Count > 0 ? 1 : 0;
            return true;
        }
        catch (Exception e)
        {
            Fail("suspending bases", e);
            return false;
        }
    }

    public static void ResumeBases()
    {
        foreach ((int i, int j) in tokens)
        {
            try
            {
                Tile t = Main.tile[i, j];
                t.inActive(false);
            }
            catch (Exception e)
            {
                Fail("restoring a base", e);
            }
        }
        tokens.Clear();
    }

    static void RemoveCactusBase(TileBase b)
    {
        for (int j = b.J + 1; j >= b.J; j--)
        {
            if (TileScan.TypeAt(b.I, j) < 0)
                continue;
            Tile t = Main.tile[b.I, j];
            if (j != b.J && !t.invisibleBlock())
                continue;
            t.inActive(false);
            try
            {
                WorldGen.KillTile(b.I, j, false, false, true);
            }
            catch (GameException)
            {
            }
            if (TileScan.TypeAt(b.I, j) >= 0)
            {
                try
                {
                    t.sTileHeader = (short)(t.sTileHeader & ~TileScan.ActiveBit);
                }
                catch (Exception e)
                {
                    Fail("removing a base", e);
                }
            }
        }
        known.Remove(TileScan.Key(b.I, b.J));
        removed++;
    }

    public static List<TileBase> CactusBases(int tx, int ty, int R)
    {
        if (!enabled)
        {
            nearList = new List<TileBase>();
            return nearList;
        }
        var result = new List<TileBase>();
        try
        {
            CactusNotSolid();
            TileScan.FindTiles(CactusMark, tx - R, ty - R, tx + R, ty + R, found);
            seen.Clear();
            TileScan.Runs(found, runs);
            foreach (TileRun run in runs)
            {
                int below = TileScan.TypeAt(run.I, run.Bot + 1);
                if (!IsSand(below))
                    continue;
                long key = TileScan.Key(run.I, run.Bot + 1);
                if (!known.ContainsKey(key))
                    known[key] = new TileBase { I = run.I, J = run.Bot + 1, T = below };
                seen.Add(key);
                result.Add(new TileBase { I = run.I, J = run.Bot + 1 });
            }
            keys.Clear();
            keys.AddRange(known.Keys);
            foreach (long key in keys)
            {
                if (seen.Contains(key))
                    continue;
                TileBase b = known[key];
                if (Math.Abs(b.I - tx) > R || Math.Abs(b.J - ty) > R)
                    continue;
                if (TileScan.TypeAt(b.I, b.J - 1) == Cactus)
                    continue;
                if (TileScan.TypeAt(b.I, b.J) != b.T)
                {
                    known.Remove(key);
                    continue;
                }
                Tile t = Main.tile[b.I, b.J];
                if (!t.invisibleBlock())
                {
                    known.Remove(key);
                    continue;
                }
                RemoveCactusBase(b);
            }
        }
        catch (Exception e)
        {
            Fail("searching for bases", e);
        }
        lastNear = result.Count;
        nearList = result;
        return result;
    }

    public static string Clean(int radius)
    {
        Player p = Main.player[Main.myPlayer];
        int R = Math.Min(Math.Max(radius > 0 ? radius : 40, 5), 120);
        int tx = (int)Math.Floor(p.position.X / 16.0), ty = (int)Math.Floor(p.position.Y / 16.0);
        int gone = 0;
        for (int i = tx - R; i <= tx + R; i++)
        {
            for (int j = ty - R; j <= ty + R; j++)
            {
                int type = TileScan.TypeAt(i, j);
                if (!IsSand(type) || TileScan.TypeAt(i, j - 1) == Cactus)
                    continue;
                Tile t = Main.tile[i, j];
                if (!t.invisibleBlock())
                    continue;
                RemoveCactusBase(new TileBase { I = i, J = j, T = type });
                gone++;
            }
        }
        return "cactus base cleanup: " + gone + " removed";
    }

    public static string Status() =>
        "cactus: active=" + enabled + " tileSolid(" + Cactus + ")=" + Main.tileSolid[Cactus] + " (before " + savedSolid + ") bases(near)=" + lastNear
        + " known=" + known.Count + " collisions without the base=" + suspended + " removed=" + removed + " errors=" + errors + (lastErr != "" ? " last error: " + lastErr : "");
}
