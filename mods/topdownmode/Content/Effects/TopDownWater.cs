using System;
using System.Collections.Generic;
using BunnyLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using topdownmode.Common.Configs;
using topdownmode.Common.Systems;
using topdownmode.Common.Utils;
using topdownmode.Content.Walls;

namespace topdownmode.Content.Effects;

// Moving water, drawn over the water floor. The shader cannot read the world, so each row of
// tiles on screen goes to it as up to four runs of water, packed as start * 512 + end.
public class TopDownWater : ModScreenEffect
{
    const int Rows = 80;
    const int MaxC = 255;
    const int HalfRows = 40;
    const int RunCount = 4;
    const int Refresh = 20;

    static readonly TypeMark markAll = new();
    static bool ready, clipped, logged;
    static int shallow, deep, tileCount, rebuilds, errors, drawErrors, kills;
    static string lastErr = "";

    static readonly byte[] grid = new byte[Rows * (MaxC + 1)];
    static readonly float[] rowsA = new float[HalfRows * RunCount], rowsB = new float[HalfRows * RunCount];
    static readonly float[] swim = new float[16];
    static readonly ScreenGraphics.UniformArray rowsAUniform = new(4, rowsA), rowsBUniform = new(4, rowsB), swimUniform = new(4, swim);
    static readonly List<int> found = new();
    static readonly List<(int Start, int End)> runs = new();

    static int cacheX = int.MinValue, cacheY = int.MinValue, cacheAge = 1000000000, cacheCols, cacheRows;

    bool any, good;

    public static string Status() =>
        "water: walls shallow=" + shallow + " deep=" + deep + " tiles on screen=" + tileCount + " masks=" + rebuilds + " errors=" + errors + (lastErr != "" ? " last error: " + lastErr : "");

    static bool Setup()
    {
        if (ready)
            return shallow > 0 || deep > 0;
        shallow = ModContent.WallType<TopDownShallow>();
        deep = ModContent.WallType<TopDownDeep>();
        markAll.Set(ModContent.WallType<TopDownShallowStone1>());
        markAll.Set(ModContent.WallType<TopDownShallowStone2>());
        markAll.Set(ModContent.WallType<TopDownShallowStone3>());
        if (shallow > 0)
            markAll.Set(shallow);
        if (deep > 0)
            markAll.Set(deep);
        ready = true;
        TopDownMode.Log("moving water: walls shallow=" + shallow + " deep=" + deep);
        return shallow > 0 || deep > 0;
    }

    static int Rebuild(int tx0, int ty0, int cols, int rows)
    {
        Array.Clear(grid);
        int n = 0;
        TileScan.FindWalls(markAll, tx0, ty0, tx0 + cols - 1, ty0 + rows - 1, found);
        for (int k = 0; k < found.Count; k += 3)
        {
            int c = found[k] - tx0, r = found[k + 1] - ty0;
            if (c < 0 || c >= cols || r < 0 || r >= rows)
                continue;
            grid[r * (MaxC + 1) + c] = 1;
            n++;
        }
        if (rebuilds == 0 || rebuilds == 40)
            TopDownMode.Log("water: window " + cols + "x" + rows + " from " + tx0 + "," + ty0 + ", " + n + " water tiles");
        Array.Clear(rowsA);
        Array.Clear(rowsB);
        for (int r = 0; r < rows; r++)
        {
            int rowBase = r * (MaxC + 1);
            runs.Clear();
            int c = 0;
            while (c < cols)
            {
                if (grid[rowBase + c] != 0)
                {
                    int s = c;
                    while (c < cols && grid[rowBase + c] != 0)
                        c++;
                    runs.Add((s, c));
                }
                else
                {
                    c++;
                }
            }
            while (runs.Count > RunCount)
            {
                int best = 0, gap = 1000000000;
                for (int i = 0; i + 1 < runs.Count; i++)
                {
                    int g = runs[i + 1].Start - runs[i].End;
                    if (g < gap)
                    {
                        gap = g;
                        best = i;
                    }
                }
                runs[best] = (runs[best].Start, runs[best + 1].End);
                runs.RemoveAt(best + 1);
            }
            float[] target = r < HalfRows ? rowsA : rowsB;
            int offset = (r < HalfRows ? r : r - HalfRows) * RunCount;
            for (int i = 0; i < runs.Count; i++)
                target[offset + i] = runs[i].Start * 512 + runs[i].End;
        }
        rebuilds++;
        return n;
    }

    public override void SetDefaults()
    {
        Fullscreen = true;
        Center = null;
        Key = "topdown:water";
    }

    public override void Update()
    {
        any = false;
        try
        {
            if (Main.gameMenu || !Setup())
                return;
            if (!TopDownWorldSystem.IsTopDown)
                return;
            TopDownConfig c = TopDownConfig.Instance;
            if (!c.WaterShimmer)
                return;

            Vector2 tl = ScreenUtils.Space.UVToWorld(new Vector2(0, 1));
            Vector2 br = ScreenUtils.Space.UVToWorld(new Vector2(1, 0));
            double sizeX = (double)br.X - tl.X, sizeY = (double)br.Y - tl.Y;
            if (!(sizeX > 8 && sizeY > 8))
            {
                lastErr = "invalid visible area: " + sizeX + "x" + sizeY;
                return;
            }
            if (!logged)
            {
                logged = true;
                Vector2 space = ScreenUtils.Space.Size;
                TopDownMode.Log("water: Space.Size=" + space.X + "x" + space.Y + " | visible area=" + sizeX.ToString("0.0") + "x" + sizeY.ToString("0.0")
                    + " | top left=" + tl.X.ToString("0.0") + "," + tl.Y.ToString("0.0") + " | game screen=" + Main.screenWidth + "x" + Main.screenHeight);
            }
            int tx0 = (int)Math.Floor(tl.X / 16.0) - 1, ty0 = (int)Math.Floor(tl.Y / 16.0) - 1;
            int wantCols = (int)Math.Ceiling(sizeX / 16) + 3, wantRows = (int)Math.Ceiling(sizeY / 16) + 3;
            int cols = Math.Min(wantCols, MaxC), rows = Math.Min(wantRows, Rows);
            if ((wantCols > cols || wantRows > rows) && !clipped)
            {
                clipped = true;
                TopDownMode.Log("water: the screen (" + wantCols + "x" + wantRows + " tiles) goes past the largest window (" + MaxC + "x" + Rows + ")");
            }

            if (tx0 != cacheX || ty0 != cacheY || cols != cacheCols || rows != cacheRows || ++cacheAge >= Refresh)
            {
                tileCount = Rebuild(tx0, ty0, cols, rows);
                cacheX = tx0;
                cacheY = ty0;
                cacheCols = cols;
                cacheRows = rows;
                cacheAge = 0;
            }
            if (tileCount == 0)
                return;

            Uniforms["uRowsA"] = rowsAUniform;
            Uniforms["uRowsB"] = rowsBUniform;
            Uniforms["uRel0"] = new Vector2((float)((double)tl.X - tx0 * 16), (float)((double)tl.Y - ty0 * 16));
            Uniforms["uAbs0"] = tl;
            Uniforms["uWorldSize"] = new Vector2((float)sizeX, (float)sizeY);
            Uniforms["uStrength"] = c.WaterShimmerStrength;
            Uniforms["uSpeed"] = 1f;
            Uniforms["uDeepCaustic"] = 0f;
            Uniforms["uDay"] = c.DayNight ? (float)TopDownWorldSystem.Daylight() : 1f;
            Array.Clear(swim);
            List<TopDownTreeDepth.Swimmer> list = TopDownTreeDepth.Swimmers;
            for (int k = 0; k < 4 && k < list.Count; k++)
            {
                swim[k * 4] = (float)list[k].X;
                swim[k * 4 + 1] = (float)list[k].Y;
                swim[k * 4 + 2] = (float)list[k].Hw;
                swim[k * 4 + 3] = 1;
            }
            Uniforms["uSwim"] = swimUniform;
            any = true;
            good = true;
        }
        catch (Exception e)
        {
            errors++;
            lastErr = e.ToString();
            if (errors <= 3)
                TopDownMode.Log("moving water (error): " + e);
            any = good;
        }
    }

    public override void OnKill()
    {
        kills++;
        if (kills <= 5)
            TopDownMode.Log("water: effect ended (" + kills + ")" + (lastErr != "" ? " last error: " + lastErr : ""));
    }

    public override void Draw(ScreenCommands cmd)
    {
        try
        {
            base.Draw(cmd);
        }
        catch (Exception e)
        {
            drawErrors++;
            lastErr = "draw: " + e;
            if (drawErrors <= 3)
                TopDownMode.Log("water (draw error): " + e);
        }
    }

    public override bool ShouldDraw() => any;
}
