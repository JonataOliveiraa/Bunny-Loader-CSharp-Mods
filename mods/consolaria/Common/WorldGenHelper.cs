using BunnyLoader.Interop;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.WorldBuilding;

namespace Consolaria.Common {
    public static class WorldGenHelper {
        public static bool TryStructureLocation(int[] tileType, int width, int height, ref int i, ref int j) {
            int minValue = int.MaxValue;
            int maxValue = int.MinValue;
            for (int i1 = 0; i1 < Main.maxTilesX; ++i1) {
                for (int j1 = 0; j1 < Main.maxTilesY; ++j1) {
                    Tile tileSafely = Framing.GetTileSafely(i1, j1);
                    if (tileSafely.TileType == (int)tileType.GetValue(0) || (tileType.Length != 1 && tileSafely.TileType == (int)tileType.GetValue(1))) {
                        if (i1 < minValue)
                            minValue = i1;
                        if (i1 > maxValue)
                            maxValue = i1;
                    }
                }
            }
            for (int index1 = 0; index1 < 1000; ++index1) {
                int x = WorldGen.genRand.Next(minValue, maxValue - width);
                int y = WorldGen.genRand.Next((int)GenVars.rockLayerLow, (int)GenVars.rockLayer + 100);
                Rectangle rectangle = new(x, y, width, height);
                if (CanPlaceStructure(rectangle, 0)) {
                    bool flag = true;
                    for (int index2 = 0; index2 < 1000; ++index2) {
                        Chest chest;
                        if (!(chest = Main.chest[index2]).IsNull && rectangle.Contains(chest.x, chest.y)) {
                            flag = false;
                            break;
                        }
                    }
                    if (flag) {
                        i = x;
                        j = y;
                        return true;
                    }
                }
            }
            return false;
        }

        // StructureMap has no typed members in the proxy. Raw names, checked against the game's metadata.
        private static MethodRef canPlaceMethod, addStructureMethod;
        private static readonly int[] StructureLayout = { 16, -1, -1 };

        public static unsafe bool CanPlaceStructure(Rectangle area, int padding) {
            ref readonly MethodRef method = ref GameInterop.Method(ref canPlaceMethod, StructureMap.Class, "bool CanPlace(Rectangle area, int padding)", StructureLayout);
            return (CallStructureMap(method, area, padding) & 0xff) != 0;
        }

        public static unsafe void AddStructure(Rectangle area, int padding) {
            ref readonly MethodRef method = ref GameInterop.Method(ref addStructureMethod, StructureMap.Class, "void AddStructure(Rectangle area, int padding)", StructureLayout);
            CallStructureMap(method, area, padding);
        }

        private static unsafe long CallStructureMap(in MethodRef method, Rectangle area, int padding) {
            CallFrame frame = default;
            frame.Ret = RetKind.Int;
            frame.X[0] = (long)GenVars.structures.Pointer;
            *(Rectangle*)(frame.X + 1) = area;
            frame.X[3] = padding;
            frame.X[4] = (long)method.Info;
            GameInterop.Call(method.Fn, &frame);
            return (long)frame.Out[0];
        }

        public static int GetWorldSize() {
            int worldSize = 1;
            if (Main.maxTilesX <= 4200) worldSize = 1;
            else if (Main.maxTilesX <= 6400) worldSize = 2;
            else if (Main.maxTilesX <= 8400) worldSize = 3;

            return worldSize;
        }
    }
}