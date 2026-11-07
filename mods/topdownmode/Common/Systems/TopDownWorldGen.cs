using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using topdownmode.Common.Configs;
using topdownmode.Common.Utils;

namespace topdownmode.Common.Systems;

// The island generator. All noise is computed in double and stored in float arrays, as the
// JavaScript version did with Float32Array, so a seed gives the same terrain.
public static class TopDownWorldGen
{
    const int StrideShift = 3;
    const int Stride = 1 << StrideShift;
    const int Edge = 6;
    const int ClearRadius = 14;

    const int DeepZone = 0, ShallowZone = 1, BeachZone = 2, LowZone = 3, HighZone = 4, MountZone = 5;
    const int GrassBiome = 0, DesertBiome = 1, SnowBiome = 2, JungleBiome = 3, EvilBiome = 4;
    const int Nb = 5;

    const int NLand = 6;
    const int KEvil = 6, KShallow = 7, KDeep = 8;

    const double ThreshDeep = 0.34, ThreshShallow = 0.41, ThreshBeach = 0.445, ThreshHigh = 9, ThreshMount = 10;
    const double CenterSafe = 0.20;
    const double ForestW = 0.70, CorridorW = 0.26;
    const double EcoBand = 0.022;
    const double RiverRate = 0.020;
    const double LakeDepth = 0.010;

    static readonly string[] Land = { "Grass", "Sand", "Snow", "Jungle", "Rock", "Evil" };
    static readonly string[] Flavors = { "Corrupt", "Crimson" };
    static readonly int[] Prio = { 0, 4, 1, 5, 3, 2, 6 };
    static readonly string[] StoneGround = { "", "Grass", "Sand", "Snow", "Jungle", "", "", "Shallow" };
    static readonly int[] StoneKinds = { 1, 2, 3, 4, 7 };
    static readonly double[] Rain = { 1.0, 0.30, 0.85, 1.5, 0.9 };
    static readonly int[] Dx4 = { 0, 1, 0, -1 }, Dy4 = { -1, 0, 1, 0 };
    static readonly int[] Dxc = { 1, 1, -1, -1 }, Dyc = { -1, 1, 1, -1 };
    static readonly int[] PlantStyles = { 0, 1, 2, 3, 4, 5 };

    static string evilFlavor = "Corrupt";
    static bool bmFlag;

    public static void SetEvilFlavor(string flavor)
    {
        if (flavor == "Corrupt" || flavor == "Crimson")
            evilFlavor = flavor;
    }

    public static string GetEvilFlavor() => evilFlavor;

    public struct Point2
    {
        public int X, Y;
    }

    public sealed class GenOpts
    {
        public int? Seed;
        public bool SoftEdges = true, Decor = true, Rivers = true, Lakes = true, Cactus = true, Fresh;
        public double TreeDensity = 1.6, DecorDensity = 1, EvilSize = 1, RiverDensity = 1, RiverWidth = 1, CactusDensity = 1;
        public EvilBiomeMode Evil = EvilBiomeMode.Auto;
        public WorldShapeMode? Shape;
        public TreesMode Trees = TreesMode.Terraria;
    }

    public sealed class GenResult
    {
        public Point2 Spawn;
        public Palette Palette = null!;
        public string? Evil;
    }

    public sealed class Palette
    {
        public int TileStone = TileID.Stone, TileSand = TileID.Sand, TileSandstone = TileID.Sandstone, TileSnow = TileID.SnowBlock, TileGrass = TileID.Grass,
            TileJungleGrass = TileID.JungleGrass, TileCorruptGrass = TileID.CorruptGrass, TileCrimsonGrass = TileID.CrimsonGrass, TileEbonstone = TileID.Ebonstone,
            TileCrimstone = TileID.Crimstone, TileIce = TileID.IceBlock, TileLeaf = TileID.LeafBlock, TileWood = TileID.LivingWood, TileGlass = TileID.Glass;
        public int WallGrass, WallDesert, WallSnow, WallJungle, WallSand, WallRock, WallEvil, WallShallow, WallDeep;
        public Mix Mix = null!;
        public Stones Stone = null!;

        public IEnumerable<int> Walls() => new[] { WallGrass, WallDesert, WallSnow, WallJungle, WallSand, WallRock, WallEvil, WallShallow, WallDeep };
    }

    public sealed class Mix
    {
        public readonly Dictionary<int, int> ByKind = new();
        public readonly List<int> Ids = new();
        public readonly Dictionary<string, int> EvilIds = new();
        public int Ok;
    }

    public sealed class Stones
    {
        public readonly Dictionary<int, List<int>> ByKind = new();
        public readonly List<int> Ids = new();
        public readonly HashSet<int> Set = new();
        public int Ok;
    }

    sealed class Tables
    {
        public readonly int[] Floor = new int[6 * Nb], Block = new int[6 * Nb], Kind = new int[6 * Nb];
    }

    sealed class Layout
    {
        public double Cx, Cy, Sc = 1, Phi0;
        public int Seed;
        public bool EvilOn;
        public double[] Ax = null!, Ay = null!, Aw = null!;
        public byte[] Ab = null!;
    }

    sealed class Fields
    {
        public int Gw, Gh;
        public float[] HF = null!, WxF = null!, WyF = null!;
        public float[]? Prox;
        public byte[] BF = null!;
        public Layout L = null!;
    }

    sealed class Mask
    {
        public int Cw;
        public byte[]?[] Chunks = null!;
    }

    sealed class WaterResult
    {
        public Mask M = null!;
        public float[]? Prox;
        public int Rivers, RiverNodes, Lakes, Oases, Mouths, LandNodes;
        public double T;
    }

    sealed class Stats
    {
        public long Writes, Obstacles, Trees, Glass, Cacti, Edges, Rocks, Plants, WaterRocks;
    }

    sealed class GenState
    {
        public bool Failed;
        public long T0;
        public GenOpts Opts = null!;
        public int Seed, W, H, Wr, X0, SpawnLX;
        public bool EvilOn, Fresh;
        public Palette P = null!;
        public Tables Tab = null!;
        public Fields? F;
        public byte[]? Kinds;
        public Stats Stats = null!;
        public WaterResult? Water;
        public Point2 Spawn;
        public Spacing? TreeIdx;
    }

    public sealed class Stage
    {
        public string Name = "";
        public double Weight;
        public Action<GenerationProgress> Run = null!;
    }

    sealed class Spacing
    {
        public int Bw, Bh;
        public List<int>?[] Cells = null!;
    }

    struct TreeCandidate
    {
        public int Px, Py, Biome;
    }

    public sealed class TreeResult
    {
        public int Ok, Tried;
        public string Err = "";
    }

    public sealed class BlendResult
    {
        public int Resets, Written;
    }

    public sealed class DecorResult
    {
        public int Rocks, Plants, PlantFail, Direct, Cleared, WaterRocks;
    }

    public sealed class DeepResult
    {
        public int Deep, Hidden, Removed, Kept, Failed;
    }

    sealed class DecorOpts
    {
        public int? Seed;
        public double Density = 1;
        public int X0, X1;
        public Point2? Spawn;
        public Mask? Water;
        public bool Clear, Rocks = true, Plants = true, WaterRocks = true;
    }

    static void Log(string message) => TopDownMode.Log(message);

    static void SquareFrame(int i, int j)
    {
        try
        {
            WorldGen.SquareTileFrame(i, j, true);
        }
        catch (Exception)
        {
        }
    }

    // JavaScript's Math.round, which rounds .5 up, unlike Math.Round.
    static int Round(double v) => (int)Math.Floor(v + 0.5);

    static int WallByName(string name, int fallback)
    {
        if (ModContent.TryFind<ModWall>("topdownmode", name, out ModWall wall) && wall.Type > 0)
            return wall.Type;
        return fallback;
    }

    public static Palette BuildPalette()
    {
        var palette = new Palette
        {
            WallGrass = WallByName("TopDownGrass", WallID.GrassUnsafe),
            WallDesert = WallByName("TopDownSand", WallID.HardenedSand),
            WallSnow = WallByName("TopDownSnow", WallID.SnowWallUnsafe),
            WallJungle = WallByName("TopDownJungle", WallID.JungleUnsafe),
            WallSand = WallByName("TopDownSand", WallID.HardenedSand),
            WallRock = WallByName("TopDownRock", WallID.Stone),
            WallEvil = WallByName(evilFlavor == "Crimson" ? "TopDownCrimson" : "TopDownCorrupt", WallID.GrassUnsafe),
            WallShallow = WallByName("TopDownShallow", WallID.IceUnsafe),
            WallDeep = WallByName("TopDownDeep", WallID.BlueDungeonUnsafe),
        };
        palette.Mix = BuildMix();
        palette.Stone = BuildStones();
        return palette;
    }

    static string CodeName(int c) => c <= 15 ? "M" + c : "C" + (c - 16);

    static bool PairOk(int a, int b)
    {
        if (a == KEvil || !(Prio[b] > Prio[a]) || (a == 4 && b == 3))
            return false;
        return b != KEvil || a == 1 || a == 2 || a == 5;
    }

    static Mix BuildMix()
    {
        var mix = new Mix();
        foreach (string fl in Flavors)
        {
            for (int a = 1; a <= NLand; a++)
            {
                for (int b = 1; b <= NLand; b++)
                {
                    if (a == b || !PairOk(a, b))
                        continue;
                    if (b != KEvil && fl != Flavors[0])
                        continue;
                    for (int c = 1; c <= 19; c++)
                    {
                        string nameB = b == KEvil ? fl : Land[b - 1];
                        int id = WallByName("TopDownEdge" + Land[a - 1] + nameB + CodeName(c), 0);
                        if (id > 0)
                        {
                            mix.Ids.Add(id);
                            if (b != KEvil || fl == evilFlavor)
                            {
                                mix.ByKind[a * 10000 + b * 100 + c] = id;
                                mix.Ok++;
                            }
                        }
                    }
                }
            }
        }
        foreach (string fl in Flavors)
            mix.EvilIds[fl] = WallByName(fl == "Crimson" ? "TopDownCrimson" : "TopDownCorrupt", 0);
        return mix;
    }

    static Stones BuildStones()
    {
        var st = new Stones();
        foreach (int k in StoneKinds)
        {
            var list = new List<int>();
            for (int n = 1; n <= 3; n++)
            {
                int id = WallByName("TopDown" + StoneGround[k] + "Stone" + n, 0);
                if (id > 0)
                {
                    list.Add(id);
                    st.Ids.Add(id);
                    st.Set.Add(id);
                    st.Ok++;
                }
            }
            st.ByKind[k] = list;
        }
        return st;
    }

    static Dictionary<int, int> KindMap(Palette P)
    {
        var m = new Dictionary<int, int>
        {
            [P.WallShallow] = KShallow,
            [P.WallDeep] = KDeep,
        };
        m[P.WallGrass] = 1;
        m[P.WallDesert] = 2;
        m[P.WallSand] = 2;
        m[P.WallSnow] = 3;
        m[P.WallJungle] = 4;
        m[P.WallRock] = 5;
        m[P.WallEvil] = KEvil;
        foreach (string fl in Flavors)
        {
            if (P.Mix.EvilIds.TryGetValue(fl, out int id) && id > 0)
                m[id] = KEvil;
        }
        foreach (KeyValuePair<int, int> entry in P.Mix.ByKind)
            m[entry.Value] = entry.Key / 10000;
        foreach (KeyValuePair<int, List<int>> entry in P.Stone.ByKind)
        {
            foreach (int id in entry.Value)
                m[id] = entry.Key;
        }
        return m;
    }

    static int ApplySoftEdges(byte[] kinds, int W, int H, Palette P, int xOffset)
    {
        Mix mix = P.Mix;
        if (mix.Ok == 0)
        {
            Log("edges: TopDownEdge* walls unavailable, nothing to do.");
            return 0;
        }
        byte[] cand = new byte[W * H];
        var list = new List<int>();
        void Add(int i)
        {
            if (i >= 0 && i < cand.Length && cand[i] == 0)
            {
                cand[i] = 1;
                list.Add(i);
            }
        }
        for (int y = 1; y < H - 1; y++)
        {
            int i = y * W + 1;
            for (int x = 1; x < W - 1; x++, i++)
            {
                int a = kinds[i];
                if (a == 0 || a > NLand)
                    continue;
                int r = kinds[i + 1], d = kinds[i + W];
                if (r != a && r > 0 && r <= NLand)
                {
                    Add(i);
                    Add(i + 1);
                }
                if (d != a && d > 0 && d <= NLand)
                {
                    Add(i);
                    Add(i + W);
                }
            }
        }
        int n1 = list.Count;
        for (int k = 0; k < n1; k++)
        {
            int i = list[k];
            for (int q = 0; q < 4; q++)
            {
                Add(i + Dx4[q] + Dy4[q] * W);
                Add(i + Dxc[q] + Dyc[q] * W);
            }
        }
        int n = 0;
        for (int k = 0; k < list.Count; k++)
        {
            int i = list[k];
            int a = kinds[i];
            if (a == 0 || a > NLand)
                continue;
            int x = i % W, y = i / W;
            if (x < 1 || y < 1 || x >= W - 1 || y >= H - 1)
                continue;
            int best = 0;
            for (int q = 0; q < 4; q++)
            {
                int o1 = kinds[i + Dx4[q] + Dy4[q] * W], o2 = kinds[i + Dxc[q] + Dyc[q] * W];
                if (o1 != a && o1 > 0 && o1 <= NLand && PairOk(a, o1) && Prio[o1] > Prio[best])
                    best = o1;
                if (o2 != a && o2 > 0 && o2 <= NLand && PairOk(a, o2) && Prio[o2] > Prio[best])
                    best = o2;
            }
            if (best == 0)
                continue;
            int code = 0;
            for (int q = 0; q < 4; q++)
            {
                if (kinds[i + Dx4[q] + Dy4[q] * W] == best)
                    code |= 1 << q;
            }
            if (code == 0)
            {
                for (int q = 0; q < 4; q++)
                {
                    if (kinds[i + Dxc[q] + Dyc[q] * W] == best)
                    {
                        code = 16 + q;
                        break;
                    }
                }
            }
            if (code == 0)
                continue;
            if (!mix.ByKind.TryGetValue(a * 10000 + best * 100 + code, out int id))
                continue;
            Tile t = Main.tile[x + xOffset, y];
            t.wall = (ushort)id;
            n++;
        }
        Log("edges between floors: " + n + " tiles (" + list.Count + " candidates)");
        return n;
    }

    public static BlendResult BlendOpenWorld(bool apply)
    {
        Palette P = BuildPalette();
        int W = Main.maxTilesX, H = Main.maxTilesY;
        Dictionary<int, int> km = KindMap(P);
        int[] baseOf = { 0, P.WallGrass, P.WallSand, P.WallSnow, P.WallJungle, P.WallRock, P.WallEvil };
        var mixIds = new HashSet<int>(P.Mix.Ids);
        byte[] kinds = new byte[W * H];
        var result = new BlendResult();
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                Tile t = Main.tile[x, y];
                int w = t.wall;
                if (w == 0)
                    continue;
                if (!km.TryGetValue(w, out int k) || k == 0)
                    continue;
                kinds[y * W + x] = (byte)k;
                if (mixIds.Contains(w))
                {
                    t.wall = (ushort)baseOf[k];
                    result.Resets++;
                }
            }
        }
        result.Written = apply ? ApplySoftEdges(kinds, W, H, P, 0) : 0;
        ApplyFloorLight(P, apply);
        return result;
    }

    static Tables BuildTables(Palette P)
    {
        var tab = new Tables();
        int evilStone = evilFlavor == "Crimson" ? P.TileCrimstone : P.TileEbonstone;
        int[] land = { P.WallGrass, P.WallDesert, P.WallSnow, P.WallJungle, P.WallEvil };
        int[] mountBlock = { P.TileStone, P.TileSandstone, P.TileSnow, P.TileStone, evilStone };
        int[] lowKind = { 1, 2, 3, 4, KEvil };
        for (int b = 0; b < Nb; b++)
        {
            tab.Floor[DeepZone * Nb + b] = P.WallDeep;
            tab.Block[DeepZone * Nb + b] = P.TileGlass;
            tab.Floor[ShallowZone * Nb + b] = P.WallShallow;
            tab.Block[ShallowZone * Nb + b] = 0;
            tab.Floor[BeachZone * Nb + b] = P.WallSand;
            tab.Block[BeachZone * Nb + b] = 0;
            tab.Floor[LowZone * Nb + b] = land[b];
            tab.Block[LowZone * Nb + b] = 0;
            tab.Floor[HighZone * Nb + b] = P.WallRock;
            tab.Block[HighZone * Nb + b] = 0;
            tab.Floor[MountZone * Nb + b] = P.WallRock;
            tab.Block[MountZone * Nb + b] = mountBlock[b];
            tab.Kind[DeepZone * Nb + b] = KDeep;
            tab.Kind[ShallowZone * Nb + b] = KShallow;
            tab.Kind[BeachZone * Nb + b] = 2;
            tab.Kind[LowZone * Nb + b] = lowKind[b];
            tab.Kind[HighZone * Nb + b] = 5;
            tab.Kind[MountZone * Nb + b] = 5;
        }
        return tab;
    }

    // The arithmetic wraps as JavaScript's Math.imul and >>> do.
    static double Hash2(int ix, int iy, int seed)
    {
        unchecked
        {
            int h = ix * 374761393 + iy * 668265263 + seed * 1442695041;
            h = (h ^ (int)((uint)h >> 13)) * 1274126177;
            h ^= (int)((uint)h >> 16);
            return (uint)h / 4294967296.0;
        }
    }

    static double VNoise(double x, double y, int seed)
    {
        double x0 = Math.Floor(x), y0 = Math.Floor(y);
        double fx = x - x0, fy = y - y0;
        double u = fx * fx * (3 - 2 * fx), v = fy * fy * (3 - 2 * fy);
        int ix = (int)(long)x0, iy = (int)(long)y0;
        double a = Hash2(ix, iy, seed), b = Hash2(ix + 1, iy, seed);
        double c = Hash2(ix, iy + 1, seed), d = Hash2(ix + 1, iy + 1, seed);
        return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
    }

    static double Fbm(double x, double y, int seed, int octaves)
    {
        double amp = 0.5, f = 1, sum = 0, norm = 0;
        for (int o = 0; o < octaves; o++)
        {
            sum += amp * VNoise(x * f + o * 17.3, y * f + o * 9.1, seed + o * 131);
            norm += amp;
            amp *= 0.5;
            f *= 2;
        }
        return sum / norm;
    }

    static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;

    static double Smooth(double a, double b, double v)
    {
        double t = Clamp01((v - a) / (b - a));
        return t * t * (3 - 2 * t);
    }

    static int ZoneOf(double h)
    {
        if (h < ThreshDeep)
            return DeepZone;
        if (h < ThreshShallow)
            return ShallowZone;
        if (h < ThreshBeach)
            return BeachZone;
        if (h < ThreshHigh)
            return LowZone;
        if (h < ThreshMount)
            return HighZone;
        return MountZone;
    }

    static Layout MakeLayout(int Wr, int H, int seed, bool evilOn, double evilSize)
    {
        double Rnd(int k) => Hash2(k, 7, seed + 777);
        double phi0 = Rnd(1) * Math.PI * 2;
        int side = Rnd(2) < 0.5 ? 1 : 3;
        int[] biomeOf = { SnowBiome, 0, JungleBiome, 0 };
        biomeOf[side] = DesertBiome;
        biomeOf[4 - side] = evilOn ? EvilBiome : GrassBiome;
        double es = Math.Max(0.3, Math.Min(4, evilSize != 0 ? evilSize : 1));
        var ax = new List<double> { 0 };
        var ay = new List<double> { 0 };
        var aw = new List<double> { ForestW };
        var ab = new List<byte> { GrassBiome };
        for (int k = 0; k < 4; k++)
        {
            double ang = phi0 + k * Math.PI / 2 + (Rnd(10 + k) - 0.5) * 0.30;
            double dist = 0.84 + (Rnd(20 + k) - 0.5) * 0.08;
            int b = biomeOf[k];
            double w = b == EvilBiome ? 0.42 + 0.25 * es : b == GrassBiome ? 0.9 : b == SnowBiome ? 1.08 : b == DesertBiome ? 0.92 : 1.0;
            ax.Add(Math.Cos(ang) * dist);
            ay.Add(Math.Sin(ang) * dist);
            aw.Add(w);
            ab.Add((byte)b);
        }
        for (int k = 0; k < 4; k++)
        {
            double ang = phi0 + k * Math.PI / 2 + Math.PI / 4 + (Rnd(40 + k) - 0.5) * 0.30;
            ax.Add(Math.Cos(ang) * 0.95);
            ay.Add(Math.Sin(ang) * 0.95);
            aw.Add(CorridorW);
            ab.Add(GrassBiome);
        }
        return new Layout
        {
            Cx = Wr / 2.0,
            Cy = H / 2.0,
            Seed = seed,
            Sc = 1,
            EvilOn = evilOn,
            Phi0 = phi0,
            Ax = ax.ToArray(),
            Ay = ay.ToArray(),
            Aw = aw.ToArray(),
            Ab = ab.ToArray(),
        };
    }

    static double EvilRag(Layout L, double x, double y)
    {
        double k = 1 / (70 * L.Sc);
        return (Fbm(x * k, y * k, L.Seed + 812, 3) - 0.5) * 0.55;
    }

    static int BiomeExact(Layout L, double lx, double y, double wxn, double wyn, bool fine)
    {
        double u0 = (lx - L.Cx) / L.Cx, v0 = (y - L.Cy) / L.Cy;
        bmFlag = false;
        if (u0 * u0 + v0 * v0 < CenterSafe * CenterSafe)
            return GrassBiome;
        double u = u0 + wxn, v = v0 + wyn;
        double[] ax = L.Ax, ay = L.Ay, aw = L.Aw;
        byte[] ab = L.Ab;
        int n = ax.Length;
        double s1 = 1e9, s2 = 1e9;
        int b1 = GrassBiome, b2 = GrassBiome;
        for (int k = 0; k < n; k++)
        {
            double dx = u - ax[k], dy = v - ay[k];
            double s = Math.Sqrt(dx * dx + dy * dy) / aw[k];
            if (ab[k] == EvilBiome)
                s -= EvilRag(L, lx, y);
            if (s < s1)
            {
                s2 = s1;
                b2 = b1;
                s1 = s;
                b1 = ab[k];
            }
            else if (s < s2)
            {
                s2 = s;
                b2 = ab[k];
            }
        }
        if (b1 != b2 && s2 - s1 < EcoBand)
        {
            bmFlag = true;
            if (fine)
            {
                double p = 0.5 * (1 - (s2 - s1) / EcoBand);
                if (VNoise(lx / 9, y / 9, L.Seed + 611) < p)
                    return b2;
            }
        }
        return b1;
    }

    static Fields MakeFields(int W, int H, int seed, bool evilOn, double evilSize)
    {
        int gw = (W >> StrideShift) + 3, gh = (H >> StrideShift) + 3;
        int nn = gw * gh;
        float[] hF = new float[nn], wxF = new float[nn], wyF = new float[nn];
        byte[] bF = new byte[nn];
        double cx = W / 2.0, cy = H / 2.0;
        double sc = Math.Max(0.4, Math.Min(W, H) / 2400.0);
        Layout L = MakeLayout(W, H, seed, evilOn, evilSize);
        L.Sc = sc;
        const double P = 2.6;
        for (int gy = 0; gy < gh; gy++)
        {
            double y = gy * Stride;
            double v = (y - cy) / cy;
            for (int gx = 0; gx < gw; gx++)
            {
                double x = gx * Stride;
                double u = (x - cx) / cx;
                double cwx = (Fbm(x / (700 * sc), y / (700 * sc), seed + 61, 3) - 0.5) * 0.24;
                double cwy = (Fbm(x / (700 * sc) + 31.7, y / (700 * sc) - 17.3, seed + 62, 3) - 0.5) * 0.24;
                double ex = Math.Abs(u + cwx), ey = Math.Abs(v + cwy);
                double rs = Math.Pow(Math.Pow(ex, P) + Math.Pow(ey, P), 1 / P);
                double d = rs + (Fbm(x / (260 * sc), y / (260 * sc), seed + 7, 4) - 0.5) * 0.20;
                double land = 1 - Smooth(0.70, 0.95, d);
                double n = Fbm(x / (340 * sc), y / (340 * sc), seed, 5);
                double hl = Clamp01((n - 0.5) * 2.3 + 0.60);
                hl = Math.Max(hl, 0.56 * (1 - Smooth(0.30, 0.60, d)));
                int i = gy * gw + gx;
                hF[i] = (float)(0.15 + (hl - 0.15) * land);
                wxF[i] = (float)((Fbm(x / (560 * sc) + 9.1, y / (560 * sc), seed + 41, 4) - 0.5) * 0.55);
                wyF[i] = (float)((Fbm(x / (560 * sc), y / (560 * sc) + 4.7, seed + 43, 4) - 0.5) * 0.55);
            }
        }
        var F = new Fields { Gw = gw, Gh = gh, HF = hF, WxF = wxF, WyF = wyF, BF = bF, L = L };
        CleanPass(F, true);
        CleanPass(F, false);
        for (int gy = 0; gy < gh; gy++)
        {
            for (int gx = 0; gx < gw; gx++)
            {
                int i = gy * gw + gx;
                int b = BiomeExact(L, gx * Stride, gy * Stride, wxF[i], wyF[i], false);
                bF[i] = (byte)(bmFlag ? (b | 0x80) : b);
            }
        }
        return F;
    }

    static void CleanPass(Fields F, bool holes)
    {
        int gw = F.Gw, gh = F.Gh;
        float[] hF = F.HF;
        int nn = gw * gh;
        int[] lab = new int[nn];
        int[] stack = new int[nn];
        var compLand = new List<bool>();
        var compSize = new List<int>();
        var compBorder = new List<bool>();
        for (int s = 0; s < nn; s++)
        {
            if (lab[s] != 0)
                continue;
            bool isLandNode = hF[s] >= ThreshShallow;
            int id = compLand.Count + 1;
            int sp = 0, size = 0;
            bool touchesBorder = false;
            stack[sp++] = s;
            lab[s] = id;
            while (sp > 0)
            {
                int i = stack[--sp];
                size++;
                int gx = i % gw, gy = i / gw;
                if (gx == 0 || gy == 0 || gx == gw - 1 || gy == gh - 1)
                    touchesBorder = true;
                if (gx > 0)
                {
                    int j = i - 1;
                    if (lab[j] == 0 && (hF[j] >= ThreshShallow) == isLandNode)
                    {
                        lab[j] = id;
                        stack[sp++] = j;
                    }
                }
                if (gx < gw - 1)
                {
                    int j = i + 1;
                    if (lab[j] == 0 && (hF[j] >= ThreshShallow) == isLandNode)
                    {
                        lab[j] = id;
                        stack[sp++] = j;
                    }
                }
                if (gy > 0)
                {
                    int j = i - gw;
                    if (lab[j] == 0 && (hF[j] >= ThreshShallow) == isLandNode)
                    {
                        lab[j] = id;
                        stack[sp++] = j;
                    }
                }
                if (gy < gh - 1)
                {
                    int j = i + gw;
                    if (lab[j] == 0 && (hF[j] >= ThreshShallow) == isLandNode)
                    {
                        lab[j] = id;
                        stack[sp++] = j;
                    }
                }
            }
            compLand.Add(isLandNode);
            compSize.Add(size);
            compBorder.Add(touchesBorder);
        }
        int mainSize = 0;
        for (int c = 0; c < compLand.Count; c++)
        {
            if (compLand[c] && compSize[c] > mainSize)
                mainSize = compSize[c];
        }
        double keep = Math.Max(10, mainSize * 0.012);
        byte[] kill = new byte[compLand.Count + 1], fill = new byte[compLand.Count + 1];
        int killed = 0, filled = 0;
        for (int k = 0; k < compLand.Count; k++)
        {
            if (!holes && compLand[k] && compSize[k] < keep)
            {
                kill[k + 1] = 1;
                killed++;
            }
            else if (holes && !compLand[k] && !compBorder[k] && compSize[k] < 700)
            {
                fill[k + 1] = 1;
                filled++;
            }
        }
        if (killed + filled == 0)
            return;
        byte[] ring = new byte[nn];
        for (int i = 0; i < nn; i++)
        {
            if (kill[lab[i]] != 0)
            {
                int gx = i % gw, gy = i / gw;
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int x = gx + dx, y = gy + dy;
                        if (x >= 0 && y >= 0 && x < gw && y < gh)
                            ring[y * gw + x] = 1;
                    }
                }
            }
        }
        for (int i = 0; i < nn; i++)
        {
            int c = lab[i];
            if (ring[i] != 0 && !(c >= 1 && compLand[c - 1] && kill[c] == 0))
                hF[i] = Math.Min(hF[i], 0.29f);
            else if (fill[c] != 0)
                hF[i] = 0.55f;
        }
    }

    static int BiomeAt(Fields F, int x, int y)
    {
        int gx = x >> StrideShift, gy = y >> StrideShift, i = gy * F.Gw + gx, g = F.Gw;
        byte[] b = F.BF;
        int v = b[i];
        if (v < 0x80 && v == b[i + 1] && v == b[i + g] && v == b[i + g + 1])
            return v;
        return BiomeExact(F.L, x, y, Sample(F, F.WxF, x, y), Sample(F, F.WyF, x, y), true);
    }

    static double Sample(Fields F, float[] field, int x, int y)
    {
        int gx = x >> StrideShift, gy = y >> StrideShift;
        double fx = (x & (Stride - 1)) / (double)Stride, fy = (y & (Stride - 1)) / (double)Stride;
        int i = gy * F.Gw + gx;
        double a = field[i], b = field[i + 1], c = field[i + F.Gw], d = field[i + F.Gw + 1];
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }

    static Mask MakeMask(int Wr, int H)
    {
        int cw = (Wr >> 5) + 1, ch = (H >> 5) + 1;
        return new Mask { Cw = cw, Chunks = new byte[]?[cw * ch] };
    }

    static void MaskPut(Mask M, int x, int y, byte code)
    {
        int ci = (y >> 5) * M.Cw + (x >> 5);
        byte[] c = M.Chunks[ci] ??= new byte[1024];
        int k = ((y & 31) << 5) | (x & 31);
        if (c[k] < code)
            c[k] = code;
    }

    static int MaskGet(Mask M, int x, int y)
    {
        if (x < 0 || y < 0)
            return 0;
        int ci = (y >> 5) * M.Cw + (x >> 5);
        if (ci >= M.Chunks.Length)
            return 0;
        byte[]? c = M.Chunks[ci];
        return c != null ? c[((y & 31) << 5) | (x & 31)] : 0;
    }

    static double Crv(double p0, double p1, double p2, double p3, double t)
    {
        double t2 = t * t, t3 = t2 * t;
        return 0.5 * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
    }

    static WaterResult BuildWater(GenState S)
    {
        Fields F = S.F!;
        int Wr = S.Wr, H = S.H, seed = S.Seed;
        GenOpts opts = S.Opts;
        int gw = F.Gw, gh = F.Gh;
        float[] hF = F.HF;
        const int STR = Stride;
        double sc = F.L.Sc, cx = Wr / 2.0, cy = H / 2.0;
        int nn = gw * gh;
        Mask M = MakeMask(Wr, H);
        float[] prox = new float[nn];
        F.Prox = prox;
        var result = new WaterResult { M = M, Prox = prox };
        bool wantRivers = opts.Rivers, wantLakes = opts.Lakes;
        if (!wantRivers && !wantLakes)
            return result;

        void Put(int x, int y, byte code)
        {
            if (x >= 1 && y >= 1 && x < Wr - 1 && y < H - 1)
                MaskPut(M, x, y, code);
        }

        void AddProx(int x, int y, double rad)
        {
            int gx0 = x >> StrideShift, gy0 = y >> StrideShift;
            double R = rad + 26;
            for (int dy = -3; dy <= 4; dy++)
            {
                for (int dx = -3; dx <= 4; dx++)
                {
                    int gx = gx0 + dx, gy = gy0 + dy;
                    if (gx < 0 || gy < 0 || gx >= gw || gy >= gh)
                        continue;
                    double ddx = gx * STR - x, ddy = gy * STR - y;
                    double val = 1 - Math.Sqrt(ddx * ddx + ddy * ddy) / R;
                    int k = gy * gw + gx;
                    if (val > prox[k])
                        prox[k] = (float)val;
                }
            }
        }

        double landMin = ThreshBeach + 0.004;
        byte[] isLand = new byte[nn];
        int landN = 0;
        for (int i = 0; i < nn; i++)
        {
            if (hF[i] >= landMin)
            {
                isLand[i] = 1;
                landN++;
            }
        }
        result.LandNodes = landN;
        if (landN < 50)
            return result;
        float[] e = new float[nn];
        for (int gy = 0; gy < gh; gy++)
        {
            for (int gx = 0; gx < gw; gx++)
            {
                int i = gy * gw + gx;
                if (isLand[i] == 0)
                    continue;
                double x = gx * STR, y = gy * STR;
                double u = (x - cx) / cx, v = (y - cy) / cy;
                double depth = Clamp01((hF[i] - ThreshBeach) / 0.22);
                double relief = Fbm(x / (260 * sc), y / (260 * sc), seed + 9001, 5);
                double bump = 1 - Smooth(0, 0.22, Math.Sqrt(u * u + v * v));
                e[i] = (float)(0.5 * depth + 0.5 * relief + 0.7 * bump);
            }
        }

        int cap = landN + 16;
        float[] hk = new float[cap];
        int[] hv = new int[cap];
        int hs = 0;

        void HPush(double k, int v)
        {
            int c = hs++;
            while (c > 0)
            {
                int p = (c - 1) >> 1;
                if (hk[p] <= k)
                    break;
                hk[c] = hk[p];
                hv[c] = hv[p];
                c = p;
            }
            hk[c] = (float)k;
            hv[c] = v;
        }

        int HPop()
        {
            int top = hv[0];
            hs--;
            if (hs > 0)
            {
                float k = hk[hs];
                int v = hv[hs];
                int c = 0;
                for (; ; )
                {
                    int l = 2 * c + 1;
                    if (l >= hs)
                        break;
                    if (l + 1 < hs && hk[l + 1] < hk[l])
                        l++;
                    if (hk[l] >= k)
                        break;
                    hk[c] = hk[l];
                    hv[c] = hv[l];
                    c = l;
                }
                hk[c] = k;
                hv[c] = v;
            }
            return top;
        }

        int[] down = new int[nn];
        Array.Fill(down, -1);
        float[] filled = new float[nn];
        byte[] done = new byte[nn];
        int[] order = new int[landN];
        int on = 0;
        const double Eps = 2e-5;
        for (int gy = 1; gy < gh - 1; gy++)
        {
            for (int gx = 1; gx < gw - 1; gx++)
            {
                int i = gy * gw + gx;
                if (isLand[i] == 0)
                    continue;
                int best = -1;
                double bh = 9;
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0)
                            continue;
                        int j = i + dy * gw + dx;
                        if (isLand[j] == 0 && hF[j] < bh)
                        {
                            bh = hF[j];
                            best = j;
                        }
                    }
                }
                if (best >= 0)
                {
                    down[i] = -(best + 2);
                    filled[i] = e[i];
                    done[i] = 1;
                    HPush(e[i], i);
                }
            }
        }
        while (hs > 0)
        {
            int i = HPop();
            order[on++] = i;
            int gx = i % gw, gy = i / gw;
            if (gx < 1 || gy < 1 || gx >= gw - 1 || gy >= gh - 1)
                continue;
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;
                    int j = i + dy * gw + dx;
                    if (isLand[j] == 0 || done[j] != 0)
                        continue;
                    done[j] = 1;
                    double f = Math.Max(e[j], filled[i] + Eps);
                    filled[j] = (float)f;
                    down[j] = i;
                    HPush(f, j);
                }
            }
        }

        float[] acc = new float[nn];
        for (int k = 0; k < on; k++)
        {
            int i = order[k];
            int gx = i % gw, gy = i / gw;
            int b = F.BF[i] & 0x7f;
            acc[i] = (float)(Rain[b] * (0.75 + 0.5 * VNoise(gx * STR / 200.0, gy * STR / 200.0, seed + 9101)));
        }
        for (int k = on - 1; k >= 0; k--)
        {
            int i = order[k], d = down[i];
            if (d >= 0)
                acc[d] = (float)((double)acc[d] + acc[i]);
        }

        float[] vals = new float[on];
        for (int k = 0; k < on; k++)
            vals[k] = acc[order[k]];
        Array.Sort(vals);
        double dens = Math.Max(0.2, opts.RiverDensity != 0 ? opts.RiverDensity : 1);
        int target = Math.Max(8, Round(RiverRate * on * dens));
        double T = on > 0 ? Math.Max(5, vals[Math.Max(0, on - target)]) : double.NaN;
        result.T = T;
        bool IsRiver(int i) => isLand[i] == 1 && acc[i] >= T;

        if (wantRivers)
        {
            byte[] up = new byte[nn];
            for (int k = 0; k < on; k++)
            {
                int i = order[k];
                if (!IsRiver(i))
                    continue;
                int d = down[i];
                if (d >= 0 && IsRiver(d))
                    up[d]++;
            }
            byte[] seen = new byte[nn];
            double widthScale = Math.Max(0.3, opts.RiverWidth != 0 ? opts.RiverWidth : 1);
            double HalfW(double a) => 0.5 * widthScale * (3.2 + 2.2 * Math.Sqrt(Math.Max(0, a / T - 1)));
            double DispX(double x, double y) => (VNoise(x / 46, y / 46, seed + 121) - 0.5) * 28 + (VNoise(x / 130, y / 130, seed + 123) - 0.5) * 60;
            double DispY(double x, double y) => (VNoise(x / 46, y / 46, seed + 122) - 0.5) * 28 + (VNoise(x / 130, y / 130, seed + 124) - 0.5) * 60;
            var heads = new List<int>();
            for (int k = 0; k < on; k++)
            {
                int i = order[k];
                if (IsRiver(i) && up[i] == 0)
                    heads.Add(i);
            }
            // OrderByDescending is stable, as JavaScript's sort is.
            foreach (int h in heads.OrderByDescending(i => acc[i]))
            {
                var ids = new List<int> { h };
                seen[h] = 1;
                int cur = h;
                bool mouth = false;
                for (int guard = 0; guard < 100000; guard++)
                {
                    int d = down[cur];
                    if (d <= -2)
                    {
                        ids.Add(-d - 2);
                        mouth = true;
                        break;
                    }
                    if (d < 0 || !IsRiver(d))
                        break;
                    ids.Add(d);
                    if (seen[d] != 0)
                        break;
                    seen[d] = 1;
                    cur = d;
                }
                if (ids.Count < 5 && !(mouth && ids.Count >= 3))
                    continue;
                int m = ids.Count;
                double[] px = new double[m], py = new double[m], pw = new double[m];
                for (int k = 0; k < m; k++)
                {
                    int gx = ids[k] % gw, gy = ids[k] / gw;
                    double x = gx * STR, y = gy * STR;
                    px[k] = x + DispX(x, y);
                    py[k] = y + DispY(x, y);
                    pw[k] = HalfW(acc[ids[Math.Min(k, mouth ? m - 2 : m - 1)]]) * (mouth && k == m - 1 ? 1.2 : 1);
                }
                result.Rivers++;
                if (mouth)
                    result.Mouths++;
                int sinceProx = 0;
                for (int k = 0; k < m - 1; k++)
                {
                    int a0 = Math.Max(0, k - 1), a3 = Math.Min(m - 1, k + 2);
                    double ldx = px[k + 1] - px[k], ldy = py[k + 1] - py[k];
                    double len = Math.Sqrt(ldx * ldx + ldy * ldy);
                    int steps = Math.Max(2, (int)Math.Ceiling(len / 1.5));
                    for (int s = 0; s < steps; s++)
                    {
                        double t = (double)s / steps;
                        double x = Crv(px[a0], px[k], px[k + 1], px[a3], t), y = Crv(py[a0], py[k], py[k + 1], py[a3], t);
                        double r0 = pw[k] + (pw[k + 1] - pw[k]) * t;
                        double r = r0 * (0.72 + 0.56 * VNoise(x / 8, y / 8, seed + 131));
                        double bank = 1.2 + 2.4 * VNoise(x / 9, y / 9, seed + 133);
                        double R = r + bank;
                        int xi0 = (int)Math.Floor(x - R), xi1 = (int)Math.Ceiling(x + R), yi0 = (int)Math.Floor(y - R), yi1 = (int)Math.Ceiling(y + R);
                        double r2 = r * r, R2 = R * R;
                        for (int yy = yi0; yy <= yi1; yy++)
                        {
                            double dy = yy - y;
                            for (int xx = xi0; xx <= xi1; xx++)
                            {
                                double dx = xx - x, d2 = dx * dx + dy * dy;
                                if (d2 <= r2)
                                    Put(xx, yy, 3);
                                else if (d2 <= R2)
                                    Put(xx, yy, 2);
                            }
                        }
                        if (++sinceProx >= 5)
                        {
                            sinceProx = 0;
                            AddProx(Round(x), Round(y), r);
                        }
                    }
                }
            }
            for (int i = 0; i < nn; i++)
            {
                if (IsRiver(i))
                    result.RiverNodes++;
            }
        }

        if (wantLakes)
        {
            float[] ld = new float[nn];
            byte[] cand = new byte[nn];
            for (int k = 0; k < on; k++)
            {
                int i = order[k];
                double dep = (double)filled[i] - e[i];
                if (dep > LakeDepth && acc[i] >= 0.5 * T)
                {
                    ld[i] = (float)dep;
                    cand[i] = 1;
                }
            }
            int[] comp = new int[nn], stack = new int[nn];
            int nc = 0;
            var members = new List<int>();
            for (int s = 0; s < nn; s++)
            {
                if (cand[s] == 0 || comp[s] != 0)
                    continue;
                int sp = 0, size = 0;
                members.Clear();
                stack[sp++] = s;
                comp[s] = ++nc;
                while (sp > 0)
                {
                    int i = stack[--sp];
                    size++;
                    members.Add(i);
                    int gx = i % gw, gy = i / gw;
                    if (gx > 0 && cand[i - 1] != 0 && comp[i - 1] == 0)
                    {
                        comp[i - 1] = nc;
                        stack[sp++] = i - 1;
                    }
                    if (gx < gw - 1 && cand[i + 1] != 0 && comp[i + 1] == 0)
                    {
                        comp[i + 1] = nc;
                        stack[sp++] = i + 1;
                    }
                    if (gy > 0 && cand[i - gw] != 0 && comp[i - gw] == 0)
                    {
                        comp[i - gw] = nc;
                        stack[sp++] = i - gw;
                    }
                    if (gy < gh - 1 && cand[i + gw] != 0 && comp[i + gw] == 0)
                    {
                        comp[i + gw] = nc;
                        stack[sp++] = i + gw;
                    }
                }
                if (size < 3)
                {
                    foreach (int i in members)
                    {
                        ld[i] = 0;
                        cand[i] = 0;
                    }
                }
                else
                {
                    result.Lakes++;
                }
            }
            double iso = 0.5 * LakeDepth;
            for (int gy = 0; gy < gh - 1; gy++)
            {
                for (int gx = 0; gx < gw - 1; gx++)
                {
                    int i = gy * gw + gx;
                    double a = ld[i], b = ld[i + 1], c = ld[i + gw], d = ld[i + gw + 1];
                    if (a == 0 && b == 0 && c == 0 && d == 0)
                        continue;
                    int x0 = gx * STR, y0 = gy * STR;
                    for (int ty = 0; ty < STR; ty++)
                    {
                        double fy = ty / (double)STR;
                        for (int tx = 0; tx < STR; tx++)
                        {
                            double fx = tx / (double)STR;
                            double v = a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy
                                + (VNoise((x0 + tx) / 6.0, (y0 + ty) / 6.0, seed + 9301) - 0.5) * iso * 0.9;
                            if (v > iso)
                                Put(x0 + tx, y0 + ty, 3);
                            else if (v > iso * 0.30)
                                Put(x0 + tx, y0 + ty, 2);
                        }
                    }
                    AddProx(x0 + 4, y0 + 4, 12);
                }
            }
        }

        if (wantLakes)
        {
            int want = Math.Max(1, Round(1.5 + 2.5 * sc * sc));
            var spots = new List<(int X, int Y, double R)>();
            for (int k = 0; k < 900 && spots.Count < want; k++)
            {
                int ox = (int)Math.Floor(Hash2(k, 11, seed + 7001) * Wr), oy = (int)Math.Floor(Hash2(k, 12, seed + 7001) * H);
                if (ox < 80 || oy < 80 || ox > Wr - 80 || oy > H - 80)
                    continue;
                if (Sample(F, hF, ox, oy) < ThreshBeach + 0.08)
                    continue;
                if (BiomeAt(F, ox, oy) != DesertBiome || MaskGet(M, ox, oy) != 0)
                    continue;
                bool ok = true;
                for (int q = 0; q < 8 && ok; q++)
                {
                    double a = q * Math.PI / 4;
                    int sx = Round(ox + Math.Cos(a) * 55 * Math.Max(0.6, sc)), sy = Round(oy + Math.Sin(a) * 55 * Math.Max(0.6, sc));
                    if (sx < 4 || sy < 4 || sx >= Wr - 4 || sy >= H - 4 || BiomeAt(F, sx, sy) != DesertBiome || Sample(F, hF, sx, sy) < ThreshBeach + 0.05 || MaskGet(M, sx, sy) != 0)
                        ok = false;
                }
                foreach ((int X, int Y, double R) s in spots)
                {
                    double ddx = s.X - ox, ddy = s.Y - oy;
                    if (Math.Sqrt(ddx * ddx + ddy * ddy) < 220 * Math.Max(0.6, sc))
                        ok = false;
                }
                if (ok)
                    spots.Add((ox, oy, 6 + Hash2(k, 13, seed + 7001) * 5));
            }
            foreach ((int X, int Y, double R) s in spots)
            {
                double bank = 2.2, ring = 6 + Hash2(s.X, s.Y, seed + 7002) * 4;
                double R = s.R + bank + ring + 3;
                for (int y = (int)Math.Floor(s.Y - R); y <= (int)Math.Ceiling(s.Y + R); y++)
                {
                    for (int x = (int)Math.Floor(s.X - R); x <= (int)Math.Ceiling(s.X + R); x++)
                    {
                        double ddx = x - s.X, ddy = y - s.Y;
                        double dd = Math.Sqrt(ddx * ddx + ddy * ddy) + (VNoise(x / 5.0, y / 5.0, seed + 7003) - 0.5) * 3.2;
                        if (dd < s.R)
                            Put(x, y, 3);
                        else if (dd < s.R + bank)
                            Put(x, y, 2);
                        else if (dd < s.R + bank + ring * (0.6 + 0.8 * VNoise(x / 13.0, y / 13.0, seed + 7004)))
                            Put(x, y, 1);
                    }
                }
                AddProx(s.X, s.Y, s.R + 6);
                result.Oases++;
            }
        }
        return result;
    }

    static Point2 FindSpawn(int W, int H, Fields F, Mask? M)
    {
        int cx = W >> 1, cy = H >> 1;
        int maxR = Math.Max(W, H);
        for (int r = 0; r < maxR; r += 8)
        {
            int steps = Math.Max(1, r / 4);
            for (int k = 0; k < steps; k++)
            {
                double a = ((double)k / steps) * Math.PI * 2;
                int x = Round(cx + Math.Cos(a) * r);
                int y = Round(cy + Math.Sin(a) * r * 0.5);
                if (x < 60 || y < 60 || x > W - 60 || y > H - 60)
                    continue;
                bool ok = true;
                for (int dy = -ClearRadius - 4; dy <= ClearRadius + 4 && ok; dy += 6)
                {
                    for (int dx = -ClearRadius - 4; dx <= ClearRadius + 4; dx += 6)
                    {
                        double h = Sample(F, F.HF, x + dx, y + dy);
                        if (h < ThreshBeach + 0.02 || h > ThreshHigh - 0.02)
                        {
                            ok = false;
                            break;
                        }
                        if (M != null && MaskGet(M, x + dx, y + dy) != 0)
                        {
                            ok = false;
                            break;
                        }
                    }
                }
                if (ok)
                    return new Point2 { X = x, Y = y };
            }
        }
        return new Point2 { X = cx, Y = cy };
    }

    static void Report(GenerationProgress progress, string? message, double? value)
    {
        if (progress.IsNull)
            return;
        if (message != null)
            progress.Message = message;
        if (value != null)
            progress.Set(value.Value);
    }

    public static List<Stage> MakeStages(Func<GenOpts> getOpts, Action<GenResult>? onDone)
    {
        var S = new GenState();
        Stage Make(string name, double weight, Action<GenerationProgress> fn) => new()
        {
            Name = name,
            Weight = weight,
            Run = progress =>
            {
                if (S.Failed)
                    return;
                try
                {
                    fn(progress);
                }
                catch (Exception e)
                {
                    S.Failed = true;
                    Log("stage \"" + name + "\" failed, generation stopped: " + e);
                }
            },
        };

        return new List<Stage>
        {
            Make("Top-Down: relief and biomes", 4, progress =>
            {
                GenOpts opts = getOpts();
                S.Opts = opts;
                S.T0 = Environment.TickCount64;
                S.Seed = opts.Seed ?? SeedFromWorld();
                int W = Main.maxTilesX, H = Main.maxTilesY;
                S.W = W;
                S.H = H;
                S.Wr = opts.Shape == WorldShapeMode.Square ? Math.Min(W, H) : W;
                S.X0 = (W - S.Wr) >> 1;
                EvilBiomeMode evilMode = opts.Evil;
                S.EvilOn = evilMode != EvilBiomeMode.None;
                SetEvilFlavor(evilMode == EvilBiomeMode.Crimson ? "Crimson" : evilMode == EvilBiomeMode.Corruption ? "Corrupt" : WorldEvilFlavor());
                S.P = BuildPalette();
                S.Tab = BuildTables(S.P);
                Report(progress, "Relief and biomes", 0.2);
                S.F = MakeFields(S.Wr, H, S.Seed, S.EvilOn, opts.EvilSize > 0 ? opts.EvilSize : 1);
                S.Kinds = opts.SoftEdges ? new byte[S.Wr * H] : null;
                S.Stats = new Stats();
                S.Fresh = opts.Fresh && IsWorldBlank(W, H, S.Seed);
                Log(W + "x" + H + ", playable area " + S.Wr + "x" + H + ", world " + (S.Fresh ? "blank (no tile-by-tile clearing)" : "with content (clearing)") + ", evil: " + (S.EvilOn ? evilFlavor : "none"));
                Report(progress, null, 1);
            }),

            Make("Top-Down: rivers and lakes", 6, progress =>
            {
                Report(progress, "Rivers, lakes and oases", 0.1);
                try
                {
                    S.Water = BuildWater(S);
                    WaterResult w = S.Water;
                    Log("water: " + w.Rivers + " rivers (" + w.Mouths + " reaching the sea, " + w.RiverNodes + " stretches, flow limit " + w.T.ToString("0.0") + "), " + w.Lakes + " lakes, " + w.Oases + " oases");
                }
                catch (Exception e)
                {
                    S.Water = new WaterResult { M = MakeMask(S.Wr, S.H) };
                    Log("rivers and lakes failed (the world goes on without them): " + e);
                }
                S.Spawn = FindSpawn(S.Wr, S.H, S.F!, S.Water.M);
                S.SpawnLX = S.Spawn.X;
                S.Spawn.X += S.X0;
                Report(progress, null, 1);
            }),

            Make("Top-Down: terrain and sea", 30, progress =>
            {
                int W = S.W, H = S.H, Wr = S.Wr, x0 = S.X0, seed = S.Seed;
                Palette P = S.P;
                Tables tab = S.Tab;
                Fields F = S.F!;
                byte[]? kinds = S.Kinds;
                Point2 spawn = S.Spawn;
                Stats stats = S.Stats;
                Mask M = S.Water!.M;
                const int clear2 = ClearRadius * ClearRadius;
                int detailSeed = seed + 3000;
                if (!S.Fresh && x0 > 0)
                    ClearOutside(W, H, x0, Wr);
                float[] hF = F.HF;
                for (int y = 0; y < H; y++)
                {
                    int gy = y >> StrideShift;
                    double fy = (y & (Stride - 1)) / (double)Stride;
                    bool nearSpawnRow = Math.Abs(y - spawn.Y) <= ClearRadius;
                    bool edgeRow = y < Edge || y >= H - Edge;
                    int mrow = (y >> 5) * M.Cw, myy = (y & 31) << 5;
                    for (int lx = 0; lx < Wr; lx++)
                    {
                        int x = lx + x0;
                        int gx = lx >> StrideShift;
                        double fx = (lx & (Stride - 1)) / (double)Stride;
                        int i = gy * F.Gw + gx, g = F.Gw;
                        double h00 = hF[i], h10 = hF[i + 1], h01 = hF[i + g], h11 = hF[i + g + 1];
                        double h = h00 + (h10 - h00) * fx + (h01 - h00) * fy + (h00 - h10 - h01 + h11) * fx * fy;
                        if (Math.Abs(h - ThreshDeep) < 0.04 || Math.Abs(h - ThreshShallow) < 0.04 || Math.Abs(h - ThreshBeach) < 0.04)
                            h += (VNoise(x / 7.0, y / 7.0, detailSeed) - 0.5) * 0.07;
                        bool edgeCell = edgeRow || lx < Edge || lx >= Wr - Edge;
                        int zone = edgeCell ? DeepZone : ZoneOf(h), biome = BiomeAt(F, lx, y);
                        if (!edgeCell)
                        {
                            byte[]? mc = M.Chunks[mrow + (lx >> 5)];
                            if (mc != null)
                            {
                                int rv = mc[myy | (lx & 31)];
                                if (rv == 3)
                                {
                                    if (zone >= ShallowZone)
                                        zone = ShallowZone;
                                }
                                else if (rv == 2)
                                {
                                    if (zone >= BeachZone)
                                        zone = BeachZone;
                                }
                                else if (rv == 1)
                                {
                                    if (zone == LowZone)
                                        biome = GrassBiome;
                                }
                            }
                        }
                        if (nearSpawnRow && (x - spawn.X) * (x - spawn.X) + (y - spawn.Y) * (y - spawn.Y) < clear2)
                        {
                            zone = LowZone;
                            biome = GrassBiome;
                        }
                        int wall = tab.Floor[zone * Nb + biome];
                        int block = tab.Block[zone * Nb + biome];
                        if (block == P.TileGlass && !edgeCell)
                            block = 0;
                        if (kinds != null)
                            kinds[y * Wr + lx] = (byte)tab.Kind[zone * Nb + biome];

                        Tile t = Main.tile[x, y];
                        if (!S.Fresh)
                            t.ClearEverything();
                        t.wall = (ushort)wall;
                        if (block != 0)
                        {
                            t.sTileHeader = TileScan.ActiveBit;
                            t.type = (ushort)block;
                            if (block == P.TileGlass)
                            {
                                stats.Glass++;
                                t.invisibleBlock(true);
                            }
                            stats.Obstacles++;
                        }
                        stats.Writes++;
                    }
                    if ((y & 15) == 0)
                        Report(progress, null, (double)y / H);
                }
                Report(progress, null, 1);
            }),

            Make("Top-Down: edges between floors", 6, progress =>
            {
                if (S.Kinds == null)
                    return;
                Report(progress, "Edges between floors", 0);
                try
                {
                    S.Stats.Edges = ApplySoftEdges(S.Kinds, S.Wr, S.H, S.P, S.X0);
                }
                catch (Exception e)
                {
                    Log("edges between floors failed (the world goes on with straight edges): " + e);
                }
                S.Kinds = null;
                Report(progress, null, 1);
            }),

            Make("Top-Down: framing", 4, progress =>
            {
                Report(progress, "Adjusting frames", 0);
                FrameWorld(S.X0, S.X0 + S.Wr, S.H, progress);
                Report(progress, null, 1);
            }),

            Make("Top-Down: trees", 28, progress =>
            {
                Fields F = S.F!;
                Mask M = S.Water!.M;
                int x0 = S.X0, Wr = S.Wr, H = S.H;
                GenOpts opts = S.Opts;
                Stats stats = S.Stats;
                S.TreeIdx = MakeSpacing(Wr, H);
                string treeInfo = "";
                if (opts.Trees == TreesMode.Terraria)
                {
                    TreeResult r = PlantRealTrees(F, S.P, S.Spawn, S.SpawnLX, x0, Wr, H, S.Seed, progress, opts.TreeDensity != 0 ? opts.TreeDensity : 1, M, S.TreeIdx);
                    stats.Trees = r.Ok;
                    treeInfo = "Terraria trees " + r.Ok + "/" + r.Tried;
                    if (r.Ok == 0)
                    {
                        Log("no Terraria tree grew (" + (r.Err != "" ? r.Err : "no error") + "): using leaf trees.");
                        stats.Trees = PlantBlobTrees(F, S.P, S.Spawn, S.SpawnLX, x0, Wr, H, S.Seed, progress, M);
                        treeInfo += " + blobs " + stats.Trees;
                        FrameWorld(x0, x0 + Wr, H, default);
                    }
                }
                else if (opts.Trees == TreesMode.Blob)
                {
                    stats.Trees = PlantBlobTrees(F, S.P, S.Spawn, S.SpawnLX, x0, Wr, H, S.Seed, progress, M);
                    treeInfo = "blobs " + stats.Trees;
                    FrameWorld(x0, x0 + Wr, H, default);
                }
                Log(treeInfo != "" ? treeInfo : "no trees");
                Report(progress, null, 1);
            }),

            Make("Top-Down: cacti", 4, progress =>
            {
                GenOpts opts = S.Opts;
                if (!opts.Cactus)
                    return;
                Report(progress, "Desert cacti", 0);
                try
                {
                    S.Stats.Cacti = PlantCacti(S.F!, S.P, S.X0, S.Wr, S.H, S.Seed, progress, opts.CactusDensity != 0 ? opts.CactusDensity : 1, S.Water!.M);
                }
                catch (Exception e)
                {
                    Log("cacti failed (the world goes on without them): " + e);
                }
                Report(progress, null, 1);
            }),

            Make("Top-Down: rocks and grass", 12, progress =>
            {
                GenOpts opts = S.Opts;
                if (!opts.Decor)
                    return;
                Report(progress, "Rocks and grass", 0);
                try
                {
                    DecorResult d = DecorateWorld(S.P, new DecorOpts { Seed = S.Seed + 9000, Density = opts.DecorDensity != 0 ? opts.DecorDensity : 1, X0 = S.X0, X1 = S.X0 + S.Wr, Spawn = S.Spawn, Water = S.Water?.M });
                    S.Stats.Rocks = d.Rocks;
                    S.Stats.Plants = d.Plants;
                    S.Stats.WaterRocks = d.WaterRocks;
                }
                catch (Exception e)
                {
                    Log("decoration failed (the world goes on without it): " + e);
                }
                Report(progress, null, 1);
            }),

            Make("Top-Down: finishing", 2, progress =>
            {
                int W = S.W, H = S.H;
                Point2 spawn = S.Spawn;
                Stats stats = S.Stats;
                CleanupVanilla(spawn);
                ApplyLayers(H);
                WorldGen.noTileActions = false;
                Log("terrain generated in " + ((Environment.TickCount64 - S.T0) / 1000.0).ToString("0.0") + " s: " + stats.Writes + " tiles written, " + stats.Obstacles + " obstacles (" + stats.Glass + " of deep sea), "
                    + stats.Trees + " trees, " + stats.Cacti + " cacti, " + stats.Rocks + " rocks (" + stats.WaterRocks + " in the water), " + stats.Plants + " tufts; spawn " + spawn.X + "," + spawn.Y);
                var result = new GenResult { Spawn = spawn, Palette = S.P, Evil = S.EvilOn ? evilFlavor : null };
                S.F = null;
                S.Kinds = null;
                S.Water = null;
                S.TreeIdx = null;
                onDone?.Invoke(result);
                Report(progress, null, 1);
            }),
        };
    }

    public static GenResult GenerateTerrain(GenerationProgress progress, int seed, GenOpts opts)
    {
        opts.Seed = seed;
        GenResult? result = null;
        List<Stage> stages = MakeStages(() => opts, r => result = r);
        foreach (Stage st in stages)
            st.Run(progress);
        return result ?? throw new InvalidOperationException("the top-down generation did not finish (see the log)");
    }

    static bool IsWorldBlank(int W, int H, int seed)
    {
        try
        {
            for (int k = 0; k < 400; k++)
            {
                int x = (int)Math.Floor(Hash2(k, 1, seed + 5) * W), y = (int)Math.Floor(Hash2(k, 2, seed + 5) * H);
                Tile t = Main.tile[x, y];
                if (t.wall != 0 || t.type != 0 || (t.sTileHeader & TileScan.ActiveBit) != 0)
                    return false;
            }
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static void ClearOutside(int W, int H, int x0, int Wr)
    {
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                if (x >= x0 && x < x0 + Wr)
                {
                    x = x0 + Wr - 1;
                    continue;
                }
                Tile t = Main.tile[x, y];
                t.ClearEverything();
            }
        }
    }

    static string WorldEvilFlavor() => WorldGen.crimson ? "Crimson" : "Corrupt";

    static void FrameWorld(int xa, int xb, int H, GenerationProgress progress)
    {
        const int step = 100;
        for (int x = xa; x < xb; x += step)
        {
            try
            {
                WorldGen.RangeFrame(x, 0, Math.Min(Main.maxTilesX - 1, x + step), H - 1);
            }
            catch (Exception e)
            {
                Log("RangeFrame failed at x=" + x + ": " + e);
                return;
            }
            Report(progress, null, (double)(x - xa) / Math.Max(1, xb - xa));
        }
    }

    static void CleanupVanilla(Point2 spawn)
    {
        int chests = 0, signs = 0;
        try
        {
            for (int i = 0; i < Main.chest.Length; i++)
            {
                if (!Main.chest[i].IsNull)
                {
                    Main.chest[i] = default;
                    chests++;
                }
            }
        }
        catch (Exception e)
        {
            Log("could not clear chests: " + e);
        }
        try
        {
            for (int i = 0; i < Main.sign.Length; i++)
            {
                if (!Main.sign[i].IsNull)
                {
                    Main.sign[i] = default;
                    signs++;
                }
            }
        }
        catch (Exception e)
        {
            Log("could not clear signs: " + e);
        }
        try
        {
            TileEntity.Clear();
        }
        catch (Exception e)
        {
            Log("TileEntity.Clear: " + e);
        }
        Main.spawnTileX = spawn.X;
        Main.spawnTileY = spawn.Y;
        try
        {
            for (int i = 0; i < 200; i++)
            {
                NPC n = Main.npc[i];
                if (n.IsNull || !n.active || !n.townNPC)
                    continue;
                n.position.X = spawn.X * 16 + 8;
                n.position.Y = spawn.Y * 16 - n.height;
                n.homeTileX = spawn.X;
                n.homeTileY = spawn.Y;
            }
        }
        catch (Exception e)
        {
            Log("town NPCs: " + e);
        }
        Log("cleanup: " + chests + " chest(s), " + signs + " sign(s) removed.");
    }

    // JavaScript also tried to set Main.UnderworldLayer, which has no setter in the phone's game
    // and only ever logged that it could not.
    static void ApplyLayers(int H)
    {
        Main.worldSurface = H - Edge - 10;
        Main.rockLayer = H - Edge - 8;
    }

    static readonly Dictionary<int, bool> lightBackup = new();
    static readonly Dictionary<int, int> blendBackup = new();
    static bool blendLogged;

    public static void ApplyFloorLight(Palette? palette, bool unify)
    {
        Palette P = palette ?? BuildPalette();
        void Light(int id)
        {
            if (!lightBackup.ContainsKey(id))
                lightBackup[id] = Main.wallLight[id];
            Main.wallLight[id] = true;
        }
        foreach (int id in P.Walls())
            Light(id);
        foreach (int id in P.Mix.Ids)
            Light(id);
        foreach (int id in P.Stone.Ids)
            Light(id);

        if (P.WallShallow > 0)
        {
            foreach (int id in P.Stone.ByKind[KShallow])
                Main.wallBlend[id] = P.WallShallow;
        }

        if (!unify)
        {
            foreach (KeyValuePair<int, int> entry in blendBackup)
                Main.wallBlend[entry.Key] = entry.Value;
            if (!blendLogged)
            {
                blendLogged = true;
                Log("edges between floors: native to Terraria (soft transitions off)");
            }
            return;
        }
        var land = new List<int> { P.WallGrass, P.WallDesert, P.WallSand, P.WallSnow, P.WallJungle, P.WallRock };
        land.AddRange(P.Mix.Ids);
        foreach (KeyValuePair<int, List<int>> entry in P.Stone.ByKind)
        {
            if (entry.Key != KShallow)
                land.AddRange(entry.Value);
        }
        int common = P.WallGrass;
        string before = "";
        foreach (int id in land)
        {
            if (!blendBackup.ContainsKey(id))
                blendBackup[id] = Main.wallBlend[id];
            if (!blendLogged && before.Length < 80)
                before += id + ":" + Main.wallBlend[id] + " ";
            Main.wallBlend[id] = common;
        }
        if (!blendLogged)
        {
            blendLogged = true;
            Log("outline between floors removed (wallBlend=" + common + "; before: " + before.Trim() + ")");
        }
    }

    public static void RestoreFloorLight()
    {
        foreach (KeyValuePair<int, bool> entry in lightBackup)
            Main.wallLight[entry.Key] = entry.Value;
        foreach (KeyValuePair<int, int> entry in blendBackup)
            Main.wallBlend[entry.Key] = entry.Value;
    }

    public static int SeedFromWorld() => WorldGen.genRand.Next(1, 2147483647);

    static Spacing MakeSpacing(int Wr, int H)
    {
        int bw = (Wr >> 3) + 2, bh = (H >> 3) + 2;
        return new Spacing { Bw = bw, Bh = bh, Cells = new List<int>?[bw * bh] };
    }

    static bool SpacingHit(Spacing SP, int px, int py, int rx, int ry)
    {
        int bx0 = Math.Max(0, (px - rx) >> 3), bx1 = Math.Min(SP.Bw - 1, (px + rx) >> 3);
        int by0 = Math.Max(0, (py - ry) >> 3), by1 = Math.Min(SP.Bh - 1, (py + ry) >> 3);
        for (int by = by0; by <= by1; by++)
        {
            for (int bx = bx0; bx <= bx1; bx++)
            {
                List<int>? a = SP.Cells[by * SP.Bw + bx];
                if (a == null)
                    continue;
                for (int k = 0; k < a.Count; k += 2)
                {
                    if (Math.Abs(a[k] - px) <= rx && Math.Abs(a[k + 1] - py) <= ry)
                        return true;
                }
            }
        }
        return false;
    }

    static void SpacingAdd(Spacing SP, int px, int py)
    {
        int k = (py >> 3) * SP.Bw + (px >> 3);
        List<int> cell = SP.Cells[k] ??= new List<int>(2);
        cell.Add(px);
        cell.Add(py);
    }

    static int PermStep(int n)
    {
        static int Gcd(int a, int b)
        {
            while (b != 0)
            {
                int t = a % b;
                a = b;
                b = t;
            }
            return a;
        }
        int s = (int)Math.Floor(n * 0.6180339887) | 1;
        while (Gcd(s, n) != 1)
            s += 2;
        return s;
    }

    static double TreeWeight(Fields F, Mask? M, int spawnLX, int spawnY, int px, int py, int seed)
    {
        double h = Sample(F, F.HF, px, py);
        if (h < ThreshBeach + 0.03)
            return 0;
        int rv = M != null ? MaskGet(M, px, py) : 0;
        if (rv >= 2)
            return 0;
        int biome = BiomeAt(F, px, py);
        if (rv == 1)
            biome = GrassBiome;
        double sc = F.L.Sc;
        double g = Fbm(px / (70 * sc), py / (70 * sc), seed + 5000, 3);
        double w;
        if (biome == GrassBiome)
            w = 0.95 * (0.20 + 0.80 * Smooth(0.34, 0.58, g));
        else if (biome == JungleBiome)
            w = 1.0 * (0.50 + 0.50 * Smooth(0.30, 0.55, g));
        else if (biome == SnowBiome)
            w = 0.70 * (0.25 + 0.75 * Smooth(0.36, 0.60, g));
        else if (biome == EvilBiome)
            w = 0.65 * (0.25 + 0.75 * Smooth(0.36, 0.60, g));
        else
            return 0;
        w *= Smooth(ThreshBeach + 0.03, ThreshBeach + 0.10, h);
        if (F.Prox != null)
            w += 0.30 * Math.Max(0, Sample(F, F.Prox, px, py));
        double ddx = px - spawnLX, ddy = py - spawnY;
        double d = Math.Sqrt(ddx * ddx + ddy * ddy);
        w *= Smooth(ClearRadius + 2, ClearRadius + 26, d);
        return w;
    }

    static bool GetTreeCandidate(Fields F, Mask? M, int spawnLX, Point2 spawn, int cx, int cy, int cell, int ox, int oy, int seed, double scale, out TreeCandidate c)
    {
        c = default;
        int px = ox + cx * cell + (int)Math.Floor(Hash2(cx, cy, seed + 11) * cell);
        int py = oy + cy * cell + (int)Math.Floor(Hash2(cx, cy, seed + 12) * cell);
        int dxs = px - spawnLX, dys = py - spawn.Y;
        if (dxs * dxs + dys * dys < ClearRadius * ClearRadius * 1.6)
            return false;
        if (dys > 0 && dys < 26 && Math.Abs(dxs) < 10)
            return false;
        double w = TreeWeight(F, M, spawnLX, spawn.Y, px, py, seed);
        if (w <= 0)
            return false;
        if (Hash2(cx, cy, seed + 13) > Math.Min(1, w * scale))
            return false;
        int biome = BiomeAt(F, px, py);
        if (M != null && MaskGet(M, px, py) == 1)
            biome = GrassBiome;
        c = new TreeCandidate { Px = px, Py = py, Biome = biome };
        return true;
    }

    static int PlantBlobTrees(Fields F, Palette P, Point2 spawn, int spawnLX, int x0, int Wr, int H, int seed, GenerationProgress progress, Mask? M)
    {
        const int cell = 6, ox = Edge + 8, oy = Edge + 8;
        int ncx = (Wr - 2 * ox) / cell, ncy = (H - 2 * oy) / cell;
        int trees = 0;
        Report(progress, "Top-down forests", 0.85);
        for (int cy = 0; cy < ncy; cy++)
        {
            for (int cx = 0; cx < ncx; cx++)
            {
                if (!GetTreeCandidate(F, M, spawnLX, spawn, cx, cy, cell, ox, oy, seed, 1.2, out TreeCandidate c))
                    continue;
                int leaf = c.Biome == SnowBiome ? P.TileIce : P.TileLeaf;
                double rr = 1.2 + Hash2(cx, cy, seed + 14) * 1.1;
                int ir = (int)Math.Ceiling(rr);
                for (int oyy = -ir; oyy <= ir; oyy++)
                {
                    for (int oxx = -ir; oxx <= ir; oxx++)
                    {
                        if (oxx * oxx + oyy * oyy > rr * rr)
                            continue;
                        Tile t = Main.tile[x0 + c.Px + oxx, c.Py + oyy];
                        t.sTileHeader = TileScan.ActiveBit;
                        t.type = (ushort)((oxx == 0 && oyy == 0) ? P.TileWood : leaf);
                    }
                }
                trees++;
            }
        }
        return trees;
    }

    sealed class TreeStat
    {
        public int Tries, Wins, A, B, C, Fails;
        public bool Off;
    }

    // Grows Terraria trees over the plain floor: the ground block goes in under the sapling spot,
    // and it ends up invisible once the tree stands.
    sealed class TreeContext
    {
        readonly Palette P;
        readonly Dictionary<int, TreeStat> stat = new();
        int diag;

        public readonly TreeResult Res = new();

        public TreeContext(Palette palette) => P = palette;

        int GroundOf(int biome) => biome == SnowBiome ? P.TileSnow : biome == JungleBiome ? P.TileJungleGrass
            : biome == EvilBiome ? (evilFlavor == "Crimson" ? P.TileCrimsonGrass : P.TileCorruptGrass) : P.TileGrass;

        bool Attempt(int gx, int gy, bool walls)
        {
            bool r = false;
            try
            {
                r = WorldGen.GrowTree(gx, gy, 0, walls);
            }
            catch (Exception e)
            {
                Res.Err = e.ToString();
                if (Res.Tried <= 3)
                    Log("GrowTree failed: " + e);
            }
            return r || TileScan.TypeAt(gx, gy - 1) == TileID.Trees;
        }

        bool AttemptVariant(int gx, int gy, bool withSides, int groundType)
        {
            ushort[] saved = new ushort[7 * 22];
            int k = 0;
            for (int dx = -3; dx <= 3; dx++)
            {
                for (int dy = -22; dy <= -1; dy++)
                {
                    Tile t = Main.tile[gx + dx, gy + dy];
                    saved[k++] = t.wall;
                    t.wall = 0;
                }
            }
            Tile sideA = default, sideB = default;
            short headerA = 0, headerB = 0;
            ushort typeA = 0, typeB = 0;
            if (withSides)
            {
                sideA = Main.tile[gx - 1, gy];
                headerA = sideA.sTileHeader;
                typeA = sideA.type;
                sideA.sTileHeader = TileScan.ActiveBit;
                sideA.type = (ushort)groundType;
                sideB = Main.tile[gx + 1, gy];
                headerB = sideB.sTileHeader;
                typeB = sideB.type;
                sideB.sTileHeader = TileScan.ActiveBit;
                sideB.type = (ushort)groundType;
            }
            try
            {
                return Attempt(gx, gy, true);
            }
            finally
            {
                k = 0;
                for (int dx = -3; dx <= 3; dx++)
                {
                    for (int dy = -22; dy <= -1; dy++)
                    {
                        Tile t = Main.tile[gx + dx, gy + dy];
                        t.wall = saved[k++];
                    }
                }
                if (withSides)
                {
                    sideA.sTileHeader = headerA;
                    sideA.type = typeA;
                    sideB.sTileHeader = headerB;
                    sideB.type = typeB;
                }
            }
        }

        void Diagnose(int gx, int gy)
        {
            try
            {
                Tile t = Main.tile[gx, gy - 1];
                int ground = TileScan.TypeAt(gx, gy);
                Log("tree failed at " + gx + "," + gy + ": ground=" + ground + " above=" + TileScan.TypeAt(gx, gy - 1) + " wall=" + t.wall + " liquid=" + t.liquid
                    + " fit=" + (ground >= 0 && WorldGen.IsTileTypeFitForTree((ushort)ground)) + " empty=" + WorldGen.EmptyTileCheck(gx - 2, gx + 2, gy - 14, gy - 1, 20));
            }
            catch (Exception e)
            {
                Log("tree failed at " + gx + "," + gy + " (diagnosis failed: " + e.Message + ")");
            }
        }

        public bool GrowAt(int gx, int gy, int biome)
        {
            int ground = GroundOf(biome);
            if (!stat.TryGetValue(ground, out TreeStat? st))
            {
                st = new TreeStat();
                stat[ground] = st;
            }
            if (st.Off)
                return false;
            Tile t = Main.tile[gx, gy];
            short prevHeader = t.sTileHeader;
            t.sTileHeader = TileScan.ActiveBit;
            t.type = (ushort)ground;
            Res.Tried++;
            st.Tries++;
            char won = '\0';
            if (Attempt(gx, gy, true))
            {
                won = 'A';
            }
            else if (st.Wins == 0 ? st.Tries <= 30 : (st.B + st.C) > 0)
            {
                if (AttemptVariant(gx, gy, false, ground))
                    won = 'B';
                else if (AttemptVariant(gx, gy, true, ground))
                    won = 'C';
            }
            if (won != '\0')
            {
                if (won == 'A')
                    st.A++;
                else if (won == 'B')
                    st.B++;
                else
                    st.C++;
                st.Wins++;
                st.Fails = 0;
                Res.Ok++;
                SquareFrame(gx, gy);
                t.invisibleBlock(true);
                return true;
            }
            if (diag < 3)
            {
                diag++;
                Diagnose(gx, gy);
            }
            t.sTileHeader = (short)(prevHeader & ~TileScan.ActiveBit);
            st.Fails++;
            if (st.Wins == 0 && st.Fails >= 120)
            {
                st.Off = true;
                Log("GrowTree never worked on ground type " + ground + ": skipping that ground.");
            }
            return false;
        }

        public void Summary()
        {
            foreach (KeyValuePair<int, TreeStat> g in stat)
                Log("trees on ground " + g.Key + ": " + g.Value.Wins + "/" + g.Value.Tries + " (direct " + g.Value.A + ", no walls " + g.Value.B + ", with support " + g.Value.C + ")");
            Log("GrowTree: " + Res.Ok + "/" + Res.Tried);
        }
    }

    static TreeResult PlantRealTrees(Fields F, Palette P, Point2 spawn, int spawnLX, int x0, int Wr, int H, int seed, GenerationProgress progress, double density, Mask? M, Spacing SP)
    {
        var ctx = new TreeContext(P);
        double d = Math.Min(3, Math.Max(0.3, density != 0 ? density : 1));
        int cell = d >= 2.2 ? 4 : d >= 1.2 ? 5 : 6;
        double scale = 0.55 + 0.45 * d;
        const int ox = Edge + 8, oy = Edge + 20;
        int ncx = Math.Max(1, (Wr - ox - Edge - 8) / cell), ncy = Math.Max(1, (H - oy - Edge - 8) / cell);
        int N = ncx * ncy, step = PermStep(N);
        Report(progress, "Terraria trees", 0.05);
        int skipped = 0, k = (int)(Hash2(N, 3, seed + 21) * N);
        for (int n = 0; n < N; n++, k = (int)(((long)k + step) % N))
        {
            int cx = k % ncx, cy = k / ncx;
            if (GetTreeCandidate(F, M, spawnLX, spawn, cx, cy, cell, ox, oy, seed, scale, out TreeCandidate c))
            {
                if (SpacingHit(SP, c.Px, c.Py, 3, 13) || SpacingHit(SP, c.Px, c.Py, 4, 5))
                    skipped++;
                else if (ctx.GrowAt(x0 + c.Px, c.Py, c.Biome))
                    SpacingAdd(SP, c.Px, c.Py);
            }
            if ((n & 4095) == 0)
                Report(progress, null, 0.05 + 0.9 * ((double)n / N));
        }
        Log("tree density " + d.ToString("0.0") + " (cell " + cell + "), " + skipped + " candidates without room");
        ctx.Summary();
        return ctx.Res;
    }

    sealed class CactusContext
    {
        readonly int sand, support;
        int ok, tried, howP, howG, howM, howB, diag;
        uint rs = 12345;
        string err = "";

        public int Ok => ok;

        public CactusContext(Palette P)
        {
            sand = P.TileSand;
            support = P.TileStone;
        }

        double Rnd()
        {
            unchecked
            {
                rs = rs * 1664525 + 1013904223;
            }
            return rs / 4294967296.0;
        }

        void Call(Action<int, int> f, string name, int x, int y)
        {
            try
            {
                f(x, y);
            }
            catch (Exception e)
            {
                if (tried <= 3)
                    Log("cactus: " + name + " failed: " + e);
                err = e.ToString();
            }
        }

        bool Manual(int gx, int gy)
        {
            int h = 2 + (int)Math.Floor(Rnd() * 4);
            int n = 0;
            for (int k = 1; k <= h; k++)
            {
                if (TileScan.TypeAt(gx, gy - k) >= 0)
                    break;
                Tile t = Main.tile[gx, gy - k];
                t.sTileHeader = TileScan.ActiveBit;
                t.type = TileID.Cactus;
                t.frameX = 0;
                t.frameY = 0;
                n++;
            }
            if (n == 0)
                return false;
            for (int k = n; k >= 1; k--)
                Call(WorldGen.CactusFrame, "CactusFrame", gx, gy - k);
            SquareFrame(gx, gy - 1);
            if (TileScan.TypeAt(gx, gy - 1) == TileID.Cactus)
                return true;
            for (int k = 1; k <= n; k++)
            {
                Tile t = Main.tile[gx, gy - k];
                t.sTileHeader = (short)(t.sTileHeader & ~TileScan.ActiveBit);
            }
            return false;
        }

        char Attempt(int gx, int gy)
        {
            Call(WorldGen.PlantCactus, "PlantCactus", gx, gy);
            if (TileScan.TypeAt(gx, gy - 1) == TileID.Cactus)
                return 'P';
            Call(WorldGen.GrowCactus, "GrowCactus", gx, gy);
            if (TileScan.TypeAt(gx, gy - 1) == TileID.Cactus)
            {
                for (int k = 0; k < 40; k++)
                {
                    int y = gy - 1 - (int)Math.Floor(Rnd() * 9);
                    if (TileScan.TypeAt(gx, y) == TileID.Cactus)
                        Call(WorldGen.GrowCactus, "GrowCactus", gx, y);
                }
                return 'G';
            }
            return Manual(gx, gy) ? 'M' : '\0';
        }

        char AttemptClear(int gx, int gy)
        {
            ushort[] saved = new ushort[5 * 12];
            int k = 0;
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -10; dy <= 1; dy++)
                {
                    Tile t = Main.tile[gx + dx, gy + dy];
                    saved[k++] = t.wall;
                    t.wall = 0;
                }
            }
            try
            {
                return Attempt(gx, gy);
            }
            finally
            {
                k = 0;
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dy = -10; dy <= 1; dy++)
                    {
                        Tile t = Main.tile[gx + dx, gy + dy];
                        t.wall = saved[k++];
                    }
                }
            }
        }

        public bool GrowAt(int gx, int gy)
        {
            if (TileScan.TypeAt(gx, gy) >= 0 || TileScan.TypeAt(gx, gy + 1) >= 0 || TileScan.TypeAt(gx, gy - 1) >= 0)
                return false;
            Tile t = Main.tile[gx, gy], s = Main.tile[gx, gy + 1];
            short hT = t.sTileHeader, hS = s.sTileHeader;
            t.sTileHeader = TileScan.ActiveBit;
            t.type = (ushort)sand;
            s.sTileHeader = TileScan.ActiveBit;
            s.type = (ushort)support;
            tried++;
            char how = Attempt(gx, gy);
            if (how == '\0' && AttemptClear(gx, gy) != '\0')
                how = 'B';
            if (how != '\0')
            {
                if (how == 'P')
                    howP++;
                else if (how == 'G')
                    howG++;
                else if (how == 'M')
                    howM++;
                else
                    howB++;
                ok++;
                t.invisibleBlock(true);
                s.invisibleBlock(true);
                SquareFrame(gx, gy - 1);
                return true;
            }
            if (diag < 3)
            {
                diag++;
                Tile above = Main.tile[gx, gy - 1];
                Log("cactus failed at " + gx + "," + gy + ": ground=" + TileScan.TypeAt(gx, gy) + " above=" + TileScan.TypeAt(gx, gy - 1) + " wall=" + above.wall);
            }
            t.sTileHeader = hT;
            s.sTileHeader = hS;
            return false;
        }

        public void Summary() =>
            Log("cacti: " + ok + "/" + tried + " (PlantCactus " + howP + ", GrowCactus " + howG + ", manual " + howM + ", no walls " + howB + ")" + (ok == 0 && err != "" ? " error: " + err : ""));
    }

    static int PlantCacti(Fields F, Palette P, int x0, int Wr, int H, int seed, GenerationProgress progress, double density, Mask? M)
    {
        var ctx = new CactusContext(P);
        double d = Math.Min(3, Math.Max(0.2, density != 0 ? density : 1));
        const int cell = 4, ox = Edge + 8, oy = Edge + 12;
        int ncx = Math.Max(1, (Wr - ox - Edge - 8) / cell), ncy = Math.Max(1, (H - oy - Edge - 8) / cell);
        int N = ncx * ncy, step = PermStep(N);
        Spacing SP = MakeSpacing(Wr, H);
        double sc = F.L.Sc;
        int k = (int)(Hash2(N, 5, seed + 22) * N);
        for (int n = 0; n < N; n++, k = (int)(((long)k + step) % N))
        {
            int cx = k % ncx, cy = k / ncx;
            int px = ox + cx * cell + (int)Math.Floor(Hash2(cx, cy, seed + 31) * cell);
            int py = oy + cy * cell + (int)Math.Floor(Hash2(cx, cy, seed + 32) * cell);
            double cl = Fbm(px / (34 * sc), py / (34 * sc), seed + 7100, 2);
            double p = (0.05 + 0.60 * Smooth(0.48, 0.70, cl)) * d;
            if (Hash2(cx, cy, seed + 33) > p)
                continue;
            if (BiomeAt(F, px, py) != DesertBiome)
                continue;
            if (Sample(F, F.HF, px, py) < ThreshBeach + 0.05)
                continue;
            if (M != null && (MaskGet(M, px, py) != 0 || MaskGet(M, px - 3, py) != 0 || MaskGet(M, px + 3, py) != 0 || MaskGet(M, px, py - 3) != 0 || MaskGet(M, px, py + 4) != 0))
                continue;
            if (BiomeAt(F, px - 5, py) != DesertBiome || BiomeAt(F, px + 5, py) != DesertBiome || BiomeAt(F, px, py - 5) != DesertBiome || BiomeAt(F, px, py + 5) != DesertBiome)
                continue;
            if (SpacingHit(SP, px, py, 2, 3))
                continue;
            if (ctx.GrowAt(x0 + px, py))
                SpacingAdd(SP, px, py);
            if ((n & 8191) == 0)
                Report(progress, null, (double)n / N);
        }
        ctx.Summary();
        return ctx.Ok;
    }

    static bool ActiveTile(in Tile t) => (t.sTileHeader & TileScan.ActiveBit) != 0;

    public static TreeResult AddTreesOpenWorld(double density)
    {
        Palette P = BuildPalette();
        int W = Main.maxTilesX, H = Main.maxTilesY;
        Dictionary<int, int> km = KindMap(P);
        var special = new HashSet<int>(P.Mix.Ids);
        special.UnionWith(P.Stone.Ids);
        var ctx = new TreeContext(P);
        double d = Math.Min(3, Math.Max(0.3, density != 0 ? density : 1));
        int cell = Math.Max(5, Round(9 / Math.Sqrt(d)));
        int seed = SeedFromWorld();
        int sx = Main.spawnTileX, sy = Main.spawnTileY;

        // Kinds 1, 3, 4 and evil have a biome. The grass biome's id is 0, which the JavaScript
        // version's check below read as "no biome", so a tree never starts on plain grass.
        static bool HasBiome(int kind, out int biome)
        {
            switch (kind)
            {
                case 1:
                    biome = GrassBiome;
                    return true;
                case 3:
                    biome = SnowBiome;
                    return true;
                case 4:
                    biome = JungleBiome;
                    return true;
                case KEvil:
                    biome = EvilBiome;
                    return true;
                default:
                    biome = 0;
                    return false;
            }
        }

        for (int cy = 4; cy * cell < H - Edge - 6; cy++)
        {
            for (int cx = 1; cx * cell < W - Edge - 4; cx++)
            {
                int px = cx * cell + 1 + (int)Math.Floor(Hash2(cx, cy, seed + 11) * (cell - 2));
                int py = cy * cell + 1 + (int)Math.Floor(Hash2(cx, cy, seed + 12) * (cell - 2));
                if (px < Edge + 4 || px > W - Edge - 4)
                    continue;
                double forest = VNoise(px / 55.0, py / 55.0, seed + 5000);
                if (Hash2(cx, cy, seed + 13) > 0.55 * d * Smooth(0.36, 0.60, forest))
                    continue;
                if ((px - sx) * (px - sx) + (py - sy) * (py - sy) < ClearRadius * ClearRadius * 2.2)
                    continue;
                Tile t = Main.tile[px, py];
                int w = t.wall;
                if (ActiveTile(t) || special.Contains(w))
                    continue;
                km.TryGetValue(w, out int kind);
                if (!HasBiome(kind, out int biome))
                    continue;
                bool free = true;
                for (int dy = -14; dy <= 1 && free; dy++)
                {
                    for (int dx = -2; dx <= 2; dx += 2)
                    {
                        Tile o = Main.tile[px + dx, py + dy];
                        if (dy <= 0 && ActiveTile(o))
                        {
                            free = false;
                            break;
                        }
                        if (dy == 0 && dx == 0)
                        {
                            km.TryGetValue(o.wall, out int centerKind);
                            if (!HasBiome(centerKind, out int centerBiome) || centerBiome == 0)
                            {
                                free = false;
                                break;
                            }
                        }
                    }
                }
                if (!free)
                    continue;
                ctx.GrowAt(px, py, biome);
            }
        }
        Log("addTrees: density " + d.ToString("0.0") + ", " + ctx.Res.Ok + " new trees.");
        ctx.Summary();
        return ctx.Res;
    }

    static DecorResult DecorateWorld(Palette P, DecorOpts opts)
    {
        int W = Main.maxTilesX, H = Main.maxTilesY;
        int x0 = Math.Max(opts.X0, 0), x1 = Math.Min(opts.X1 != 0 ? opts.X1 : W, W);
        double dens = Math.Min(4, Math.Max(0.1, opts.Density != 0 ? opts.Density : 1));
        int seed = opts.Seed ?? SeedFromWorld();
        Dictionary<int, int> km = KindMap(P);
        var edgeIds = new HashSet<int>(P.Mix.Ids);
        HashSet<int> stoneIds = P.Stone.Set;
        Point2 spawn = opts.Spawn ?? new Point2 { X = Main.spawnTileX, Y = Main.spawnTileY };
        const int plant = TileID.Plants;
        int grassT = P.TileGrass;
        var result = new DecorResult();

        int Pure(int x, int y)
        {
            Tile t = Main.tile[x, y];
            if (ActiveTile(t))
                return 0;
            int w = t.wall;
            if (w == 0 || edgeIds.Contains(w) || stoneIds.Contains(w))
                return 0;
            km.TryGetValue(w, out int k);
            return k >= 1 && k <= 4 ? k : 0;
        }

        bool NearSpawn(int x, int y, int r) => (x - spawn.X) * (x - spawn.X) + (y - spawn.Y) * (y - spawn.Y) < r * r;

        if (opts.Clear)
        {
            int[] baseOf = { 0, P.WallGrass, P.WallSand, P.WallSnow, P.WallJungle, 0, 0, P.WallShallow };
            for (int y = Edge; y < H - Edge; y++)
            {
                for (int x = x0 + Edge; x < x1 - Edge; x++)
                {
                    Tile t = Main.tile[x, y];
                    if (stoneIds.Contains(t.wall))
                    {
                        km.TryGetValue(t.wall, out int k);
                        int plain = baseOf[k];
                        t.wall = (ushort)(plain != 0 ? plain : P.WallGrass);
                        result.Cleared++;
                    }
                    if (ActiveTile(t) && t.type == plant)
                    {
                        t.sTileHeader = (short)(t.sTileHeader & ~TileScan.ActiveBit);
                        result.Cleared++;
                    }
                }
            }
        }

        int[] neighborX = { 1, -1, 0, 0 }, neighborY = { 0, 0, 1, -1 };

        if (P.Stone.Ok > 0 && opts.Rocks)
        {
            for (int y = Edge + 3; y < H - Edge - 3; y += 3)
            {
                for (int x = x0 + Edge + 3; x < x1 - Edge - 3; x += 3)
                {
                    double roll = Hash2(x, y, seed + 101);
                    double clump = VNoise(x / 16.0, y / 16.0, seed + 151);
                    if (roll > (0.03 + 0.24 * Smooth(0.55, 0.80, clump)) * dens)
                        continue;
                    int gx = x + (int)Math.Floor(Hash2(x, y, seed + 102) * 3), gy = y + (int)Math.Floor(Hash2(x, y, seed + 103) * 3);
                    if (NearSpawn(gx, gy, 3))
                        continue;
                    int k = Pure(gx, gy);
                    if (k == 0)
                        continue;
                    if (Pure(gx + 1, gy) != k || Pure(gx - 1, gy) != k || Pure(gx, gy + 1) != k || Pure(gx, gy - 1) != k)
                        continue;
                    if (!P.Stone.ByKind.TryGetValue(k, out List<int>? list) || list.Count == 0)
                        continue;
                    Tile t = Main.tile[gx, gy];
                    t.wall = (ushort)list[(int)Math.Floor(Hash2(gx, gy, seed + 104) * list.Count)];
                    result.Rocks++;
                    if (Hash2(gx, gy, seed + 105) < 0.35)
                    {
                        int q = (int)Math.Floor(Hash2(gx, gy, seed + 106) * 4);
                        int ox = gx + neighborX[q], oy = gy + neighborY[q];
                        if (Pure(ox, oy) == k)
                        {
                            Tile other = Main.tile[ox, oy];
                            other.wall = (ushort)list[(int)Math.Floor(Hash2(ox, oy, seed + 107) * list.Count)];
                            result.Rocks++;
                        }
                    }
                }
            }
        }

        if (P.Stone.ByKind.TryGetValue(KShallow, out List<int>? wlist) && wlist.Count > 0 && opts.WaterRocks)
        {
            int sh = P.WallShallow;
            bool Shallow(int x, int y)
            {
                Tile t = Main.tile[x, y];
                return !ActiveTile(t) && t.wall == sh;
            }
            bool InWater(int x, int y) => Shallow(x, y) && (opts.Water == null || MaskGet(opts.Water, x - x0, y) == 3);
            bool OpenEx(int x, int y, int ex, int ey)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (x + dx == ex && y + dy == ey)
                            continue;
                        if (!Shallow(x + dx, y + dy))
                            return false;
                    }
                }
                return true;
            }
            bool Open(int x, int y, int r)
            {
                for (int dy = -r; dy <= r; dy++)
                {
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (!Shallow(x + dx, y + dy))
                            return false;
                    }
                }
                return true;
            }
            for (int y = Edge + 3; y < H - Edge - 3; y += 2)
            {
                for (int x = x0 + Edge + 3; x < x1 - Edge - 3; x += 2)
                {
                    double roll = Hash2(x, y, seed + 301);
                    double clump = VNoise(x / 10.0, y / 10.0, seed + 351);
                    if (roll > (0.10 + 0.34 * Smooth(0.45, 0.75, clump)) * dens)
                        continue;
                    int gx = x + (Hash2(x, y, seed + 302) < 0.5 ? 0 : 1), gy = y + (Hash2(x, y, seed + 303) < 0.5 ? 0 : 1);
                    if (!InWater(gx, gy) || !Open(gx, gy, opts.Water != null ? 1 : 2))
                        continue;
                    Tile t = Main.tile[gx, gy];
                    t.wall = (ushort)wlist[(int)Math.Floor(Hash2(gx, gy, seed + 304) * wlist.Count)];
                    result.WaterRocks++;
                    if (Hash2(gx, gy, seed + 305) < 0.3)
                    {
                        int q = (int)Math.Floor(Hash2(gx, gy, seed + 306) * 4);
                        int ox = gx + neighborX[q], oy = gy + neighborY[q];
                        if (InWater(ox, oy) && OpenEx(ox, oy, gx, gy))
                        {
                            Tile other = Main.tile[ox, oy];
                            other.wall = (ushort)wlist[(int)Math.Floor(Hash2(ox, oy, seed + 307) * wlist.Count)];
                            result.WaterRocks++;
                        }
                    }
                }
            }
        }

        if (opts.Plants)
        {
            for (int y = Edge + 3; y < H - Edge - 3; y += 2)
            {
                for (int x = x0 + Edge + 3; x < x1 - Edge - 3; x += 2)
                {
                    double roll = Hash2(x, y, seed + 201);
                    double clump = VNoise(x / 8.0, y / 8.0, seed + 251);
                    if (roll > (0.04 + 0.5 * Smooth(0.5, 0.75, clump)) * 0.6 * dens)
                        continue;
                    int gx = x + (Hash2(x, y, seed + 202) < 0.5 ? 0 : 1), gy = y + (Hash2(x, y, seed + 203) < 0.5 ? 0 : 1);
                    if (NearSpawn(gx, gy, 2))
                        continue;
                    int k = Pure(gx, gy);
                    if (k != 1 && k != 4)
                        continue;
                    Tile below = Main.tile[gx, gy + 1];
                    if (ActiveTile(below))
                        continue;
                    int style = PlantStyles[(int)Math.Floor(Hash2(gx, gy, seed + 204) * PlantStyles.Length)];
                    short h0 = below.sTileHeader;
                    ushort ty0 = below.type;
                    below.sTileHeader = (short)(h0 | TileScan.ActiveBit);
                    below.type = (ushort)grassT;
                    bool ok = false;
                    try
                    {
                        try
                        {
                            WorldGen.PlaceTile(gx, gy, plant, true, true, -1, style);
                        }
                        catch (Exception)
                        {
                        }
                        ok = TileScan.TypeAt(gx, gy) == plant;
                    }
                    finally
                    {
                        below.sTileHeader = h0;
                        below.type = ty0;
                    }
                    if (!ok)
                    {
                        Tile t = Main.tile[gx, gy];
                        t.sTileHeader = (short)(t.sTileHeader | TileScan.ActiveBit);
                        t.type = plant;
                        t.frameX = (short)(style * 18);
                        t.frameY = 0;
                        ok = TileScan.TypeAt(gx, gy) == plant;
                        if (ok)
                            result.Direct++;
                    }
                    if (ok)
                        result.Plants++;
                    else if (++result.PlantFail <= 3)
                        Log("grass: could not place tile " + plant + " at " + gx + "," + gy);
                }
            }
        }
        Log("decoration: " + result.Rocks + " rocks, " + result.WaterRocks + " rocks in the water, " + result.Plants + " tufts (" + result.Direct + " written directly, " + result.PlantFail + " failures)"
            + (result.Cleared > 0 ? ", " + result.Cleared + " old ones erased" : "") + "; density " + dens.ToString("0.0"));
        return result;
    }

    public static DecorResult DecorateOpenWorld(double density)
    {
        Palette P = BuildPalette();
        DecorResult r = DecorateWorld(P, new DecorOpts { Clear = true, Density = density });
        ApplyFloorLight(P, true);
        return r;
    }

    public static DecorResult UndecorateOpenWorld() => DecorateWorld(BuildPalette(), new DecorOpts { Clear = true, Rocks = false, Plants = false });

    public static DeepResult HideDeepOpenWorld()
    {
        Palette P = BuildPalette();
        int deep = P.WallDeep, glass = P.TileGlass;
        var res = new DeepResult();
        if (deep <= 0)
        {
            Log("deep sea: TopDownDeep wall unavailable, nothing to do.");
            return res;
        }
        var mark = new TypeMark();
        mark.Set(deep);
        int W = Main.maxTilesX, H = Main.maxTilesY;
        var found = new List<int>();
        const int step = 64;
        for (int y0 = 0; y0 < H; y0 += step)
        {
            int y1 = Math.Min(y0 + step, H) - 1;
            TileScan.FindWalls(mark, 0, y0, W - 1, y1, found);
            for (int k = 0; k < found.Count; k += 3)
            {
                int x = found[k], y = found[k + 1];
                res.Deep++;
                if (TileScan.TypeAt(x, y) != glass)
                    continue;
                try
                {
                    Tile t = Main.tile[x, y];
                    t.invisibleBlock(true);
                    res.Hidden++;
                }
                catch (Exception)
                {
                    res.Failed++;
                }
            }
        }
        Log("deep sea: " + res.Deep + " deep water tiles, " + res.Hidden + " glass blocks hidden" + (res.Failed > 0 ? ", " + res.Failed + " failed" : "") + ".");
        return res;
    }

    public static DeepResult OpenDeepOpenWorld()
    {
        Palette P = BuildPalette();
        int deep = P.WallDeep, glass = P.TileGlass;
        var res = new DeepResult();
        if (deep <= 0)
        {
            Log("deep sea: TopDownDeep wall unavailable, nothing to do.");
            return res;
        }
        var mark = new TypeMark();
        mark.Set(deep);
        int W = Main.maxTilesX, H = Main.maxTilesY;
        var found = new List<int>();
        var cand = new List<int>();
        const int step = 64;
        int minX = 1000000000, maxX = -1;
        for (int y0 = 0; y0 < H; y0 += step)
        {
            int y1 = Math.Min(y0 + step, H) - 1;
            TileScan.FindWalls(mark, 0, y0, W - 1, y1, found);
            for (int k = 0; k < found.Count; k += 3)
            {
                int x = found[k], y = found[k + 1];
                res.Deep++;
                if (x < minX)
                    minX = x;
                if (x > maxX)
                    maxX = x;
                if (TileScan.TypeAt(x, y) == glass)
                {
                    cand.Add(x);
                    cand.Add(y);
                }
            }
        }
        for (int k = 0; k < cand.Count; k += 2)
        {
            int x = cand[k], y = cand[k + 1];
            if (y < Edge || y >= H - Edge || x < minX + Edge || x > maxX - Edge)
            {
                res.Kept++;
                continue;
            }
            try
            {
                Tile t = Main.tile[x, y];
                t.sTileHeader = (short)(t.sTileHeader & ~TileScan.ActiveBit);
                t.type = 0;
                res.Removed++;
            }
            catch (Exception)
            {
                res.Failed++;
            }
        }
        Log("deep sea: " + res.Deep + " deep water tiles, " + res.Removed + " glass blocks removed, " + res.Kept + " kept at the world's edge" + (res.Failed > 0 ? ", " + res.Failed + " failed" : "") + ".");
        return res;
    }
}
