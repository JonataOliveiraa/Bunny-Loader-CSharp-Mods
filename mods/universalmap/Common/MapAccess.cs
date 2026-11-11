using System;
using BunnyLoader.Interop;
using Terraria;
using Terraria.Map;

namespace UniversalMapMod.Common;

public static class MapAccess
{
    public const int EDGE = 40;
    public const int CHUNK = 64;
    const int TILE_BYTES = CHUNK * CHUNK * 4;

    static bool chunksMissing, chunkReadBroken;
    static byte[] compressed = new byte[TILE_BYTES];
    static byte[] tiles = new byte[TILE_BYTES];

    public static int XMin => EDGE;
    public static int XMax => Main.maxTilesX - EDGE;
    public static int YMin => EDGE;
    public static int YMax => Main.maxTilesY - EDGE;

    public static bool IsRevealed(int x, int y) => Main.Map.IsRevealed(x, y);

    // False: the game's chunk list cannot be read at all (the original's "undefined").
    // True with a null chunk: that part of the map was never touched.
    static bool ChunkAt(int x, int y, out WorldMapChunk chunk)
    {
        chunk = default;
        try
        {
            WorldMap map = Main.Map;
            if (map.IsNull)
                return Missing();
            GameArray<WorldMapChunk> chunks = map._chunks;
            int width = map.MaxChunkWidth;
            if (chunks.IsNull || width <= 0)
                return Missing();
            int c = (y >> 6) * width + (x >> 6);
            if (c < 0 || c >= chunks.Length)
                return false;
            chunk = chunks[c];
            return true;
        }
        catch (Exception)
        {
            return Missing();
        }
    }

    static bool Missing()
    {
        if (!chunksMissing)
            Util.LogOnce("nochunks", "WorldMap._chunks is unavailable: scanning the whole map");
        chunksMissing = true;
        return false;
    }

    public static bool ChunkEmpty(int x, int y)
    {
        if (!ChunkAt(x, y, out WorldMapChunk chunk))
            return false;
        if (chunk.IsNull)
            return true;
        try
        {
            if (chunk.Loaded)
                return false;
            return chunk.CompressedDataLength <= 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    // False: reading failed, go tile by tile. True with null: nothing explored in this part.
    // The buffer it returns is reused by the next call.
    public static bool ChunkTiles(int x, int y, out byte[]? result)
    {
        result = null;
        if (chunkReadBroken || !ChunkAt(x, y, out WorldMapChunk chunk))
            return false;
        if (chunk.IsNull)
            return true;
        try
        {
            if (chunk.dirty)
                chunk.SaveCompressed();
            long length = chunk.CompressedDataLength;
            if (length <= 0)
                return true;
            GameArray<byte> data = chunk.CompressedData;
            if (data.IsNull)
                return true;
            Span<byte> source = data.AsSpan().Slice(0, (int)length);
            if (compressed.Length < source.Length)
                compressed = new byte[source.Length];
            source.CopyTo(compressed);
            int count = Inflate.Run(new ReadOnlySpan<byte>(compressed, 0, (int)length), ref tiles);
            if (count < TILE_BYTES)
                throw new InvalidOperationException("chunk has " + count + " bytes");
            result = tiles;
            return true;
        }
        catch (Exception e)
        {
            chunkReadBroken = true;
            Util.LogOnce("chunkread", "Could not read map chunk data, sharing at full brightness instead: " + e.Message);
            return false;
        }
    }

    public static bool QueueRoom() => MapHelper.numUpdateTile < MapHelper.maxUpdateTile;

    public static void QueueRedraw(int x, int y)
    {
        int n = MapHelper.numUpdateTile;
        if (n >= MapHelper.maxUpdateTile)
            return;
        MapHelper.updateTileX[n] = (short)x;
        MapHelper.updateTileY[n] = (short)y;
        MapHelper.numUpdateTile = n + 1;
    }

    public static int TypeFromWorld(int x, int y) => MapHelper.CreateMapTile(x, y, 255).Type;

    public static void Write(int x, int y, int type, byte light)
    {
        MapTile tile = MapTile.Create((ushort)type, light, 0);
        Main.Map.SetTile(x, y, ref tile);
    }

    public static void ResetNames()
    {
        chunkReadBroken = false;
        chunksMissing = false;
    }
}
