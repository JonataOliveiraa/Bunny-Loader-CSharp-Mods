using System.Collections.Generic;
using Terraria;

namespace topdownmode.Common.Utils;

// A set of tile or wall types, the C# side of the JavaScript bridge's bl.hookMarks.
public sealed class TypeMark
{
    bool[] flags = new bool[1024];

    public void Set(int type)
    {
        if (type < 0)
            return;
        if (type >= flags.Length)
            System.Array.Resize(ref flags, type * 2);
        flags[type] = true;
    }

    public bool Has(int type) => type >= 0 && type < flags.Length && flags[type];
}

public struct TileRun
{
    public int I, Top, Bot, N;
}

// Reads the world as bl.tiles and bl.walls did: rectangles are clamped to the world, and
// results are flat lists, sorted by column and then by row.
public static class TileScan
{
    public const short ActiveBit = 0x20;

    // The type of the active tile, or -1 if there is none.
    public static int TypeAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY)
            return -1;
        Tile tile = Main.tile[x, y];
        return (tile.sTileHeader & ActiveBit) != 0 ? tile.type : -1;
    }

    public static int WallAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY)
            return 0;
        Tile tile = Main.tile[x, y];
        return tile.wall;
    }

    // [i, j, ...] for each active tile of a marked type.
    public static void FindTiles(TypeMark mark, int x0, int y0, int x1, int y1, List<int> found)
    {
        found.Clear();
        if (x0 < 0) x0 = 0;
        if (y0 < 0) y0 = 0;
        if (x1 >= Main.maxTilesX) x1 = Main.maxTilesX - 1;
        if (y1 >= Main.maxTilesY) y1 = Main.maxTilesY - 1;
        for (int i = x0; i <= x1; i++)
        {
            for (int j = y0; j <= y1; j++)
            {
                Tile tile = Main.tile[i, j];
                if ((tile.sTileHeader & ActiveBit) != 0 && mark.Has(tile.type))
                {
                    found.Add(i);
                    found.Add(j);
                }
            }
        }
    }

    // [i, j, wall, ...] for each wall of a marked type.
    public static void FindWalls(TypeMark mark, int x0, int y0, int x1, int y1, List<int> found)
    {
        found.Clear();
        if (x0 < 0) x0 = 0;
        if (y0 < 0) y0 = 0;
        if (x1 >= Main.maxTilesX) x1 = Main.maxTilesX - 1;
        if (y1 >= Main.maxTilesY) y1 = Main.maxTilesY - 1;
        for (int i = x0; i <= x1; i++)
        {
            for (int j = y0; j <= y1; j++)
            {
                Tile tile = Main.tile[i, j];
                int wall = tile.wall;
                if (mark.Has(wall))
                {
                    found.Add(i);
                    found.Add(j);
                    found.Add(wall);
                }
            }
        }
    }

    // The unbroken vertical runs in a FindTiles result.
    public static void Runs(List<int> found, List<TileRun> runs)
    {
        runs.Clear();
        for (int k = 0; k < found.Count;)
        {
            int i = found[k], top = found[k + 1], bot = top;
            k += 2;
            while (k < found.Count && found[k] == i && found[k + 1] == bot + 1)
            {
                bot++;
                k += 2;
            }
            runs.Add(new TileRun { I = i, Top = top, Bot = bot, N = bot - top + 1 });
        }
    }

    // Tile positions as dictionary keys, for the JavaScript "i,j" strings.
    public static long Key(int i, int j) => ((long)i << 32) | (uint)j;
}
