using System;

namespace UniversalMapMod.Common;

public static class Codec
{
    // Every message received on the phone must fit in 32767 bytes.
    public const int TILE_PACKET_BYTES = 20000;
    public const int UNKNOWN_TYPE = 0xFFFF;
}

// Runs of tiles in a row: x, y and length (2 bytes each), then type (2 bytes) and light per tile.
public class TileWriter
{
    readonly byte[] bytes = new byte[Codec.TILE_PACKET_BYTES + 16];
    int length;
    int head = -1;
    int sx, sy, len;

    public bool Full => length >= Codec.TILE_PACKET_BYTES - 9;

    public void Add(int x, int y, int type, byte light)
    {
        byte[] b = bytes;
        if (head < 0 || y != sy || x != sx + len || len >= 65535)
        {
            head = length;
            sx = x;
            sy = y;
            len = 0;
            b[length++] = (byte)x;
            b[length++] = (byte)(x >> 8);
            b[length++] = (byte)y;
            b[length++] = (byte)(y >> 8);
            length += 2;
        }
        b[length++] = (byte)type;
        b[length++] = (byte)(type >> 8);
        b[length++] = light;
        len++;
        b[head + 4] = (byte)len;
        b[head + 5] = (byte)(len >> 8);
    }

    public byte[] Take()
    {
        byte[] data = new byte[length];
        Array.Copy(bytes, data, length);
        length = 0;
        head = -1;
        return data;
    }
}

// Walks what TileWriter wrote, one tile at a time.
public class TileReader
{
    readonly byte[] bytes;
    int i;
    int x, y, len, k;

    public int X, Y, Type;
    public byte Light;

    public TileReader(byte[] bytes) => this.bytes = bytes;

    public bool Next()
    {
        byte[] b = bytes;
        while (true)
        {
            if (k < len && i + 3 <= b.Length)
            {
                X = x + k;
                Y = y;
                Type = b[i] | (b[i + 1] << 8);
                Light = b[i + 2];
                k++;
                i += 3;
                return true;
            }
            if (i + 6 > b.Length)
                return false;
            x = b[i] | (b[i + 1] << 8);
            y = b[i + 2] | (b[i + 3] << 8);
            len = b[i + 4] | (b[i + 5] << 8);
            k = 0;
            i += 6;
        }
    }
}
