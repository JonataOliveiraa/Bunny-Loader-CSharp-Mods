using System;
using System.Collections.Generic;
using System.Text;
using BunnyLoader.Interop;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GnorpBuilderMod.Common;

// The structures: Builds/index.json has each one's size and its chests, signs and tile entities;
// Builds/<name>.gns has the cells: "GNS1", width and height (u16), then column by column one
// flags byte per cell followed by the parts the flags announce.
public static unsafe class Structures
{
    const byte NullTile = 1, NullWall = 2, HasTile = 4, HasWall = 8, HasLiquid = 16;
    const int SyncChunk = 32;
    const int NetTileEntitySync = 86;

    sealed class ChestItem
    {
        public string Mod = "";
        public int Type, Stack;
    }

    sealed class Extra
    {
        public string Kind = "";
        public int X, Y, Type, Item;
        public string Text = "";
        public List<ChestItem> Items = new();
    }

    sealed class Entry
    {
        public int W, H;
        public List<Extra>? Nbt;
    }

    sealed class Cells
    {
        public byte[] Bytes = Array.Empty<byte>();
        public int W, H;
    }

    // The only tile entity the data gives an item to. The class has no proxy, so its field goes by name.
    static readonly GameClass FoodPlatter = new("Terraria.GameContent.Tile_Entities", "TEFoodPlatter");
    static int foodPlatterItemOffset = -1;

    static Dictionary<string, Entry>? index;
    static readonly Dictionary<string, Cells> Cache = new();

    static Dictionary<string, Entry> Index()
    {
        if (index != null) return index;
        var result = new Dictionary<string, Entry>(StringComparer.Ordinal);
        try
        {
            string json = Encoding.UTF8.GetString(ModContent.GetFileBytes(Util.ModName + "/Builds/index.json"));
            foreach ((string name, object? value) in (Dictionary<string, object?>)MiniJson.Parse(json)!)
                result[name] = ReadEntry((Dictionary<string, object?>)value!);
        }
        catch (Exception e)
        {
            GnorpBuilder.Instance.Logger.Error("Builds/index.json could not be read: " + e.Message);
        }
        return index = result;
    }

    static Entry ReadEntry(Dictionary<string, object?> data)
    {
        var entry = new Entry { W = Int(data, "w"), H = Int(data, "h") };
        if (!data.TryGetValue("nbt", out object? nbt) || nbt is not List<object?> list) return entry;
        entry.Nbt = new List<Extra>(list.Count);
        foreach (object? item in list)
        {
            var e = (Dictionary<string, object?>)item!;
            var extra = new Extra
            {
                Kind = (string)e["kind"]!,
                X = Int(e, "x"),
                Y = Int(e, "y"),
                Type = Int(e, "type"),
                Item = Int(e, "item"),
                Text = e.TryGetValue("text", out object? text) ? text as string ?? "" : "",
            };
            if (e.TryGetValue("items", out object? items) && items is List<object?> slots)
            {
                foreach (object? slot in slots)
                {
                    var row = (List<object?>)slot!;
                    extra.Items.Add(new ChestItem { Mod = (string)row[0]!, Type = (int)(double)row[1]!, Stack = (int)(double)row[2]! });
                }
            }
            entry.Nbt.Add(extra);
        }
        return entry;
    }

    static int Int(Dictionary<string, object?> data, string key) =>
        data.TryGetValue(key, out object? value) && value is double number ? (int)number : 0;

    static Cells? Data(string name)
    {
        if (Cache.TryGetValue(name, out Cells? data)) return data;
        byte[]? bytes = null;
        try
        {
            bytes = ModContent.GetFileBytes(Util.ModName + "/Builds/" + name + ".gns");
        }
        catch (Exception)
        {
            // Reported below, the same as a damaged file.
        }
        if (bytes == null || bytes.Length < 8 || bytes[0] != 'G' || bytes[1] != 'N' || bytes[2] != 'S' || bytes[3] != '1')
        {
            GnorpBuilder.Instance.Logger.Error("structure " + name + " not found or damaged");
            return null;
        }
        data = new Cells { Bytes = bytes, W = U16(bytes, 4), H = U16(bytes, 6) };
        Cache[name] = data;
        return data;
    }

    public static string BuildName(string path) => path.Substring(path.LastIndexOf('/') + 1);

    public static bool Dimensions(string path, out int w, out int h)
    {
        if (Index().TryGetValue(BuildName(path), out Entry? entry))
        {
            w = entry.W;
            h = entry.H;
            return true;
        }
        w = h = 0;
        return false;
    }

    public static bool IsInBounds(string path, int x, int y) =>
        Dimensions(path, out int w, out int h) && x >= 0 && x + w < Main.maxTilesX && y >= 0 && y + h < Main.maxTilesY;

    static int U16(byte[] b, int i) => b[i] | (b[i + 1] << 8);
    static short I16(byte[] b, int i) => (short)U16(b, i);
    static uint U32(byte[] b, int i) => (uint)(b[i] | (b[i + 1] << 8) | (b[i + 2] << 16) | (b[i + 3] << 24));

    static void ApplyTilePart(Tile tile, ushort type, short frameX, short frameY, uint bits, byte bright)
    {
        tile.SetType(type);
        if ((bits & 1) != 0) tile.active(true);
        tile.SetFrameXY(frameX, frameY);
        if ((bits & 2) != 0) tile.inActive(true);
        if ((bits & 4) != 0) tile.actuator(true);
        uint color = (bits >> 3) & 31;
        if (color != 0) tile.color((byte)color);
        uint wallColor = (bits >> 8) & 31;
        if (wallColor != 0) tile.wallColor((byte)wallColor);
        uint frameNumber = (bits >> 13) & 3;
        if (frameNumber != 0) tile.frameNumber((byte)frameNumber);
        uint wallFrameNumber = (bits >> 15) & 3;
        if (wallFrameNumber != 0) tile.wallFrameNumber((byte)wallFrameNumber);
        uint wallFrameX = (bits >> 17) & 15;
        if (wallFrameX != 0) tile.wallFrameX((int)wallFrameX * 36);
        uint wallFrameY = (bits >> 21) & 7;
        if (wallFrameY != 0) tile.wallFrameY((int)wallFrameY * 36);
        if (((bits >> 24) & 1) != 0) tile.halfBrick(true);
        uint slope = (bits >> 25) & 7;
        if (slope != 0) tile.slope((byte)slope);
        if (((bits >> 28) & 1) != 0) tile.wire(true);
        if (((bits >> 29) & 1) != 0) tile.wire2(true);
        if (((bits >> 30) & 1) != 0) tile.wire3(true);
        if (((bits >> 31) & 1) != 0) tile.wire4(true);
        if ((bright & 1) != 0) tile.invisibleBlock(true);
        if ((bright & 2) != 0) tile.invisibleWall(true);
        if ((bright & 4) != 0) tile.fullbrightBlock(true);
        if ((bright & 8) != 0) tile.fullbrightWall(true);
    }

    static void PlaceCells(Cells data, int x0, int y0)
    {
        byte[] b = data.Bytes;
        int i = 8;
        for (int col = 0; col < data.W; col++)
        {
            for (int row = 0; row < data.H; row++)
            {
                byte flags = b[i++];
                ushort type = 0, wall = 0;
                short frameX = 0, frameY = 0;
                uint bits = 0;
                byte bright = 0, amount = 0, liquidType = 0;
                if ((flags & HasTile) != 0)
                {
                    type = (ushort)U16(b, i);
                    frameX = I16(b, i + 2);
                    frameY = I16(b, i + 4);
                    bits = U32(b, i + 6);
                    bright = b[i + 10];
                    i += 11;
                }
                if ((flags & HasWall) != 0)
                {
                    wall = (ushort)U16(b, i);
                    i += 2;
                }
                if ((flags & HasLiquid) != 0)
                {
                    amount = b[i];
                    liquidType = b[i + 1];
                    i += 2;
                }
                if ((flags & NullTile) != 0 && (flags & NullWall) != 0) continue;

                Tile tile = Main.tile[x0 + col, y0 + row];
                if ((flags & NullTile) != 0)
                {
                    tile.SetWall(wall);
                    continue;
                }

                bool keepsWall = (flags & NullWall) != 0;
                ushort kept = keepsWall ? tile.wall : (ushort)0;
                tile.ClearEverything();
                if ((flags & HasTile) != 0) ApplyTilePart(tile, type, frameX, frameY, bits, bright);
                if (keepsWall) tile.SetWall(kept);
                else if (wall != 0) tile.SetWall(wall);
                if (amount != 0)
                {
                    tile.liquidType(liquidType);
                    tile.SetLiquid(amount);
                }
            }
        }
    }

    static void PlaceExtras(string name, int x0, int y0)
    {
        if (!Index().TryGetValue(name, out Entry? entry) || entry.Nbt == null) return;
        foreach (Extra e in entry.Nbt)
        {
            int x = x0 + e.X, y = y0 + e.Y;
            try
            {
                if (e.Kind == "chest") PlaceChest(e, x, y);
                else if (e.Kind == "sign") PlaceSign(e, x, y);
                else if (e.Kind == "te") PlaceTileEntity(e, x, y);
            }
            catch (Exception ex)
            {
                GnorpBuilder.Instance.Logger.Warn(name + ": " + e.Kind + " at " + x + "," + y + " failed: " + ex.Message);
            }
        }
    }

    static void PlaceChest(Extra e, int x, int y)
    {
        int id = Chest.CreateChest(x, y, -1);
        if (id < 0) return;
        Chest chest = Main.chest[id];
        for (int slot = 0; slot < e.Items.Count; slot++)
        {
            ChestItem item = e.Items[slot];
            if (item.Mod != "Terraria" || item.Type <= 0) continue;
            Item made = Item.New();
            made.SetDefaults(item.Type, default);
            made.stack = item.Stack;
            chest.SetItem(slot, made);
        }
    }

    static void PlaceSign(Extra e, int x, int y)
    {
        int id = Sign.ReadSign(x, y, true);
        if (id >= 0) Sign.TextSign(id, e.Text);
    }

    static void PlaceTileEntity(Extra e, int x, int y)
    {
        TileEntity.PlaceEntityNet(x, y, e.Type);
        if (e.Item <= 0) return;

        var byPosition = new GameDictionary(TileEntity.ByPosition);
        if (!byPosition.TryGetValue(TileEntity.GetKey(x, y), out IntPtr entity) || entity == IntPtr.Zero) return;
        if (!GameInterop.IsInstance(entity, FoodPlatter)) return;

        Item item = Item.New();
        item.SetDefaults(e.Item, default);
        *(IntPtr*)(entity + GameInterop.Offset(ref foodPlatterItemOffset, FoodPlatter, "item")) = item.Pointer;
        if ((Main.netMode & NetmodeID.Server) != 0)
            NetMessage.SendData(NetTileEntitySync, -1, -1, default(NetworkText), new TileEntity(entity).ID, x, y, 0f, 0, 0, 0);
    }

    static void FrameTile(int x, int y)
    {
        try { WorldGen.TileFrame(x, y, false, false); }
        catch (Exception) { /* One bad frame must not stop the rest of the outline. */ }
    }

    static void FrameWall(int x, int y)
    {
        try { WorldGen.SquareWallFrame(x, y, true); }
        catch (Exception) { /* One bad frame must not stop the rest of the outline. */ }
    }

    // The cells carry their own frames; only the outline has new neighbours.
    static void Finish(int x0, int y0, int w, int h)
    {
        for (int n = 0; n <= w; n++)
        {
            FrameTile(x0 + n, y0);
            FrameTile(x0 + n, y0 + h);
            FrameWall(x0 + n, y0);
            FrameWall(x0 + n, y0 + h);
        }
        for (int n = 0; n <= h; n++)
        {
            FrameTile(x0, y0 + n);
            FrameTile(x0 + w, y0 + n);
            FrameWall(x0, y0 + n);
            FrameWall(x0 + w, y0 + n);
        }
        if ((Main.netMode & NetmodeID.Server) != 0) SyncArea(x0, y0, w, h);
    }

    public static void SyncArea(int x0, int y0, int w, int h)
    {
        for (int x = x0; x < x0 + w; x += SyncChunk)
        {
            for (int y = y0; y < y0 + h; y += SyncChunk)
            {
                try
                {
                    NetMessage.SendTileSquare(-1, x, y, Math.Min(SyncChunk, x0 + w - x), Math.Min(SyncChunk, y0 + h - y), TileChangeType.None);
                }
                catch (Exception e)
                {
                    GnorpBuilder.Instance.Logger.Warn("area sync at " + x + "," + y + " failed: " + e.Message);
                }
            }
        }
    }

    public static bool GenerateStructure(string path, int x, int y)
    {
        string name = BuildName(path);
        Cells? data = Data(name);
        if (data == null || !IsInBounds(name, x, y)) return false;
        PlaceCells(data, x, y);
        PlaceExtras(name, x, y);
        Finish(x, y, data.W, data.H);
        return true;
    }
}
