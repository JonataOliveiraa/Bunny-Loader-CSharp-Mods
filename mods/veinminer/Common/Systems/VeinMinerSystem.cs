using System.Collections.Generic;
using BunnyLoader.Interop;
using Terraria;
using Terraria.ModLoader;
using VeinMinerMod.Common.Configs;

namespace VeinMinerMod.Common.Systems;

public class VeinMinerSystem : ModSystem
{
    const int TilesPerTick = 10;
    const int MsgTileManipulation = 17;

    static readonly (int X, int Y)[] Around =
    {
        (0, 1), (1, 1), (1, 0), (1, -1), (0, -1), (-1, -1), (-1, 0), (-1, 1),
    };

    static readonly Queue<(int X, int Y, int Type)> Pending = new();
    static readonly HashSet<long> Queued = new();

    static long Key(int x, int y) => ((long)x << 32) | (uint)y;

    static int TypeAt(int x, int y)
    {
        Tile tile = Main.tile[x, y];
        return tile.active() ? tile.type : -1;
    }

    static bool CanVeinMine(int type)
    {
        VeinMinerConfig config = VeinMinerConfig.Instance;
        if (type < 0)
            return false;
        if (config.AllowOres && TileList.IsOre(type))
            return true;
        if (config.UseDefaultList && TileList.IsDefault(type))
            return true;
        return config.UsePlayerList && TileList.HasPlayerTile(type);
    }

    static void QueueVein(int x, int y, int type)
    {
        int maxX = Main.maxTilesX - 1, maxY = Main.maxTilesY - 1;
        int limit = VeinMinerConfig.Instance.MaxTiles;
        var seen = new HashSet<long> { Key(x, y) };
        var open = new List<(int X, int Y)> { (x, y) };
        int found = 0;

        for (int i = 0; i < open.Count && found < limit; i++)
        {
            foreach (var (dx, dy) in Around)
            {
                int nx = open[i].X + dx, ny = open[i].Y + dy;
                if (nx < 1 || ny < 1 || nx >= maxX || ny >= maxY || !seen.Add(Key(nx, ny)))
                    continue;
                if (TypeAt(nx, ny) != type)
                    continue;
                open.Add((nx, ny));
                if (Queued.Add(Key(nx, ny)))
                    Pending.Enqueue((nx, ny, type));
                if (++found >= limit)
                    break;
            }
        }
    }

    static void Break(int x, int y, int type)
    {
        if (TypeAt(x, y) != type)
            return;
        WorldGen.KillTile(x, y, false, false, false);
        if ((Main.netMode & 1) != 0 && TypeAt(x, y) != type)
            NetMessage.SendData(MsgTileManipulation, -1, -1, default, 0, x, y, 0, 0, 0, 0);
    }

    static void Clear()
    {
        Pending.Clear();
        Queued.Clear();
    }

    static bool LocalPickaxe()
    {
        Player player = Main.LocalPlayer;
        return !player.IsNull && player.active && !player.dead && player.HeldItem.pick > 0;
    }

    public override void OnModLoad()
    {
        On_HitTile.AddDamage += (orig, self, tileId, damageAmount, updateAmount) =>
        {
            int damage = orig(self, tileId, damageAmount, updateAmount);
            if (damage < 100 || !VeinMinerConfig.Instance.Enabled || !LocalPickaxe())
                return damage;

            GameArray<int> data = self.HitTileObjectData;
            int slot = tileId * HitTile.HitTileObjectCount;
            if (tileId < 0 || data.IsNull || slot + HitTile.yIndex >= data.Length)
                return damage;
            int x = data[slot + HitTile.xIndex], y = data[slot + HitTile.yIndex];
            int type = TypeAt(x, y);
            if (CanVeinMine(type))
                QueueVein(x, y, type);
            return damage;
        };
    }

    public override void PostUpdateEverything()
    {
        if (Pending.Count == 0)
            return;
        if (!VeinMinerConfig.Instance.Enabled)
        {
            Clear();
            return;
        }
        for (int i = 0; i < TilesPerTick && Pending.TryDequeue(out var tile); i++)
        {
            Queued.Remove(Key(tile.X, tile.Y));
            Break(tile.X, tile.Y, tile.Type);
        }
    }

    public override void OnWorldUnload() => Clear();
}
