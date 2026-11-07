using System.Collections.Generic;
using Terraria;
using Terraria.WorldBuilding;

namespace FlatSeedMod.Common.Gen;

public static class Tiles
{
    public static Tile At(int x, int y) => Main.tile[x, y];

    public static bool Solid(Tile tile) => tile.active();

    public static void Clear(Tile tile) => tile.ClearEverything();

    public static void Place(Tile tile, int type)
    {
        tile.ResetToType((ushort)type);
        tile.frameX = -1;
        tile.frameY = -1;
    }

    public static bool Furniture(Tile tile) => tile.frameX >= 0 || tile.frameY >= 0;

    public static void Progress(GenerationProgress progress, double value) => progress.Set(value);

    static readonly HashSet<int> TerrainTypes = new()
    {
        0, 1, 2, 23, 40, 53, 57, 59, 60, 70, 109, 112, 116, 123, 147, 161, 163, 164, 199, 200, 234, 396, 397, 398, 399, 400, 401, 402, 403,
    };

    public static bool Terrain(int type) => TerrainTypes.Contains(type);

    public static int FillType(int type) => type switch
    {
        1 or 2 or 23 or 109 or 199 => 0,
        60 or 70 => 59,
        _ => type,
    };
}
