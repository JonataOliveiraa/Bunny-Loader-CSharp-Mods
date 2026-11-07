using System;
using System.Runtime.CompilerServices;
using BunnyLoader.Interop;
using Terraria;
using Terraria.Map;

namespace UniversalMapMod.Common;

// WorldMap and WorldMapChunk have no members in the C# API, so they are bound by name at run
// time, the way the JavaScript version reached them. These names only fail when the game runs.
public static unsafe class MapAccess
{
    public const int EDGE = 40;
    public const int CHUNK = 64;
    const int TILE_BYTES = CHUNK * CHUNK * 4;

    static readonly GameClass ChunkClass = new("Terraria.Map", "WorldMapChunk");
    static readonly int[] Void0 = { -1 };
    static readonly int[] Two = { -1, -1, -1 };
    static readonly int[] Three = { -1, -1, -1, -1 };

    static MethodRef isRevealed, setTile, saveCompressed, getLoaded;
    static int chunksOffset = -1, maxChunkWidthOffset = -1;
    static int compressedDataOffset = -1, compressedLengthOffset = -1, dirtyOffset = -1;

    static bool readChecked, canRead, writeChecked, canWrite;
    static bool chunksMissing, chunkReadBroken;
    static byte[] compressed = new byte[TILE_BYTES];
    static byte[] tiles = new byte[TILE_BYTES];

    public static bool CanRead()
    {
        if (!readChecked)
        {
            readChecked = true;
            canRead = Bind("WorldMap.IsRevealed", () => GameInterop.Method(ref isRevealed, WorldMap.Class, "bool IsRevealed(int x, int y)", Two));
        }
        return canRead;
    }

    public static bool CanWrite()
    {
        if (!writeChecked)
        {
            writeChecked = true;
            canWrite = Bind("WorldMap.SetTile", () => GameInterop.Method(ref setTile, WorldMap.Class, "void SetTile(int x, int y, ref MapTile tile)", Three));
        }
        return canWrite;
    }

    delegate void Binding();

    static bool Bind(string label, Binding bind)
    {
        try
        {
            bind();
            return true;
        }
        catch (Exception e)
        {
            Util.LogOnce("missing:" + label, label + " not found: " + e.Message);
            return false;
        }
    }

    public static int XMin => EDGE;
    public static int XMax => Main.maxTilesX - EDGE;
    public static int YMin => EDGE;
    public static int YMax => Main.maxTilesY - EDGE;

    public static bool IsRevealed(int x, int y)
    {
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)Main.Map.Pointer;
        frame.X[1] = x;
        frame.X[2] = y;
        frame.X[3] = (long)isRevealed.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(isRevealed.Fn, &frame);
        return (frame.Out[0] & 0xFF) != 0;
    }

    // False: the game's chunk list cannot be read at all (the original's "undefined").
    // True with chunk zero: that part of the map was never touched.
    static bool ChunkAt(int x, int y, out IntPtr chunk)
    {
        chunk = IntPtr.Zero;
        try
        {
            WorldMap map = Main.Map;
            if (map.IsNull)
                return Missing();
            IntPtr array = *(IntPtr*)(map.Pointer + GameInterop.Offset(ref chunksOffset, WorldMap.Class, "_chunks"));
            int width = *(int*)(map.Pointer + GameInterop.Offset(ref maxChunkWidthOffset, WorldMap.Class, "MaxChunkWidth"));
            if (array == IntPtr.Zero || width <= 0)
                return Missing();
            var chunks = new GameArray<IntPtr>(array);
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

    static bool Loaded(IntPtr chunk)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref getLoaded, ChunkClass, "bool get_Loaded()", Void0);
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)chunk;
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
        return (frame.Out[0] & 0xFF) != 0;
    }

    public static bool ChunkEmpty(int x, int y)
    {
        if (!ChunkAt(x, y, out IntPtr chunk))
            return false;
        if (chunk == IntPtr.Zero)
            return true;
        try
        {
            if (Loaded(chunk))
                return false;
            return *(long*)(chunk + GameInterop.Offset(ref compressedLengthOffset, ChunkClass, "CompressedDataLength")) <= 0;
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
        if (chunkReadBroken || !ChunkAt(x, y, out IntPtr chunk))
            return false;
        if (chunk == IntPtr.Zero)
            return true;
        try
        {
            if (*(bool*)(chunk + GameInterop.Offset(ref dirtyOffset, ChunkClass, "dirty")))
                SaveCompressed(chunk);
            long length = *(long*)(chunk + GameInterop.Offset(ref compressedLengthOffset, ChunkClass, "CompressedDataLength"));
            if (length <= 0)
                return true;
            IntPtr data = *(IntPtr*)(chunk + GameInterop.Offset(ref compressedDataOffset, ChunkClass, "CompressedData"));
            if (data == IntPtr.Zero)
                return true;
            Span<byte> source = new GameArray<byte>(data).AsSpan().Slice(0, (int)length);
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

    static void SaveCompressed(IntPtr chunk)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref saveCompressed, ChunkClass, "void SaveCompressed()", Void0);
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)chunk;
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
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
        // MapTile.Create(type, light, 0): the color byte is the only other thing in a map tile.
        MapTile tile = default;
        tile.Type = (ushort)type;
        tile.Light = light;
        tile._extraData = 0;
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)Main.Map.Pointer;
        frame.X[1] = x;
        frame.X[2] = y;
        frame.X[3] = (long)&tile;
        frame.X[4] = (long)setTile.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(setTile.Fn, &frame);
    }

    public static void ResetNames()
    {
        chunkReadBroken = false;
        chunksMissing = false;
    }
}
