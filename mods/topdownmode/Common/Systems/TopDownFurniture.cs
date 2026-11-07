using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ObjectData;
using topdownmode.Common.Configs;
using topdownmode.Common.Players;

namespace topdownmode.Common.Systems;

// Furniture stands anywhere: when the game refuses a placement or breaks an object for lacking
// ground, the check runs again with a temporary row of dirt under it, and furniture can stack.
public static class TopDownFurniture
{
    const int Half = 6;
    const int Tree = TileID.Trees;
    const int Support = TileID.Dirt;
    const short ActiveBit = 0x20;

    enum Kind { PlaceObject, CanPlace, Survive, Anchors, Torch, Frame }

    sealed class Stat
    {
        public long Calls, Refused, Retried, Fixed, Stacked;
    }

    sealed class Candidate
    {
        public int X0, X1, Row;
        public bool HasSource;
        public int Sx0, Sx1;
    }

    sealed class FrameInfo
    {
        public int X, Y, Type, Xl, Xr, Yb, Yt;
    }

    sealed class Geometry
    {
        public int W, H, Ox, Oy;
    }

    sealed class SupportRow
    {
        readonly List<(Tile Tile, short Header, ushort Type)> saved = new();

        public int N => saved.Count;

        public void Add(Tile tile, short header, ushort type) => saved.Add((tile, header, type));

        public void Undo()
        {
            for (int i = saved.Count - 1; i >= 0; i--)
            {
                (Tile tile, short header, ushort type) = saved[i];
                try
                {
                    tile.sTileHeader = header;
                    tile.type = type;
                }
                catch (Exception e)
                {
                    Fail(e);
                }
            }
        }
    }

    sealed class SolidSwap
    {
        readonly List<(int Type, bool Solid, bool Top, bool NoAttach)> saved = new();

        public int N => saved.Count;

        public void Add(int type, bool solid, bool top, bool noAttach) => saved.Add((type, solid, top, noAttach));

        public void Undo()
        {
            for (int i = saved.Count - 1; i >= 0; i--)
            {
                (int type, bool solid, bool top, bool noAttach) = saved[i];
                try
                {
                    Main.tileSolid[type] = solid;
                    Main.tileSolidTop[type] = top;
                    Main.tileNoAttach[type] = noAttach;
                }
                catch (Exception e)
                {
                    Fail(e);
                }
            }
        }
    }

    static bool on = true, live, lazyTried, frameHooked;
    static int errors, depth, depthF, tickN;
    static string lastErr = "";
    static readonly List<string> trace = new();
    static readonly List<string> kills = new();
    static readonly Stat[] stats = { new(), new(), new(), new(), new(), new() };
    static readonly Dictionary<long, Geometry?> geoCache = new();
    static readonly Dictionary<int, bool> objCache = new();

    static bool Active()
    {
        if (!on)
            return false;
        if (!(TopDownPlayer.On && TopDownPlayer.Active))
            return false;
        if (Main.gameMenu)
            return false;
        return TopDownConfig.Instance.FurnitureTopDown;
    }

    static void Fail(Exception e)
    {
        errors++;
        string text = e.ToString();
        lastErr = text.Length > 200 ? text[..200] : text;
        if (errors <= 3)
            TopDownMode.Log("furniture (error): " + lastErr);
    }

    static void Trace(string s)
    {
        trace.Add(s);
        if (trace.Count > 14)
            trace.RemoveAt(0);
    }

    static bool InWorld(int x, int y) => x >= 2 && y >= 2 && x < Main.maxTilesX - 2 && y < Main.maxTilesY - 2;

    static bool IsActiveTile(in Tile t) => (t.sTileHeader & ActiveBit) != 0;

    static SupportRow AddSupport(int x0, int x1, int row)
    {
        var saved = new SupportRow();
        for (int x = x0; x <= x1; x++)
        {
            if (!InWorld(x, row))
                continue;
            try
            {
                Tile t = Main.tile[x, row];
                short h = t.sTileHeader;
                if ((h & ActiveBit) != 0)
                    continue;
                saved.Add(t, h, t.type);
                t.type = Support;
                t.sTileHeader = (short)(h | ActiveBit);
            }
            catch (Exception e)
            {
                Fail(e);
            }
        }
        return saved;
    }

    static HashSet<int> ObjTypes(int row, int x0, int x1)
    {
        var result = new HashSet<int>();
        for (int x = x0; x <= x1; x++)
        {
            if (!InWorld(x, row))
                continue;
            Tile u = Main.tile[x, row];
            if (IsActiveTile(u) && IsObjectType(u.type))
                result.Add(u.type);
        }
        return result;
    }

    static SolidSwap PushSolid(HashSet<int> types)
    {
        var saved = new SolidSwap();
        foreach (int ty in types)
        {
            bool sol = Main.tileSolid[ty], top = Main.tileSolidTop[ty], noa = Main.tileNoAttach[ty];
            if (sol && !top && !noa)
                continue;
            Main.tileSolid[ty] = true;
            Main.tileSolidTop[ty] = false;
            Main.tileNoAttach[ty] = false;
            saved.Add(ty, sol, top, noa);
        }
        return saved;
    }

    static Geometry? GeometryOf(int type, int style)
    {
        long key = ((long)type << 32) | (uint)style;
        if (geoCache.TryGetValue(key, out Geometry? g))
            return g;
        g = null;
        try
        {
            TileObjectData d = TileObjectData.GetTileData(type, style, 0);
            if (!d.IsNull)
            {
                int w = d.Width, h = d.Height;
                if (w > 0 && h > 0)
                    g = new Geometry { W = w, H = h, Ox = d.Origin.X, Oy = d.Origin.Y };
            }
        }
        catch (Exception e)
        {
            Fail(e);
        }
        geoCache[key] = g;
        return g;
    }

    static Candidate PlanObject(int x, int y, int type, int style)
    {
        Geometry? g = GeometryOf(type, style);
        if (g != null)
            return new Candidate { X0 = x - g.Ox - 2, X1 = x - g.Ox + g.W + 1, Row = y - g.Oy + g.H, HasSource = true, Sx0 = x - g.Ox, Sx1 = x - g.Ox + g.W - 1 };
        return new Candidate { X0 = x - Half, X1 = x + Half, Row = y + 1, HasSource = true, Sx0 = x, Sx1 = x };
    }

    static Candidate? PlanSurvive(int x, int y)
    {
        if (!InWorld(x, y))
            return null;
        Tile t = Main.tile[x, y];
        if (!IsActiveTile(t))
            return null;
        int type = t.type;
        if (type == Tree || !Main.tileFrameImportant[type])
            return null;
        int yb = y + 1, pv = y;
        while (yb < y + 16 && InWorld(x, yb))
        {
            Tile u = Main.tile[x, yb];
            if (!IsActiveTile(u) || u.type != type)
                break;
            Tile previous = Main.tile[x, pv];
            if (u.frameY <= previous.frameY)
                break;
            pv = yb;
            yb++;
        }
        return new Candidate { X0 = x - Half, X1 = x + Half, Row = yb, HasSource = true, Sx0 = x, Sx1 = x };
    }

    static bool IsObjectType(int type)
    {
        if (!objCache.TryGetValue(type, out bool v))
        {
            v = type != Tree && Main.tileFrameImportant[type];
            objCache[type] = v;
        }
        return v;
    }

    static bool SameObject(int a, int b, int type)
    {
        if (!InWorld(a, b))
            return false;
        Tile u = Main.tile[a, b];
        return IsActiveTile(u) && u.type == type;
    }

    static short FrameYAt(int a, int b)
    {
        Tile u = Main.tile[a, b];
        return u.frameY;
    }

    static FrameInfo? PlanFrame(int x, int y)
    {
        if (!InWorld(x, y))
            return null;
        Tile t = Main.tile[x, y];
        if (!IsActiveTile(t))
            return null;
        int type = t.type;
        if (!IsObjectType(type))
            return null;
        int yb = y + 1, pv = y;
        while (yb < y + 16 && SameObject(x, yb, type) && !(FrameYAt(x, yb) <= FrameYAt(x, pv)))
        {
            pv = yb;
            yb++;
        }
        int yt = y - 1;
        pv = y;
        while (yt > y - 16 && SameObject(x, yt, type) && !(FrameYAt(x, yt) >= FrameYAt(x, pv)))
        {
            pv = yt;
            yt--;
        }
        int xl = x;
        while (xl > x - 8 && SameObject(xl - 1, y, type))
            xl--;
        int xr = x;
        while (xr < x + 8 && SameObject(xr + 1, y, type))
            xr++;
        return new FrameInfo { X = x, Y = y, Type = type, Xl = xl, Xr = xr, Yb = yb, Yt = yt };
    }

    static Candidate? PlanAnchors(int sx, int sy, int w, int h)
    {
        if (w < 1 || h < 1 || w > 16 || h > 16)
            return null;
        return new Candidate { X0 = sx - 1, X1 = sx + w, Row = sy + h, HasSource = true, Sx0 = sx, Sx1 = sx + w - 1 };
    }

    // The slow path of the placement checks: the game said no, so ask again with ground under it,
    // first plain dirt and then with the furniture below counting as solid.
    static bool Retry(Kind kind, int x, int y, Candidate? c, Func<bool> call)
    {
        Stat s = stats[(int)kind];
        if (c == null)
            return false;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            bool stack = attempt == 1;
            if (stack && !c.HasSource)
                continue;
            SupportRow? sup = null;
            SolidSwap? sol = null;
            try
            {
                sup = AddSupport(c.X0, c.X1, c.Row);
                if (stack)
                {
                    HashSet<int> types = ObjTypes(c.Row, c.Sx0, c.Sx1);
                    if (types.Count > 0)
                    {
                        sol = PushSolid(types);
                        if (sol.N == 0)
                            sol = null;
                    }
                }
            }
            catch (Exception e)
            {
                Fail(e);
                sup?.Undo();
                sol?.Undo();
                continue;
            }
            if (stack ? sol == null : sup.N == 0)
            {
                sup.Undo();
                continue;
            }
            s.Retried++;
            bool r2 = false;
            depth++;
            try
            {
                r2 = call();
            }
            catch (Exception e)
            {
                Fail(e);
            }
            finally
            {
                depth--;
                sup.Undo();
                sol?.Undo();
            }
            if (r2)
            {
                s.Fixed++;
                if (stack)
                    s.Stacked++;
                if (s.Fixed <= 6)
                {
                    string m = kind + " ok " + x + "," + y + " (row " + c.Row + ", " + sup.N + " supports" + (stack ? ", STACKED on furniture" : "") + ")";
                    Trace(m);
                    if (s.Fixed <= 2 || stack)
                        TopDownMode.Log("furniture: " + m);
                }
                return true;
            }
        }
        return false;
    }

    static bool Refused(Kind kind)
    {
        Stat s = stats[(int)kind];
        if (depth > 0)
            return false;
        try
        {
            if (!Active())
                return false;
        }
        catch (Exception e)
        {
            Fail(e);
            return false;
        }
        s.Refused++;
        return true;
    }

    static bool RetryPlaceObject(On_WorldGen.orig_PlaceObject orig, int x, int y, int type, bool mute, int style, int alternate, int random, int direction) =>
        Retry(Kind.PlaceObject, x, y, PlanObject(x, y, type, style), () => orig(x, y, type, mute, style, alternate, random, direction));

    static bool RetryCanPlace(On_TileObject.orig_CanPlace orig, int x, int y, int type, int style, int dir, ref TileObject objectData, bool onlyCheck, BunnyLoader.Interop.GameNullable<int> forcedRandom)
    {
        TileObject data = objectData;
        bool r = Retry(Kind.CanPlace, x, y, PlanObject(x, y, type, style), () => orig(x, y, type, style, dir, out data, onlyCheck, forcedRandom));
        objectData = data;
        return r;
    }

    static bool RetrySurvive(On_WorldGen.orig_CheckTileBreakability2_ShouldTileSurvive orig, int x, int y) =>
        Retry(Kind.Survive, x, y, PlanSurvive(x, y), () => orig(x, y));

    static bool RetryAnchors(On_WorldGen.orig_CheckTileAnchors orig, int sx, int sy, int w, int h, int mode, Terraria.Enums.AnchorType anchor) =>
        Retry(Kind.Anchors, sx, sy, PlanAnchors(sx, sy, w, h), () => orig(sx, sy, w, h, mode, anchor));

    static void InstallEager()
    {
        On_WorldGen.PlaceObject += (orig, x, y, type, mute, style, alternate, random, direction) =>
        {
            bool r = orig(x, y, type, mute, style, alternate, random, direction);
            stats[(int)Kind.PlaceObject].Calls++;
            if (r || !Refused(Kind.PlaceObject))
                return r;
            return RetryPlaceObject(orig, x, y, type, mute, style, alternate, random, direction);
        };
        On_TileObject.CanPlace += (orig, x, y, type, style, dir, out objectData, onlyCheck, forcedRandom) =>
        {
            bool r = orig(x, y, type, style, dir, out objectData, onlyCheck, forcedRandom);
            stats[(int)Kind.CanPlace].Calls++;
            if (r || !Refused(Kind.CanPlace))
                return r;
            return RetryCanPlace(orig, x, y, type, style, dir, ref objectData, onlyCheck, forcedRandom);
        };
        On_WorldGen.CheckTileBreakability2_ShouldTileSurvive += (orig, x, y) =>
        {
            bool r = orig(x, y);
            stats[(int)Kind.Survive].Calls++;
            if (r || !Refused(Kind.Survive))
                return r;
            return RetrySurvive(orig, x, y);
        };
        On_WorldGen.CheckTileAnchors += (orig, sx, sy, w, h, mode, anchor) =>
        {
            bool r = orig(sx, sy, w, h, mode, anchor);
            stats[(int)Kind.Anchors].Calls++;
            if (r || !Refused(Kind.Anchors))
                return r;
            return RetryAnchors(orig, sx, sy, w, h, mode, anchor);
        };
        On_WorldGen.CheckTorch += (orig, x, y) =>
        {
            Stat s = stats[(int)Kind.Torch];
            s.Calls++;
            SupportRow? sup = null;
            try
            {
                if (depth == 0 && Active() && InWorld(x, y) && IsActiveTile(Main.tile[x, y]))
                {
                    sup = AddSupport(x - 1, x + 1, y + 1);
                    if (sup.N == 0)
                    {
                        sup = null;
                    }
                    else
                    {
                        s.Retried++;
                        if (s.Retried <= 3)
                            Trace("torch support at " + x + "," + y);
                    }
                }
            }
            catch (Exception e)
            {
                Fail(e);
                sup = null;
            }
            if (sup == null)
            {
                orig(x, y);
                return;
            }
            depth++;
            try
            {
                orig(x, y);
            }
            finally
            {
                depth--;
                sup.Undo();
            }
        };
    }

    // Installed when the first world is playing, so world generation never passes through them.
    static void InstallLazy()
    {
        On_WorldGen.TileFrame += (orig, i, j, resetFrame, noBreak) =>
        {
            if (!live || depthF > 6 || noBreak)
            {
                orig(i, j, resetFrame, noBreak);
                return;
            }
            Stat s = stats[(int)Kind.Frame];
            s.Calls++;
            List<SupportRow>? sups = null;
            SolidSwap? sol = null;
            FrameInfo? info = null;
            try
            {
                info = PlanFrame(i, j);
                if (info != null)
                {
                    sups = new List<SupportRow>(2);
                    SupportRow bottom = AddSupport(info.Xl - 1, info.Xr + 1, info.Yb);
                    if (bottom.N > 0)
                        sups.Add(bottom);
                    SupportRow topRow = AddSupport(info.Xl - 1, info.Xr + 1, info.Yt);
                    if (topRow.N > 0)
                        sups.Add(topRow);
                    if (sups.Count == 0)
                        sups = null;
                    HashSet<int> types = ObjTypes(info.Yb, info.Xl, info.Xr);
                    types.UnionWith(ObjTypes(info.Yt, info.Xl, info.Xr));
                    if (types.Count > 0)
                    {
                        SolidSwap p = PushSolid(types);
                        if (p.N > 0)
                            sol = p;
                    }
                }
            }
            catch (Exception e)
            {
                Fail(e);
                if (sups != null)
                {
                    for (int k = sups.Count - 1; k >= 0; k--)
                        sups[k].Undo();
                }
                sol?.Undo();
                sups = null;
                sol = null;
            }
            if (sups == null && sol == null)
            {
                orig(i, j, resetFrame, noBreak);
                return;
            }
            s.Retried++;
            depthF++;
            try
            {
                orig(i, j, resetFrame, noBreak);
            }
            finally
            {
                depthF--;
                if (sups != null)
                {
                    for (int k = sups.Count - 1; k >= 0; k--)
                        sups[k].Undo();
                }
                sol?.Undo();
                try
                {
                    Tile u = Main.tile[info!.X, info.Y];
                    if (IsActiveTile(u) && u.type == info.Type)
                    {
                        s.Fixed++;
                        if (sol != null)
                            s.Stacked++;
                    }
                    else
                    {
                        s.Refused++;
                        Trace("frame: tile " + info.Type + " gone at " + info.X + "," + info.Y + " even with support");
                    }
                }
                catch (Exception e)
                {
                    Fail(e);
                }
            }
        };
        frameHooked = true;
        On_WorldGen.KillTile += (orig, i, j, fail, effectOnly, noItem) =>
        {
            try
            {
                if (live && !fail && InWorld(i, j))
                {
                    Tile t = Main.tile[i, j];
                    if (IsActiveTile(t) && IsObjectType(t.type))
                    {
                        kills.Add("tile " + t.type + " at " + i + "," + j + " [" + (depthF > 0 ? "inside TileFrame WITH support" : depth > 0 ? "inside a retry" : "outside our hooks (player or game)") + "]");
                        if (kills.Count > 12)
                            kills.RemoveAt(0);
                    }
                }
            }
            catch (Exception e)
            {
                Fail(e);
            }
            orig(i, j, fail, effectOnly, noItem);
        };
    }

    static void Tick()
    {
        if ((tickN++ & 15) != 0)
            return;
        bool now = false;
        try
        {
            now = Active();
        }
        catch (Exception e)
        {
            Fail(e);
        }
        live = now;
        if (now && !lazyTried)
        {
            lazyTried = true;
            try
            {
                InstallLazy();
            }
            catch (Exception e)
            {
                Fail(e);
            }
            TopDownMode.Log("furniture: TileFrame " + (frameHooked ? "hooked (support during the tile update)" : "NOT hooked"));
        }
    }

    public static void InstallFurniture()
    {
        on = true;
        live = false;
        InstallEager();
        On_Player.Update += (orig, self, i) =>
        {
            orig(self, i);
            try
            {
                Tick();
            }
            catch (Exception e)
            {
                Fail(e);
            }
        };
        TopDownMode.Log("furniture: hooks installed (TileFrame joins when a world loads)");
    }

    public static void UninstallFurniture()
    {
        on = false;
        live = false;
    }

    public static string Status()
    {
        string columns = string.Join(" ", Enum.GetValues<Kind>().Select(k =>
        {
            Stat s = stats[(int)k];
            return k + "[" + s.Calls + "/" + s.Refused + "/" + s.Retried + "/" + s.Fixed + (s.Stacked > 0 ? " stacked=" + s.Stacked : "") + "]";
        }));
        return "furniture: " + columns + " (calls/refused/retried/passed) live=" + live + " errors=" + errors + (lastErr != "" ? " last error: " + lastErr : "")
            + (kills.Count > 0 ? " | objects broken recently: " + string.Join(" ; ", kills.TakeLast(6)) : "")
            + (trace.Count > 0 ? " | last events: " + string.Join(" ; ", trace.TakeLast(6)) : "");
    }
}
