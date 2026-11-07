using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MenuMediaPort.Lib;

// Writes an uncompressed RGBA PNG (zlib "stored" blocks): enough for Unity to decode it. It also
// reduces the frame (nearest neighbor) and flattens alpha over black.
public static class Png
{
    static readonly uint[] CrcTable = MakeCrcTable();

    static uint[] MakeCrcTable()
    {
        var t = new uint[256];
        for (uint n = 0; n < 256; n++)
        {
            uint c = n;
            for (int k = 0; k < 8; k++)
                c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
            t[n] = c;
        }
        return t;
    }

    static uint Crc32(byte[] buf, int start, int end)
    {
        uint c = 0xFFFFFFFFu;
        for (int i = start; i < end; i++)
            c = CrcTable[(c ^ buf[i]) & 0xFF] ^ (c >> 8);
        return c ^ 0xFFFFFFFFu;
    }

    static uint Adler32(byte[] buf)
    {
        uint a = 1, b = 0;
        int n = buf.Length;
        for (int i = 0; i < n;)
        {
            int end = Math.Min(n, i + 5552);
            for (; i < end; i++)
            {
                a += buf[i];
                b += a;
            }
            a %= 65521;
            b %= 65521;
        }
        return (b << 16) | a;
    }

    static void Put32(byte[] buf, int o, uint v)
    {
        buf[o] = (byte)(v >> 24);
        buf[o + 1] = (byte)(v >> 16);
        buf[o + 2] = (byte)(v >> 8);
        buf[o + 3] = (byte)v;
    }

    static void PutAscii(byte[] buf, int o, string text)
    {
        for (int i = 0; i < text.Length; i++)
            buf[o + i] = (byte)text[i];
    }

    // In steps: each MoveNext() does a part of the work, and the last Current holds the PNG's bytes.
    // Media calls one step at a time, between game frames.
    // canvas: ABGR little-endian, srcW x srcH; output outW x outH.
    public static IEnumerator<byte[]?> EncodeSteps(uint[] canvas, int srcW, int srcH, int outW, int outH)
    {
        int rowLen = outW * 4 + 1;
        var raw = new byte[rowLen * outH];
        var row = new uint[outW];

        var xmap = new int[outW];
        for (int x = 0; x < outW; x++)
            xmap[x] = Math.Min(srcW - 1, (int)Math.Floor((x + 0.5) * srcW / outW));

        for (int y = 0; y < outH; y++)
        {
            int start = Math.Min(srcH - 1, (int)Math.Floor((y + 0.5) * srcH / outH)) * srcW;
            for (int x = 0; x < outW; x++)
            {
                uint v = canvas[start + xmap[x]];
                row[x] = (v >> 24) != 0 ? v : 0xFF000000u;
            }
            // The byte before each line is filter 0.
            MemoryMarshal.AsBytes(row.AsSpan()).CopyTo(raw.AsSpan(y * rowLen + 1));
        }

        yield return null;   // step 1: the pixels are ready

        uint adler = Adler32(raw);
        yield return null;   // step 2: Adler32

        // zlib: header, stored blocks of up to 65535 bytes, Adler32.
        int blocks = Math.Max(1, (int)Math.Ceiling(raw.Length / 65535.0));
        int idatLen = 2 + raw.Length + blocks * 5 + 4;
        var png = new byte[8 + 25 + (12 + idatLen) + 12];
        int o = 0;
        new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(png, o);
        o += 8;

        // IHDR
        Put32(png, o, 13);
        PutAscii(png, o + 4, "IHDR");
        Put32(png, o + 8, (uint)outW);
        Put32(png, o + 12, (uint)outH);
        png[o + 16] = 8;   // 8 bits
        png[o + 17] = 6;   // RGBA
        Put32(png, o + 21, Crc32(png, o + 4, o + 21));
        o += 25;

        // IDAT
        Put32(png, o, (uint)idatLen);
        PutAscii(png, o + 4, "IDAT");
        int d = o + 8;
        png[d++] = 0x78;
        png[d++] = 0x01;
        for (int pos = 0; pos < raw.Length || pos == 0; pos += 65535)
        {
            int n = Math.Min(65535, raw.Length - pos);
            bool last = pos + n >= raw.Length;
            png[d++] = (byte)(last ? 1 : 0);
            png[d++] = (byte)(n & 0xFF);
            png[d++] = (byte)((n >> 8) & 0xFF);
            png[d++] = (byte)(~n & 0xFF);
            png[d++] = (byte)((~n >> 8) & 0xFF);
            Array.Copy(raw, pos, png, d, n);
            d += n;
            if (last)
                break;
        }
        Put32(png, d, adler);
        d += 4;
        Put32(png, d, Crc32(png, o + 4, d));
        o = d + 4;

        // IEND
        Put32(png, o, 0);
        PutAscii(png, o + 4, "IEND");
        Put32(png, o + 8, 0xAE426082u);
        yield return png;
    }
}
