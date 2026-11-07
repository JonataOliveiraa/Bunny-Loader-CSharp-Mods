using System.Collections.Generic;

namespace UniversalMapMod.Common;

// The brightness of each tile someone has shared, in 64x64 chunks made only where needed.
public class LightStore
{
    public readonly int w, h, cw;
    public readonly Dictionary<int, byte[]> chunks = new();

    public LightStore(int w, int h)
    {
        this.w = w;
        this.h = h;
        cw = (w + 63) / 64;
    }

    public byte Get(int x, int y)
    {
        return chunks.TryGetValue((y >> 6) * cw + (x >> 6), out byte[]? c) ? c[((y & 63) << 6) | (x & 63)] : (byte)0;
    }

    public void Set(int x, int y, byte light)
    {
        int key = (y >> 6) * cw + (x >> 6);
        if (!chunks.TryGetValue(key, out byte[]? c))
        {
            c = new byte[4096];
            chunks[key] = c;
        }
        c[((y & 63) << 6) | (x & 63)] = light != 0 ? light : (byte)1;
    }
}
