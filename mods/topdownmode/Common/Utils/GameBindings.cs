using System;
using Terraria;
using Terraria.Map;

namespace topdownmode.Common.Utils;

public static class GameBindings
{
    public static void MapClear() => Map().Clear();

    public static void MapLoad() => Map().Load();

    static WorldMap Map()
    {
        WorldMap map = Main.Map;
        if (map.IsNull)
            throw new InvalidOperationException("Main.Map is null");
        return map;
    }
}
