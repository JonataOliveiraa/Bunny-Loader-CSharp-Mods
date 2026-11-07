using System;
using System.Collections.Generic;
using System.Text;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Renderers;
using Terraria.ID;
using Terraria.ModLoader;
using topdownmode.Common.Configs;
using topdownmode.Common.Players;
using topdownmode.Common.Utils;
using topdownmode.Content.Walls;

namespace topdownmode.Common.Systems;

// Y-sort. The game draws entities over trees and furniture; here an entity north of an object is
// drawn before the tiles, so the object covers it, and one south of it is drawn again after them.
// Entities standing in water are drawn lowered and clipped at the waterline.
public static class TopDownTreeDepth
{
    enum Mode { None, Behind, Front }

    enum NpcKind { Skip, Air, Ground }

    public struct Swimmer
    {
        public double X, Y, Hw;
    }

    struct Trunk
    {
        public int Cx, BaseY;
    }

    struct TreeScan
    {
        public bool Behind, BehindTrunk, BehindTop, Front, FrontTrunk;
        public double BehindOv, FrontOv;
    }

    struct DrawArgs
    {
        public bool Set;
        public LegacyPlayerRenderer Renderer;
        public float Rot, Scale, Ox, Oy;
    }

    struct SwimGeom
    {
        public double Sink, Px, Py, W, H, Water;
    }

    const int Tree = TileID.Trees;
    const int Grass = TileID.Plants;
    const int RefreshMs = 30;
    const int NpcMaxSize = 72;
    const int NpcCull = 240;
    const int GrassLine = 10;
    const int FurnLine = 8;
    const int CactusLine = 8;
    const int MaxEntity = 256;
    const int MaxNpc = 201;

    static readonly int[] FurnDefault =
    {
        14, 15, 16, 17, 18, 21, 26, 77, 79, 85, 86, 87, 88, 89, 90, 92, 93, 94, 96, 100, 101, 104, 105, 106, 114, 125, 128, 133, 134, 139, 172,
        207, 209, 215, 217, 218, 219, 220, 228, 247, 354, 355, 356, 377, 467,
    };

    static readonly TypeMark grassMark = new(), furnMark = new();
    static readonly List<int> grassTypes = new(), furnTypes = new();

    static bool enabled = true, hooked, npcHooked, npcs = true, grass = true, furniture = true, cactus = true;
    static bool blitErr, busy, inPass;
    static int passId, redrawn = -1, blitMiss, blitLogs, blits, errors, npcErrors, dbgLogs;
    static string order = "", lastErr = "";
    static int grassFront, grassBehind, furnFront, furnBehind, cactusFront, cactusBehind;
    static int lastFound, noBaseTrees;
    static int firstT = -1, firstP = -1, seqCount;
    static long statPasses, statNative, statEarly, statRedraw, statSkipped, statBehind, statFront, statFrontWins;
    static long statNpcNative, statNpcEarly, statNpcRedraw, statNpcSkipped, statGrassEarly, statFurnEarly, statCactusEarly, statSwimDraw;

    static readonly Mode[] mode = new Mode[MaxEntity], nmode = new Mode[MaxNpc];
    static readonly long[] modeTime = NewTimes(MaxEntity), nmodeTime = NewTimes(MaxNpc);
    static readonly List<Trunk>?[] trees = new List<Trunk>?[MaxEntity], ntrees = new List<Trunk>?[MaxNpc];
    static readonly int[] swim = new int[MaxEntity], nswim = new int[MaxNpc];
    static readonly int[] early = new int[MaxEntity], nearly = new int[MaxNpc], nat = new int[MaxEntity], nnat = new int[MaxNpc];
    static readonly DrawArgs[] args = new DrawArgs[MaxEntity];
    static GameHandle?[] argOffsets = new GameHandle?[MaxEntity];

    static bool idsReady;
    static int waterShallow, waterDeep, stone1, stone2, stone3;

    static readonly List<int> found = new();
    static readonly List<TileRun> runs = new();
    static readonly HashSet<long> cactusHas = new(), cactusDone = new();
    static readonly Stack<(int I, int J)> cactusStack = new();

    public static readonly List<Swimmer> Swimmers = new();

    static long[] NewTimes(int n)
    {
        var times = new long[n];
        Array.Fill(times, -1);
        return times;
    }

    static void Dbg(string message)
    {
        if (dbgLogs++ < 25)
            TopDownMode.Log("depth: " + message);
    }

    static void Fail(string where, Exception e)
    {
        errors++;
        lastErr = where + ": " + e;
        if (errors <= 3)
            TopDownMode.Log("tree depth (" + where + "): " + e);
        if (errors >= 6)
        {
            enabled = false;
            TopDownMode.Log("tree depth off after repeated errors.");
        }
    }

    static bool Active()
    {
        if (!enabled || Main.gameMenu)
            return false;
        if (!TopDownWorldSystem.IsTopDown)
            return false;
        return TopDownConfig.Instance.TreeDepth;
    }

    static bool NpcOn() => npcs && TopDownConfig.Instance.NpcDepth;

    static bool SwimOn() => TopDownConfig.Instance.Swim;

    static bool FurnOn() => furniture && TopDownConfig.Instance.FurnitureDepth;

    static bool CactusOn() => cactus && TopDownConfig.Instance.CactusDepth;

    static bool GrassOn() => grass && TopDownConfig.Instance.GrassDepth;

    static void ReadWaterIds()
    {
        if (idsReady)
            return;
        idsReady = true;
        waterShallow = ModContent.WallType<TopDownShallow>();
        waterDeep = ModContent.WallType<TopDownDeep>();
        stone1 = ModContent.WallType<TopDownShallowStone1>();
        stone2 = ModContent.WallType<TopDownShallowStone2>();
        stone3 = ModContent.WallType<TopDownShallowStone3>();
    }

    static int WaterKind(double left, double right, double feet)
    {
        ReadWaterIds();
        int w = TileScan.WallAt((int)Math.Floor((left + right) / 32), (int)Math.Floor((feet - 2) / 16));
        if (w <= 0)
            return 0;
        if (w == waterDeep)
            return 2;
        return w == waterShallow || w == stone1 || w == stone2 || w == stone3 ? 1 : 0;
    }

    static TreeScan ScanTrees(double left, double right, double top, double feet, int key, List<Trunk>?[] store)
    {
        var scan = new TreeScan();
        int x0 = (int)Math.Floor(left / 16) - 7, x1 = (int)Math.Floor(right / 16) + 7;
        int y0 = (int)Math.Floor(top / 16) - 10, y1 = (int)Math.Floor(feet / 16) + 30;
        TileScan.FindTiles(TopDownTreeBase.TreeMark, x0, y0, x1, y1, found);
        lastFound = found.Count / 2;
        List<Trunk> list = store[key] ??= new List<Trunk>();
        list.Clear();
        if (found.Count == 0)
            return scan;

        TileScan.Runs(found, runs);
        foreach (TileRun rn in runs)
        {
            int i = rn.I, j = rn.Bot;
            int below = TileScan.TypeAt(i, j + 1);
            if (below == Tree)
                continue;
            int n = rn.N;
            if (below < 0)
            {
                if (n < 3)
                    continue;
                noBaseTrees++;
            }
            else if (n < 3)
            {
                bool trunkNear = false;
                foreach (TileRun o in runs)
                {
                    if (!(o.I == rn.I && o.Top == rn.Top) && o.N >= 3 && Math.Abs(o.I - i) <= 2 && Math.Abs(o.Bot - j) <= 3)
                    {
                        trunkNear = true;
                        break;
                    }
                }
                if (trunkNear)
                    continue;
            }
            int gy = j + 1;
            int cx = i * 16 + 8, baseY = gy * 16, line = baseY + 8;
            int topJ = rn.Top;
            int tx0 = cx - 64, tx1 = cx + 64, ty0 = topJ * 16 - 72, ty1 = baseY + 4;
            if (right < tx0 || left > tx1 || feet < ty0 || top > ty1)
                continue;
            bool overTrunk = right > cx - 12 && left < cx + 12;
            double ov = Math.Max(0, Math.Min(right, cx + 9) - Math.Max(left, cx - 9));
            if (feet < line)
            {
                scan.Behind = true;
                if (overTrunk)
                {
                    scan.BehindTrunk = true;
                    if (ov > scan.BehindOv)
                        scan.BehindOv = ov;
                    list.Add(new Trunk { Cx = cx, BaseY = baseY });
                }
                else
                {
                    scan.BehindTop = true;
                }
            }
            else
            {
                scan.Front = true;
                if (overTrunk)
                {
                    scan.FrontTrunk = true;
                    if (ov > scan.FrontOv)
                        scan.FrontOv = ov;
                }
            }
        }
        return scan;
    }

    static bool ScanGrass(double left, double right, double top, double feet)
    {
        int x0 = (int)Math.Floor((left - 6) / 16) - 1, x1 = (int)Math.Floor((right + 6) / 16) + 1;
        int y0 = (int)Math.Floor((top - 4) / 16) - 1, y1 = (int)Math.Floor((feet + 4) / 16) + 1;
        TileScan.FindTiles(grassMark, x0, y0, x1, y1, found);
        if (found.Count == 0)
            return false;
        int infront = 0, behind = 0;
        for (int k = 0; k < found.Count; k += 2)
        {
            int i = found[k], j = found[k + 1];
            int tl = i * 16 - 2, tr = i * 16 + 18, tt = j * 16 - 4, tb = j * 16 + 16;
            if (right + 4 <= tl || left - 4 >= tr || feet <= tt || top >= tb)
                continue;
            if (feet < j * 16 + GrassLine)
                infront++;
            else
                behind++;
        }
        grassFront = infront;
        grassBehind = behind;
        return infront > 0 && behind == 0;
    }

    static bool ScanFurniture(double left, double right, double top, double feet)
    {
        int x0 = (int)Math.Floor((left - 4) / 16), x1 = (int)Math.Floor((right + 4) / 16);
        int y0 = (int)Math.Floor(top / 16) - 6, y1 = (int)Math.Floor(feet / 16) + 6;
        TileScan.FindTiles(furnMark, x0, y0, x1, y1, found);
        if (found.Count == 0)
            return false;
        TileScan.Runs(found, runs);
        int infront = 0, behind = 0;
        foreach (TileRun run in runs)
        {
            int i = run.I;
            int objTop = run.Top * 16, baseY = (run.Bot + 1) * 16;
            if (right + 2 <= i * 16 || left - 2 >= i * 16 + 16 || feet <= objTop || top >= baseY)
                continue;
            if (feet < baseY - FurnLine)
                infront++;
            else
                behind++;
        }
        furnFront = infront;
        furnBehind = behind;
        return infront > 0 && behind == 0;
    }

    static bool ScanCactus(double left, double right, double top, double feet)
    {
        int x0 = (int)Math.Floor((left - 4) / 16) - 3, x1 = (int)Math.Floor((right + 4) / 16) + 3;
        int y0 = (int)Math.Floor(top / 16) - 2, y1 = (int)Math.Floor(feet / 16) + 16;
        TileScan.FindTiles(TopDownCactus.CactusMark, x0, y0, x1, y1, found);
        if (found.Count == 0)
            return false;
        cactusHas.Clear();
        cactusDone.Clear();
        for (int k = 0; k < found.Count; k += 2)
            cactusHas.Add(TileScan.Key(found[k], found[k + 1]));
        int infront = 0, behind = 0;
        for (int k = 0; k < found.Count; k += 2)
        {
            int i = found[k], j = found[k + 1];
            if (right + 2 <= i * 16 || left - 2 >= i * 16 + 16 || feet <= j * 16 || top >= j * 16 + 16)
                continue;
            if (cactusDone.Contains(TileScan.Key(i, j)))
                continue;
            cactusStack.Clear();
            cactusStack.Push((i, j));
            cactusDone.Add(TileScan.Key(i, j));
            int bot = j;
            while (cactusStack.Count > 0)
            {
                (int ci, int cj) = cactusStack.Pop();
                if (cj > bot)
                    bot = cj;
                PushCactus(ci + 1, cj);
                PushCactus(ci - 1, cj);
                PushCactus(ci, cj + 1);
                PushCactus(ci, cj - 1);
            }
            int baseY = (bot + 1) * 16;
            if (feet < baseY - CactusLine)
                infront++;
            else
                behind++;
        }
        cactusFront = infront;
        cactusBehind = behind;
        return infront > 0 && behind == 0;
    }

    static void PushCactus(int i, int j)
    {
        long key = TileScan.Key(i, j);
        if (cactusHas.Contains(key) && cactusDone.Add(key))
            cactusStack.Push((i, j));
    }

    static Mode Classify(double px, double py, double w, double h, int key, bool isNpc)
    {
        double left = px, right = px + w, top = py, feet = py + h;
        (isNpc ? nswim : swim)[key] = SwimOn() ? WaterKind(left, right, feet) : 0;
        TreeScan t = ScanTrees(left, right, top, feet, key, isNpc ? ntrees : trees);
        if (t.BehindTrunk && t.FrontTrunk && t.FrontOv >= t.BehindOv)
        {
            statFrontWins++;
            return Mode.Front;
        }
        if (t.BehindTrunk)
            return Mode.Behind;
        if (!t.FrontTrunk && GrassOn() && ScanGrass(left, right, top, feet))
        {
            statGrassEarly++;
            return Mode.Behind;
        }
        if (t.BehindTop)
            return Mode.None;
        if (!t.FrontTrunk && FurnOn() && ScanFurniture(left, right, top, feet))
        {
            statFurnEarly++;
            return Mode.Behind;
        }
        if (!t.FrontTrunk && CactusOn() && ScanCactus(left, right, top, feet))
        {
            statCactusEarly++;
            return Mode.Behind;
        }
        return t.Front ? Mode.Front : Mode.None;
    }

    static Mode ModeFor(double px, double py, double w, double h, int key, bool isNpc)
    {
        long now = Environment.TickCount64;
        long[] times = isNpc ? nmodeTime : modeTime;
        Mode[] modes = isNpc ? nmode : mode;
        if (times[key] < 0 || now - times[key] >= RefreshMs)
        {
            times[key] = now;
            Mode m = Mode.None;
            try
            {
                m = Classify(px, py, w, h, key, isNpc);
            }
            catch (Exception e)
            {
                Fail("reading the trees", e);
            }
            if (!isNpc && m != modes[key] && key == Main.myPlayer)
                Dbg("mode " + m + " (tree tiles in the area: " + lastFound + ", trees behind: " + (trees[key]?.Count ?? 0) + ", grass: " + grassFront + "/" + grassBehind + ", order " + order + ")");
            modes[key] = m;
        }
        return modes[key];
    }

    static NpcKind KindOf(NPC n)
    {
        if (n.IsNull || !n.active || n.hide || n.behindTiles)
            return NpcKind.Skip;
        int w = n.width, h = n.height;
        if (w <= 0 || w > NpcMaxSize || h > NpcMaxSize)
            return NpcKind.Skip;
        return n.noGravity ? NpcKind.Air : NpcKind.Ground;
    }

    static Mode NpcMode(NPC n, int i)
    {
        NpcKind kind = KindOf(n);
        if (kind == NpcKind.Skip)
            return Mode.None;
        if (kind == NpcKind.Air)
        {
            nmode[i] = Mode.Front;
            nmodeTime[i] = Environment.TickCount64;
            return Mode.Front;
        }
        return ModeFor(n.position.X, n.position.Y, n.width, n.height, i, true);
    }

    static void BlitTrunks(double px, double py, int width, int height, List<Trunk>? list)
    {
        if (blitErr)
            return;
        if (list == null || list.Count == 0)
            return;
        SpriteBatch sb = Main.spriteBatch;
        bool began = false;
        RenderTarget2D target = Main.instance.tile2Target;
        try
        {
            if (target.IsNull)
                throw new InvalidOperationException("tile target unavailable");
            Vector2 sp = Main.screenPosition, sc = Main.sceneTile2Pos;
            double spx = sp.X, spy = sp.Y, scx = sc.X, scy = sc.Y;
            Texture2D texture = target;
            int tw = texture.Width, th = texture.Height;
            foreach (Trunk t in list)
            {
                double wx0 = Math.Max(px, t.Cx - 12), wx1 = Math.Min(px + width, t.Cx + 12);
                double wy0 = py, wy1 = Math.Min(py + height, t.BaseY + 2);
                if (wx1 <= wx0 || wy1 <= wy0)
                    continue;
                int sx = (int)Math.Floor(wx0 - scx), sy = (int)Math.Floor(wy0 - scy);
                int w = (int)Math.Ceiling(wx1 - wx0), h = (int)Math.Ceiling(wy1 - wy0);
                if (sx < 0 || sy < 0)
                    continue;
                if (sx + w > tw)
                    w = tw - sx;
                if (sy + h > th)
                    h = th - sy;
                if (w <= 0 || h <= 0)
                    continue;
                if (!began)
                    began = BeginBatch(sb);
                sb.Draw(texture, new Vector2((float)Math.Floor(wx0 - spx), (float)Math.Floor(wy0 - spy)), new Rectangle(sx, sy, w, h), Color.White);
                blits++;
                if (blits == 1)
                    Dbg("first trunk copied: source " + sx + "," + sy + " " + w + "x" + h + " target " + tw + "x" + th + " drawToScreen=" + Main.drawToScreen);
            }
        }
        catch (Exception e)
        {
            if (!target.IsNull)
                blitErr = true;
            else if (++blitMiss > 200)
                blitErr = true;
            lastErr = "trunk: " + e;
            if (++blitLogs <= 3)
                TopDownMode.Log("depth: could not cover the player with the trunk: " + e);
        }
        finally
        {
            if (began)
                sb.End();
        }
    }

    static bool BeginBatch(SpriteBatch sb)
    {
        if (sb._beginCalled)
            return false;
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, default, Main.Transform, true);
        return true;
    }

    static SwimGeom SwimGeomOf(double px, double py, double w, double h, int kind)
    {
        double sink = kind == 1 ? 2 : 7;
        double cut = kind == 1 ? 0.78 : 0.5;
        return new SwimGeom { Sink = sink, Px = px, Py = py, W = w, H = h, Water = py + sink + cut * h };
    }

    static void ClipOn(SpriteBatch sb, in SwimGeom g)
    {
        Vector2 sp = Main.screenPosition;
        double spx = sp.X, spy = sp.Y;
        int x0 = (int)Math.Floor(g.Px - 60 - spx), y0 = (int)Math.Floor(g.Py - 100 - spy);
        var r = new Rectangle(x0, y0, (int)Math.Ceiling(g.W + 120), Math.Max(1, (int)Math.Floor(g.Water - spy) - y0));
        SpriteBatchItem.EnableClipping(r, r, sb, true);
    }

    static bool DrawPlayerNow(Player p, int idx)
    {
        ref DrawArgs a = ref args[idx];
        if (!a.Set)
            return false;
        SpriteBatch sb = Main.spriteBatch;
        bool began = false, clipped = false;
        busy = true;
        try
        {
            began = BeginBatch(sb);
            int kind = SwimOn() ? swim[idx] : 0;
            var pos = new Vector2(p.position.X, p.position.Y);
            if (kind > 0)
            {
                SwimGeom g = SwimGeomOf(p.position.X, p.position.Y, p.width, p.height, kind);
                pos = new Vector2((float)g.Px, (float)(g.Py + g.Sink));
                ClipOn(sb, g);
                clipped = true;
            }
            var origin = new Vector2(a.Ox, a.Oy);
            LegacyPlayerRenderer renderer = a.Renderer.IsNull ? Main.PlayerRenderer : a.Renderer;
            GameHandle? held = argOffsets[idx];
            GameArray<Vector2> offsets = held != null ? new GameArray<Vector2>(held.Target) : default;
            renderer.DrawPlayer(Main.Camera, p, pos, a.Rot, origin, 0, a.Scale, offsets);
            return true;
        }
        catch (Exception e)
        {
            Fail("redrawing the player", e);
            return false;
        }
        finally
        {
            if (clipped)
                SpriteBatchItem.DisabledClipping();
            busy = false;
            if (began)
                sb.End();
        }
    }

    static bool DrawNpcNow(NPC n, int i)
    {
        if (!npcs)
            return false;
        SpriteBatch sb = Main.spriteBatch;
        bool began = false, clipped = false;
        float? lifted = null;
        busy = true;
        try
        {
            began = BeginBatch(sb);
            int kind = SwimOn() ? nswim[i] : 0;
            if (kind > 0)
            {
                SwimGeom g = SwimGeomOf(n.position.X, n.position.Y, n.width, n.height, kind);
                lifted = n.gfxOffY;
                n.gfxOffY = (float)(lifted.Value + g.Sink);
                ClipOn(sb, g);
                clipped = true;
            }
            Main.instance.DrawNPCDirect(sb, n, false, Main.screenPosition);
            return true;
        }
        catch (Exception e)
        {
            npcErrors++;
            lastErr = "NPC (Main.DrawNPCDirect): " + e;
            if (npcErrors <= 4)
                TopDownMode.Log("NPC depth (Main.DrawNPCDirect): " + e);
            npcs = false;
            TopDownMode.Log("NPC depth off: drawing the NPC did not work.");
            return false;
        }
        finally
        {
            if (clipped)
                SpriteBatchItem.DisabledClipping();
            if (lifted != null)
                n.gfxOffY = lifted.Value;
            busy = false;
            if (began)
                sb.End();
        }
    }

    static bool OnNativePlayer(LegacyPlayerRenderer renderer, Player p, float rotation, Vector2 origin, float scale, GameArray<Vector2> offsets)
    {
        int idx = p.whoAmI;
        if (idx == Main.myPlayer && firstP < 0)
            firstP = seqCount++;
        statNative++;
        ref DrawArgs a = ref args[idx];
        a.Set = true;
        a.Renderer = renderer;
        a.Rot = rotation;
        a.Scale = scale;
        a.Ox = origin.X;
        a.Oy = origin.Y;
        KeepOffsets(idx, offsets);
        if (!Active())
            return false;
        Mode m = ModeFor(p.position.X, p.position.Y, p.width, p.height, idx, false);
        if (idx == Main.myPlayer)
        {
            if (m == Mode.Behind)
                statBehind++;
            else if (m == Mode.Front)
                statFront++;
        }
        if (inPass && early[idx] == passId)
        {
            statSkipped++;
            return true;
        }
        nat[idx] = passId;
        if (SwimOn() && swim[idx] > 0 && DrawPlayerNow(p, idx))
        {
            statSwimDraw++;
            return true;
        }
        return false;
    }

    // The early draw uses the arguments of the previous frame, so the array must outlive this call.
    static void KeepOffsets(int idx, GameArray<Vector2> offsets)
    {
        GameHandle? held = argOffsets[idx];
        if (offsets.IsNull)
        {
            held?.Dispose();
            argOffsets[idx] = null;
            return;
        }
        if (held != null && held.Target == offsets.Pointer)
            return;
        held?.Dispose();
        argOffsets[idx] = new GameHandle(offsets.Pointer);
    }

    static void NoteSwim(double px, double py, double w, double h, int kind)
    {
        if (kind <= 0 || !SwimOn() || Swimmers.Count >= 4)
            return;
        SwimGeom g = SwimGeomOf(px, py, w, h, kind);
        Swimmers.Add(new Swimmer { X = g.Px + g.W / 2, Y = g.Water, Hw = Math.Max(g.W / 2 + 6, 14) });
    }

    static bool OnNativeNpc(NPC npc)
    {
        int idx = npc.whoAmI;
        statNpcNative++;
        if (!Active() || !NpcOn())
            return false;
        if (KindOf(npc) == NpcKind.Skip)
            return false;
        NpcMode(npc, idx);
        if (inPass && nearly[idx] == passId)
        {
            statNpcSkipped++;
            return true;
        }
        nnat[idx] = passId;
        if (SwimOn() && nswim[idx] > 0 && DrawNpcNow(npc, idx))
        {
            statSwimDraw++;
            return true;
        }
        return false;
    }

    static void StartNpcs()
    {
        if (!NpcOn())
            return;
        Vector2 sp = Main.screenPosition;
        double sx = (double)sp.X - NpcCull, sy = (double)sp.Y - NpcCull;
        double ex = sx + Main.screenWidth + NpcCull * 2, ey = sy + Main.screenHeight + NpcCull * 2;
        int max = Math.Min(Main.maxNPCs, 200);
        for (int i = 0; i < max; i++)
        {
            NPC n = Main.npc[i];
            if (n.IsNull || !n.active)
                continue;
            if (KindOf(n) != NpcKind.Ground)
                continue;
            double x = n.position.X, y = n.position.Y;
            if (x < sx || x > ex || y < sy || y > ey)
                continue;
            Mode nm = NpcMode(n, i);
            NoteSwim(x, y, n.width, n.height, nswim[i]);
            if (nm != Mode.Behind)
                continue;
            if (DrawNpcNow(n, i))
            {
                nearly[i] = passId;
                statNpcEarly++;
                if (!Main.drawToScreen)
                    BlitTrunks(n.position.X, n.position.Y, n.width, n.height, ntrees[i]);
            }
        }
    }

    static void StartPass()
    {
        if (inPass)
            return;
        inPass = true;
        passId++;
        Swimmers.Clear();
        if (!Active())
            return;
        int n = Math.Min(Main.maxPlayers, 255);
        for (int idx = 0; idx < n; idx++)
        {
            Player p = Main.player[idx];
            if (p.IsNull || !p.active || p.dead || !args[idx].Set)
                continue;
            Mode pm = ModeFor(p.position.X, p.position.Y, p.width, p.height, idx, false);
            NoteSwim(p.position.X, p.position.Y, p.width, p.height, swim[idx]);
            if (pm != Mode.Behind)
                continue;
            if (DrawPlayerNow(p, idx))
            {
                early[idx] = passId;
                statEarly++;
                if (!Main.drawToScreen)
                    BlitTrunks(p.position.X, p.position.Y, p.width, p.height, trees[idx]);
            }
        }
        try
        {
            StartNpcs();
        }
        catch (Exception e)
        {
            Fail("NPCs before the tiles", e);
        }
    }

    static void AfterTrees()
    {
        if (redrawn == passId)
            return;
        redrawn = passId;
        for (int idx = 0; idx < MaxEntity; idx++)
        {
            if (mode[idx] != Mode.Front)
                continue;
            Player p = Main.player[idx];
            if (p.IsNull || !p.active || p.dead || !args[idx].Set)
                continue;
            if (nat[idx] != passId)
                continue;
            if (DrawPlayerNow(p, idx))
                statRedraw++;
        }
        if (!NpcOn())
            return;
        long now = Environment.TickCount64;
        for (int idx = 0; idx < MaxNpc; idx++)
        {
            if (nmode[idx] != Mode.Front)
                continue;
            NPC n = Main.npc[idx];
            if (n.IsNull || !n.active || n.alpha > 0)
                continue;
            if (now - nmodeTime[idx] > 250)
                continue;
            if (nnat[idx] != passId)
                continue;
            if (KindOf(n) == NpcKind.Skip)
                continue;
            if (DrawNpcNow(n, idx))
                statNpcRedraw++;
        }
    }

    static void LearnOrder()
    {
        int t = firstT, pl = firstP;
        firstT = -1;
        firstP = -1;
        seqCount = 0;
        if (t < 0 || pl < 0)
            return;
        string o = t < pl ? "TP" : "PT";
        if (o != order)
        {
            order = o;
            TopDownMode.Log("native draw order: " + (o == "PT" ? "player, then tree tops" : "tree tops, then player"));
        }
    }

    static void InstallNpcHook()
    {
        if (npcHooked)
            return;
        On_Main.DrawNPCDirect_SpriteBatch_NPC_bool_Vector2 += (orig, self, batch, npc, behind, screen) =>
        {
            if (SkipNativeNpc(npc, behind))
                return;
            orig(self, batch, npc, behind, screen);
        };
        On_Main.DrawNPCDirect_SpriteBatch_NPC_bool_Vector2_LightMap_refRectangle += (orig, self, batch, npc, behind, screen, map, ref region) =>
        {
            if (SkipNativeNpc(npc, behind))
                return;
            orig(self, batch, npc, behind, screen, map, ref region);
        };
        npcHooked = true;
    }

    static bool SkipNativeNpc(NPC npc, bool behind)
    {
        if (busy || !enabled || !npcs || behind)
            return false;
        try
        {
            return OnNativeNpc(npc);
        }
        catch (Exception e)
        {
            Fail("drawing the NPC", e);
            return false;
        }
    }

    public static void InstallTreeDepth()
    {
        enabled = true;
        if (grassTypes.Count == 0)
            grassTypes.Add(Grass);
        foreach (int t in grassTypes)
            grassMark.Set(t);
        if (furnTypes.Count == 0)
            furnTypes.AddRange(FurnDefault);
        foreach (int t in furnTypes)
            furnMark.Set(t);
        TopDownCactus.CactusMark.Set(TopDownCactus.Cactus);
        InstallNpcHook();
        if (hooked)
            return;
        On_LegacyPlayerRenderer.DrawPlayer += (orig, renderer, camera, player, position, rotation, origin, shadow, scale, offsets) =>
        {
            if (busy || !enabled || shadow != 0)
            {
                orig(renderer, camera, player, position, rotation, origin, shadow, scale, offsets);
                return;
            }
            bool skip = false;
            try
            {
                skip = OnNativePlayer(renderer, player, rotation, origin, scale, offsets);
            }
            catch (Exception e)
            {
                Fail("drawing the player", e);
            }
            if (skip)
                return;
            orig(renderer, camera, player, position, rotation, origin, shadow, scale, offsets);
        };
        On_TileDrawing.PostDrawTiles += (orig, self, solidLayer, forRenderTargets, intoRenderTargets) =>
        {
            if (solidLayer || intoRenderTargets || busy || !enabled || Main.gameMenu)
            {
                orig(self, solidLayer, forRenderTargets, intoRenderTargets);
                return;
            }
            statPasses++;
            if (firstT < 0)
                firstT = seqCount++;
            try
            {
                StartPass();
            }
            catch (Exception e)
            {
                Fail("starting the pass", e);
            }
            orig(self, solidLayer, forRenderTargets, intoRenderTargets);
            try
            {
                if (Active())
                    AfterTrees();
            }
            catch (Exception e)
            {
                Fail("after the tree tops", e);
            }
        };
        On_Main.DrawTiles += (orig, self, solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride) =>
        {
            BeforeTrunks(solidLayer, intoRenderTargets);
            orig(self, solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride);
        };
        On_Main.DoDraw_Tiles_NonSolid += (orig, self) =>
        {
            BeforeTrunks(false, false);
            orig(self);
        };
        On_TileDrawing.PreDrawTiles += (orig, self, solidLayer, forRenderTargets, intoRenderTargets) =>
        {
            BeforeTrunks(solidLayer, intoRenderTargets);
            orig(self, solidLayer, forRenderTargets, intoRenderTargets);
        };
        On_Main.DrawInterface += (orig, self, gameTime) =>
        {
            try
            {
                LearnOrder();
            }
            catch (Exception e)
            {
                Fail("order", e);
            }
            inPass = false;
            orig(self, gameTime);
        };
        hooked = true;
        TopDownMode.Log("tree depth: hooks installed");
    }

    static void BeforeTrunks(bool solid, bool renderTarget)
    {
        if (!enabled || busy || Main.gameMenu || solid || renderTarget)
            return;
        try
        {
            StartPass();
        }
        catch (Exception e)
        {
            Fail("before the trunks", e);
        }
    }

    public static void UninstallTreeDepth() => enabled = false;

    public static string Status() =>
        "depth: order=" + order + " active=" + enabled + " errors=" + errors + " passes=" + statPasses + " player(native)=" + statNative + " behind=" + statBehind + " front=" + statFront
        + " before the trunks=" + statEarly + " redraws=" + statRedraw + " skipped=" + statSkipped + " trunks without a base=" + noBaseTrees + " drawToScreen=" + Main.drawToScreen + " tiles in the area=" + lastFound
        + (lastErr != "" ? " last error: " + lastErr : "")
        + "\ndepth (NPCs/grass): npcs=" + npcs + " grass=" + grass + " npc native=" + statNpcNative + " npc before=" + statNpcEarly + " npc redraw=" + statNpcRedraw + " npc skipped=" + statNpcSkipped
        + " grass before=" + statGrassEarly + " grass types=" + string.Join(",", grassTypes) + " swimming=" + statSwimDraw + " (player " + swim[Main.myPlayer] + ", swimmers " + Swimmers.Count + ")"
        + " npc errors=" + npcErrors + " last grass=" + grassFront + " in front/" + grassBehind + " behind | furniture=" + furniture + " before=" + statFurnEarly + " types=" + string.Join(",", furnTypes) + " last=" + furnFront + "/" + furnBehind
        + " | cactus=" + cactus + " before=" + statCactusEarly + " last=" + cactusFront + "/" + cactusBehind + " | front wins=" + statFrontWins;

    public static string Nearby(int radius)
    {
        Player p = Main.player[Main.myPlayer];
        int R = Math.Min(Math.Max(radius > 0 ? radius : 6, 1), 15);
        int tx = (int)Math.Floor(p.position.X / 16.0), ty = (int)Math.Floor(p.position.Y / 16.0);
        var count = new SortedDictionary<int, int>();
        for (int i = tx - R; i <= tx + R; i++)
        {
            for (int j = ty - R; j <= ty + R; j++)
            {
                int t = TileScan.TypeAt(i, j);
                if (t >= 0)
                {
                    count.TryGetValue(t, out int c);
                    count[t] = c + 1;
                }
            }
        }
        var parts = new StringBuilder();
        foreach (KeyValuePair<int, int> entry in count)
            parts.Append(entry.Key).Append('x').Append(entry.Value).Append(furnTypes.Contains(entry.Key) ? "(furniture) " : " ");
        return "tiles around (type x count): " + (parts.Length > 0 ? parts.ToString() : "(none)") + " -> to include one: /topdown addfurniture <type>";
    }

    public static bool AddFurnitureTile(int type)
    {
        if (type < 0)
            return false;
        if (!furnTypes.Contains(type))
            furnTypes.Add(type);
        furnMark.Set(type);
        return true;
    }

    public static bool AddGrassTile(int type)
    {
        if (type < 0)
            return false;
        if (!grassTypes.Contains(type))
            grassTypes.Add(type);
        grassMark.Set(type);
        return true;
    }

    public static string Ghosts(int radius)
    {
        Player p = Main.player[Main.myPlayer];
        int R = Math.Min(Math.Max(radius > 0 ? radius : 30, 5), 30);
        int tx = (int)Math.Floor(p.position.X / 16.0), ty = (int)Math.Floor(p.position.Y / 16.0);
        var list = new List<int>();
        TileScan.FindTiles(TopDownTreeBase.TreeMark, tx - R, ty - R, tx + R, ty + R, list);
        var low = new SortedDictionary<int, int>();
        var high = new Dictionary<int, int>();
        for (int k = 0; k < list.Count; k += 2)
        {
            int i = list[k], j = list[k + 1];
            if (!low.TryGetValue(i, out int lo) || j > lo)
                low[i] = j;
            if (!high.TryGetValue(i, out int hi) || j < hi)
                high[i] = j;
        }
        int withBase = 0;
        var ghosts = new List<string>();
        foreach (KeyValuePair<int, int> entry in low)
        {
            int i = entry.Key, j = entry.Value;
            if (j - high[i] < 2)
                continue;
            int below = TileScan.TypeAt(i, j + 1);
            if (below >= 0 && below != Tree)
                withBase++;
            else if (below != Tree)
                ghosts.Add(i + "," + j);
        }
        return "tall trunks: " + withBase + " with a base, " + ghosts.Count + " without" + (ghosts.Count > 0 ? " (" + string.Join(" ", ghosts.GetRange(0, Math.Min(12, ghosts.Count))) + ")" : "");
    }
}
