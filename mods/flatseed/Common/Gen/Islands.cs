using System.Collections.Generic;
using Terraria.WorldBuilding;

namespace FlatSeedMod.Common.Gen;

public static class Islands
{
    const int HalfWidth = 110;
    const int Above = 50;
    const int Below = 45;

    public static List<(int X, int Y)> All()
    {
        var list = new List<(int X, int Y)>();
        for (int i = 0; i < GenVars.numIslandHouses; i++)
            list.Add((GenVars.floatingIslandHouseX[i], GenVars.floatingIslandHouseY[i]));
        return list;
    }

    public static List<(int Top, int Bottom)> IslandSpans(List<(int X, int Y)> islands, int x)
    {
        var spans = new List<(int Top, int Bottom)>();
        foreach ((int X, int Y) island in islands)
        {
            if (x >= island.X - HalfWidth && x <= island.X + HalfWidth)
                spans.Add((island.Y - Above, island.Y + Below));
        }
        return spans;
    }
}
