using System;

namespace UniversalMapMod.Common;

// Reads the zlib data of the game's compressed map chunks.
public static class Inflate
{
    static readonly int[] LENGTH_BASE = { 3, 4, 5, 6, 7, 8, 9, 10, 11, 13, 15, 17, 19, 23, 27, 31, 35, 43, 51, 59, 67, 83, 99, 115, 131, 163, 195, 227, 258 };
    static readonly int[] LENGTH_EXTRA = { 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 5, 0 };
    static readonly int[] DIST_BASE = { 1, 2, 3, 4, 5, 7, 9, 13, 17, 25, 33, 49, 65, 97, 129, 193, 257, 385, 513, 769, 1025, 1537, 2049, 3073, 4097, 6145, 8193, 12289, 16385, 24577 };
    static readonly int[] DIST_EXTRA = { 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10, 10, 11, 11, 12, 12, 13, 13 };
    static readonly int[] ORDER = { 16, 17, 18, 0, 8, 7, 9, 6, 10, 5, 11, 4, 12, 3, 13, 2, 14, 1, 15 };

    sealed class Huffman
    {
        public readonly ushort[] counts = new ushort[16];
        public readonly ushort[] symbols;

        public Huffman(byte[] lengths, int offset, int n)
        {
            symbols = new ushort[n];
            Span<ushort> next = stackalloc ushort[16];
            for (int i = 0; i < n; i++)
                counts[lengths[offset + i]]++;
            counts[0] = 0;
            for (int i = 1; i < 16; i++)
                next[i] = (ushort)(next[i - 1] + counts[i - 1]);
            for (int i = 0; i < n; i++)
            {
                if (lengths[offset + i] != 0)
                    symbols[next[lengths[offset + i]]++] = (ushort)i;
            }
        }
    }

    static Huffman? fixedLit, fixedDist;

    static void Fixed()
    {
        if (fixedLit != null)
            return;
        byte[] l = new byte[288];
        Array.Fill(l, (byte)8, 0, 144);
        Array.Fill(l, (byte)9, 144, 112);
        Array.Fill(l, (byte)7, 256, 24);
        Array.Fill(l, (byte)8, 280, 8);
        fixedLit = new Huffman(l, 0, 288);
        byte[] d = new byte[30];
        Array.Fill(d, (byte)5);
        fixedDist = new Huffman(d, 0, 30);
    }

    ref struct Reader
    {
        public ReadOnlySpan<byte> src;
        public int pos;
        public int end;
        public int bitBuf;
        public int bitCnt;

        public int Bits(int n)
        {
            int v = bitBuf;
            while (bitCnt < n)
            {
                if (pos >= end)
                    throw new InvalidOperationException("inflate: out of input");
                v |= src[pos++] << bitCnt;
                bitCnt += 8;
            }
            bitBuf = (int)((uint)v >> n);
            bitCnt -= n;
            return v & ((1 << n) - 1);
        }

        public int Decode(Huffman h)
        {
            int code = 0, first = 0, index = 0;
            for (int len = 1; len < 16; len++)
            {
                code |= Bits(1);
                int count = h.counts[len];
                if (code - first < count)
                    return h.symbols[index + code - first];
                index += count;
                first = (first + count) << 1;
                code <<= 1;
            }
            throw new InvalidOperationException("inflate: bad code");
        }
    }

    // Grows dst when it does not fit. Returns how many bytes it holds.
    public static int Run(ReadOnlySpan<byte> src, ref byte[] dst)
    {
        int length = src.Length;
        int start = 0;
        if (length >= 2 && (src[0] & 15) == 8 && ((src[0] << 8) | src[1]) % 31 == 0)
            start = 2;
        var r = new Reader { src = src, pos = start, end = length };
        byte[] output = dst;
        int n = 0;
        int last = 0;
        while (last == 0)
        {
            last = r.Bits(1);
            int type = r.Bits(2);
            if (type == 0)
            {
                r.bitBuf = 0;
                r.bitCnt = 0;
                if (r.pos + 4 > r.end)
                    throw new InvalidOperationException("inflate: bad stored block");
                int len = r.src[r.pos] | (r.src[r.pos + 1] << 8);
                r.pos += 4;
                Room(ref output, n, len);
                for (int i = 0; i < len; i++)
                    output[n++] = r.src[r.pos++];
                continue;
            }
            Huffman lit, dist;
            if (type == 1)
            {
                Fixed();
                lit = fixedLit!;
                dist = fixedDist!;
            }
            else if (type == 2)
            {
                int nlen = r.Bits(5) + 257, ndist = r.Bits(5) + 1, ncode = r.Bits(4) + 4;
                byte[] lengths = new byte[320];
                for (int i = 0; i < ncode; i++)
                    lengths[ORDER[i]] = (byte)r.Bits(3);
                var codes = new Huffman(lengths, 0, 19);
                Array.Clear(lengths, 0, 19);
                int k = 0;
                while (k < nlen + ndist)
                {
                    int sym = r.Decode(codes);
                    if (sym < 16)
                    {
                        lengths[k++] = (byte)sym;
                        continue;
                    }
                    int rep;
                    byte val = 0;
                    if (sym == 16)
                    {
                        if (k == 0)
                            throw new InvalidOperationException("inflate: bad repeat");
                        val = lengths[k - 1];
                        rep = 3 + r.Bits(2);
                    }
                    else if (sym == 17)
                    {
                        rep = 3 + r.Bits(3);
                    }
                    else
                    {
                        rep = 11 + r.Bits(7);
                    }
                    while (rep-- > 0)
                        lengths[k++] = val;
                }
                lit = new Huffman(lengths, 0, nlen);
                dist = new Huffman(lengths, nlen, ndist);
            }
            else
            {
                throw new InvalidOperationException("inflate: bad block type");
            }
            while (true)
            {
                int sym = r.Decode(lit);
                if (sym < 256)
                {
                    Room(ref output, n, 1);
                    output[n++] = (byte)sym;
                }
                else if (sym == 256)
                {
                    break;
                }
                else
                {
                    int li = sym - 257;
                    int len = LENGTH_BASE[li] + r.Bits(LENGTH_EXTRA[li]);
                    int di = r.Decode(dist);
                    int d = DIST_BASE[di] + r.Bits(DIST_EXTRA[di]);
                    if (d > n)
                        throw new InvalidOperationException("inflate: bad distance");
                    Room(ref output, n, len);
                    for (int k = 0; k < len; k++, n++)
                        output[n] = output[n - d];
                }
            }
        }
        dst = output;
        return n;
    }

    static void Room(ref byte[] output, int n, int k)
    {
        if (n + k <= output.Length)
            return;
        byte[] grown = new byte[Math.Max(output.Length * 2, n + k)];
        Array.Copy(output, grown, n);
        output = grown;
    }
}
