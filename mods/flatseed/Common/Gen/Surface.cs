using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.WorldBuilding;
using static FlatSeedMod.Common.Gen.Tiles;

namespace FlatSeedMod.Common.Gen;

// What the player chose when generation started.
public sealed record FlatSettings(int SurfaceHeight, bool FillChasms, bool SmoothBeaches);

public static class Surface
{
    const int OceanMargin = 200;
    const int RampWidth = 100;
    const int MinSurface = 240;
    const double MaxSurface = 0.45;
    const int Topsoil = 6;

    static int JsRound(double value) => (int)Math.Floor(value + 0.5);

    public static int FlatY(FlatSettings config)
    {
        int desired = JsRound(Main.maxTilesY * config.SurfaceHeight / 100.0);
        int max = (int)Math.Floor(Main.maxTilesY * MaxSurface);
        return Math.Max(MinSurface, Math.Min(desired, max));
    }

    static (int Left, int Right) OceanLimits()
    {
        int width = Main.maxTilesX;
        int left = GenVars.leftBeachEnd - OceanMargin;
        int right = GenVars.rightBeachStart + OceanMargin;
        if (left <= 0)
            left = GenVars.beachBordersWidth - 100;
        if (right >= width)
            right = width - GenVars.beachBordersWidth + 100;
        return (left, right);
    }

    static bool Ocean(int x, (int Left, int Right) limits) => x <= limits.Left || x >= limits.Right;

    static bool Ramp(int x, (int Left, int Right) limits, FlatSettings config)
    {
        if (!config.SmoothBeaches)
            return false;
        return (x > limits.Left && x <= limits.Left + RampWidth) || (x >= limits.Right - RampWidth && x < limits.Right);
    }

    static int SurfaceY(int x, int limit)
    {
        for (int y = 0; y < limit; y++)
        {
            if (Solid(At(x, y)))
                return y;
        }
        return -1;
    }

    static int RampTarget(int x, int flatY, int limit, (int Left, int Right) limits, FlatSettings config)
    {
        if (!Ramp(x, limits, config))
            return flatY;
        double t;
        int edge;
        if (x <= limits.Left + RampWidth)
        {
            t = (x - limits.Left) / (double)RampWidth;
            edge = limits.Left;
        }
        else
        {
            t = (limits.Right - x) / (double)RampWidth;
            edge = limits.Right;
        }
        int ocean = SurfaceY(edge, limit);
        if (ocean < 0)
            return flatY;
        return JsRound(ocean + (flatY - ocean) * t);
    }

    static void Anchor(int flatY)
    {
        double distance = Main.rockLayer - Main.worldSurface;
        GenVars.worldSurfaceLow = flatY;
        GenVars.worldSurface = flatY;
        GenVars.worldSurfaceHigh = flatY;
        Main.worldSurface = flatY + 25;
        double rock = Math.Min(Main.worldSurface + distance, Main.maxTilesY - 340);
        Main.rockLayer = rock;
        GenVars.rockLayerLow = rock;
        GenVars.rockLayer = rock;
        GenVars.rockLayerHigh = rock;
    }

    public static void FlattenSurface(GenerationProgress progress, FlatSettings config)
    {
        int flatY = FlatY(config);
        int limit = Math.Min((int)Math.Floor(GenVars.worldSurfaceHigh) + 300, Main.maxTilesY - 100);
        (int Left, int Right) limits = OceanLimits();
        int width = Main.maxTilesX;
        for (int x = 0; x < width; x++)
        {
            if (x % 64 == 0)
                Progress(progress, x / (double)width);
            if (Ocean(x, limits))
                continue;
            int target = RampTarget(x, flatY, limit, limits, config);
            int surface = SurfaceY(x, limit);
            if (surface < 0 || surface == target)
                continue;
            if (surface < target)
            {
                int top = At(x, surface).type;
                for (int y = surface; y < target; y++)
                    Clear(At(x, y));
                if (FillType(top) != 1)
                {
                    for (int y = target; y < target + Topsoil; y++)
                    {
                        Tile tile = At(x, y);
                        if (Solid(tile) && tile.type == 1)
                            Place(tile, FillType(top));
                    }
                }
                continue;
            }
            int type = At(x, surface).type;
            int fill = FillType(type);
            for (int y = target; y < surface; y++)
                Place(At(x, y), y == target ? type : fill);
        }
        Anchor(flatY);
    }

    static void TrimColumn(int x, int flatY, Func<int, bool> cuttable, List<(int X, int Y)>? islands = null)
    {
        List<(int Top, int Bottom)>? spans = islands == null ? null : Islands.IslandSpans(islands, x);
        for (int y = 0; y < flatY; y++)
        {
            bool skipped = false;
            if (spans != null)
            {
                foreach ((int Top, int Bottom) span in spans)
                {
                    if (y >= span.Top && y <= span.Bottom)
                    {
                        y = span.Bottom;
                        skipped = true;
                        break;
                    }
                }
            }
            if (skipped)
                continue;
            Tile tile = At(x, y);
            if (!Solid(tile))
                continue;
            if (Furniture(tile) || !cuttable(tile.type))
                return;
            Clear(tile);
        }
    }

    static void FillColumn(int x, int flatY, int depth)
    {
        int bottom = Math.Min(flatY + depth, Main.maxTilesY);
        for (int y = flatY; y < bottom; y++)
        {
            Tile tile = At(x, y);
            if (!Solid(tile))
                continue;
            if (y == flatY)
                return;
            int type = tile.type;
            int fill = FillType(type);
            for (int k = flatY; k < y; k++)
            {
                Tile t = At(x, k);
                if (!Solid(t))
                    Place(t, k == flatY ? type : fill);
            }
            return;
        }
    }

    public static void FlattenDesert(GenerationProgress progress, FlatSettings config)
    {
        Rectangle area = GenVars.UndergroundDesertLocation;
        if (area.Width <= 0 || area.Height <= 0)
            return;
        int flatY = FlatY(config);
        (int Left, int Right) limits = OceanLimits();
        int start = Math.Max(area.X - 100, 0);
        int end = Math.Min(area.X + area.Width + 100, Main.maxTilesX);
        for (int x = start; x < end; x++)
        {
            if ((x - start) % 32 == 0)
                Progress(progress, (x - start) / (double)(end - start));
            if (Ocean(x, limits) || Ramp(x, limits, config))
                continue;
            TrimColumn(x, flatY, Terrain);
            FillColumn(x, flatY, 40);
        }
    }

    public static void TrimSurface(GenerationProgress progress, FlatSettings config, List<(int X, int Y)> islands)
    {
        int flatY = FlatY(config);
        (int Left, int Right) limits = OceanLimits();
        int width = Main.maxTilesX;
        int depth = Math.Max(0, (int)Math.Floor(Main.rockLayer) + 100 - flatY);
        for (int x = 0; x < width; x++)
        {
            if (x % 64 == 0)
                Progress(progress, x / (double)width);
            if (Ocean(x, limits) || Ramp(x, limits, config))
                continue;
            TrimColumn(x, flatY, Terrain, islands);
            if (config.FillChasms)
                FillColumn(x, flatY, depth);
        }
    }
}
