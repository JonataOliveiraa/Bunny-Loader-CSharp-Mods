using System;

namespace MenuMediaPort.Lib;

// A GIF reader. Each Next() decodes ONE frame (LZW) and composes it on the canvas, with the format's
// disposal methods 2 and 3, so the work can be spread over several game frames.
//   Width, Height        the GIF's size
//   OutWidth, OutHeight  the size after reducing it (longest side <= maxSide)
//   Count                how many frames the file has
//   Canvas               the whole picture as ABGR, valid until the next Next()
public sealed class GifReader
{
    readonly byte[] b;
    readonly int first;
    readonly uint[]? globalPalette;
    uint[]? saved;
    int prevDisposal;
    int prevX, prevY, prevW, prevH;
    bool hasPrevRect;
    int p;

    public readonly int Width, Height, OutWidth, OutHeight, Count;
    public readonly uint[] Canvas;
    public int Duration;   // ms, the sum of the delays

    // Reading past the end of a cut file gives 0, as the original's typed arrays did.
    byte At(int position) => position >= 0 && position < b.Length ? b[position] : (byte)0;

    public GifReader(byte[] bytes, int maxSide = 480)
    {
        if (!BitConverter.IsLittleEndian)
            throw new InvalidOperationException("GIF: only works on a little-endian CPU");
        b = bytes;
        if (b.Length < 14 || b[0] != 0x47 || b[1] != 0x49 || b[2] != 0x46)
            throw new InvalidOperationException("not a GIF");

        Width = b[6] | (b[7] << 8);
        Height = b[8] | (b[9] << 8);
        if (Width == 0 || Height == 0)
            throw new InvalidOperationException("GIF without a size");
        if (Width * Height > 16 * 1024 * 1024)
            throw new InvalidOperationException("GIF too large (" + Width + "x" + Height + ")");

        int flags = b[10];
        int at = 13;
        if ((flags & 0x80) != 0)
        {
            int n = 1 << ((flags & 7) + 1);
            globalPalette = Palette(at, n);
            at += n * 3;
        }
        p = at;
        first = at;

        double scale = Math.Min(1.0, Math.Max(16, maxSide) / (double)Math.Max(Width, Height));
        OutWidth = Math.Max(1, (int)Math.Floor(Width * scale + 0.5));
        OutHeight = Math.Max(1, (int)Math.Floor(Height * scale + 0.5));

        Canvas = new uint[Width * Height];
        Count = CountFrames();
    }

    uint[] Palette(int at, int n)
    {
        var pal = new uint[256];
        Array.Fill(pal, 0xFF000000u);
        for (int i = 0; i < n; i++)
        {
            int o = at + i * 3;
            pal[i] = 0xFF000000u | ((uint)At(o + 2) << 16) | ((uint)At(o + 1) << 8) | At(o);
        }
        return pal;
    }

    int SkipSub(int at)
    {
        while (at < b.Length)
        {
            int n = b[at++];
            if (n == 0)
                break;
            at += n;
        }
        return at;
    }

    int CountFrames()
    {
        int at = first, n = 0, pending = 10, total = 0;
        while (at < b.Length)
        {
            int t = b[at++];
            if (t == 0x3B)
                break;
            if (t == 0x21)
            {
                if (At(at) == 0xF9 && At(at + 1) >= 4)
                    pending = At(at + 3) | (At(at + 4) << 8);
                at = SkipSub(at + 1);
            }
            else if (t == 0x2C)
            {
                total += pending < 2 ? 100 : pending * 10;
                pending = 10;
                int flags = At(at + 8);
                at += 9;
                if ((flags & 0x80) != 0)
                    at += 3 << ((flags & 7) + 1);
                at = SkipSub(at + 1);
                n++;
            }
            else
            {
                break;
            }
        }
        Duration = total;
        return n;
    }

    // The color indices of one frame (the GIF's LZW).
    static byte[] Lzw(byte[] data, int minCode, int total)
    {
        var output = new byte[total];
        if (minCode < 2 || minCode > 11)
            return output;

        int clear = 1 << minCode, eoi = clear + 1;
        var prefix = new ushort[4096];
        var suffix = new byte[4096];
        var stack = new byte[4097];
        for (int i = 0; i < clear; i++)
            suffix[i] = (byte)i;

        int size = minCode + 1, mask = (1 << size) - 1, next = eoi + 1;
        uint bits = 0;
        int nbits = 0, pos = 0, op = 0, prev = -1, firstCode = 0;
        int len = data.Length;

        while (op < total)
        {
            while (nbits < size)
            {
                if (pos >= len)
                    return output;   // cut short: what came stays
                bits |= (uint)data[pos++] << nbits;
                nbits += 8;
            }
            int code = (int)(bits & (uint)mask);
            bits >>= size;
            nbits -= size;

            if (code == clear)
            {
                size = minCode + 1;
                mask = (1 << size) - 1;
                next = eoi + 1;
                prev = -1;
                continue;
            }
            if (code == eoi)
                break;
            if (prev == -1)
            {
                if (code >= clear)
                    return output;   // broken
                output[op++] = suffix[code];
                prev = code;
                firstCode = code;
                continue;
            }

            int inCode = code;
            int sp = 0;
            if (code >= next)
            {
                if (code > next)
                    return output;   // broken
                stack[sp++] = (byte)firstCode;
                code = prev;
            }
            while (code >= clear)
            {
                stack[sp++] = suffix[code];
                code = prefix[code];
            }
            firstCode = suffix[code];
            stack[sp++] = (byte)firstCode;

            if (next < 4096)
            {
                prefix[next] = (ushort)prev;
                suffix[next] = (byte)firstCode;
                next++;
                if (next == (1 << size) && size < 12)
                {
                    size++;
                    mask = (1 << size) - 1;
                }
            }
            prev = inCode;
            while (sp > 0 && op < total)
                output[op++] = stack[--sp];
        }
        return output;
    }

    // The next frame, already composed on the canvas. False at the end.
    public bool Next(out int delay)
    {
        bool hasGce = false;
        int gceDisposal = 0, gceTransparent = -1, gceDelay = 10;

        while (p < b.Length)
        {
            int t = b[p++];
            if (t == 0x3B)
                break;

            if (t == 0x21)
            {
                int label = At(p++);
                if (label == 0xF9 && At(p) >= 4)
                {
                    int packed = At(p + 1);
                    hasGce = true;
                    gceDisposal = (packed >> 2) & 7;
                    gceTransparent = (packed & 1) != 0 ? At(p + 4) : -1;
                    gceDelay = At(p + 2) | (At(p + 3) << 8);
                }
                p = SkipSub(p);
                continue;
            }
            if (t != 0x2C)
                break;

            int left = At(p) | (At(p + 1) << 8);
            int top = At(p + 2) | (At(p + 3) << 8);
            int w = At(p + 4) | (At(p + 5) << 8);
            int h = At(p + 6) | (At(p + 7) << 8);
            int flags = At(p + 8);
            p += 9;

            uint[]? pal = globalPalette;
            if ((flags & 0x80) != 0)
            {
                int n = 1 << ((flags & 7) + 1);
                pal = Palette(p, n);
                p += n * 3;
            }
            int minCode = At(p++);

            // The data sub-blocks become one array.
            int q = p, total = 0;
            while (q < b.Length)
            {
                int n = b[q++];
                if (n == 0)
                    break;
                total += n;
                q += n;
            }
            var data = new byte[total];
            q = p;
            int o = 0;
            while (q < b.Length)
            {
                int n = b[q++];
                if (n == 0)
                    break;
                Array.Copy(b, q, data, o, Math.Min(n, b.Length - q));
                o += n;
                q += n;
            }
            p = q;

            int disposal = hasGce ? gceDisposal : 0;
            int transparent = hasGce ? gceTransparent : -1;
            int cs = hasGce ? gceDelay : 10;

            // The previous frame's disposal.
            if (prevDisposal == 2 && hasPrevRect)
            {
                for (int y = prevY; y < Math.Min(Height, prevY + prevH); y++)
                {
                    int from = y * Width + prevX, to = y * Width + Math.Min(Width, prevX + prevW);
                    if (to > from)
                        Array.Clear(Canvas, from, to - from);
                }
            }
            else if (prevDisposal == 3 && saved != null)
            {
                Array.Copy(saved, Canvas, Canvas.Length);
            }
            if (disposal == 3)
                saved = (uint[])Canvas.Clone();

            if (w > 0 && h > 0 && pal != null)
            {
                byte[] idx = Lzw(data, minCode, w * h);
                int[]? rows = (flags & 0x40) != 0 ? Interlace(h) : null;
                for (int r = 0; r < h; r++)
                {
                    int y = top + (rows != null ? rows[r] : r);
                    if (y >= Height)
                        continue;
                    int start = y * Width + left, from = r * w;
                    int limit = Math.Min(w, Width - left);
                    for (int x = 0; x < limit; x++)
                    {
                        int c = idx[from + x];
                        if (c != transparent)
                            Canvas[start + x] = pal[c];
                    }
                }
            }

            prevDisposal = disposal;
            prevX = left;
            prevY = top;
            prevW = w;
            prevH = h;
            hasPrevRect = true;
            delay = cs < 2 ? 100 : cs * 10;
            return true;
        }
        delay = 0;
        return false;
    }

    // The order of an interlaced GIF's lines: line read r -> line of the picture.
    static int[] Interlace(int h)
    {
        var rows = new int[h];
        int r = 0;
        foreach ((int start, int step) in new[] { (0, 8), (4, 8), (2, 4), (1, 2) })
        {
            for (int y = start; y < h; y += step)
                rows[r++] = y;
        }
        return rows;
    }
}
