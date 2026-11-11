using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using UniversalMapMod.Common.Configs;

namespace UniversalMapMod.Common;

public interface ISyncJob
{
    bool Step(double deadline);

    void Abort() { }
}

public class ScanJob : ISyncJob
{
    readonly int[] chunks;
    readonly Action<byte[], bool> onPacket;
    readonly TileWriter writer = new();
    int index;
    int count;
    bool slow;
    int slowX0, slowX1, slowY1, slowX, slowY;

    public ScanJob(int[] chunks, Action<byte[], bool> onPacket)
    {
        this.chunks = chunks;
        this.onPacket = onPacket;
    }

    public void Abort() => Sync.scanning = false;

    void Add(int x, int y, int type, byte light)
    {
        count++;
        Sync.known!.Set(x, y, light);
        writer.Add(x, y, type, light);
        if (writer.Full)
            onPacket(writer.Take(), false);
    }

    bool ReadChunk(in Sync.Area a)
    {
        if (!MapAccess.ChunkTiles(a.cx, a.cy, out byte[]? tiles))
            return false;
        if (tiles == null)
            return true;
        LightStore known = Sync.known!;
        for (int y = a.y0; y < a.y1; y++)
        {
            for (int x = a.x0; x < a.x1; x++)
            {
                int i = (((y - a.cy) << 6) + (x - a.cx)) << 2;
                byte light = tiles[i + 2];
                if (light != 0 && known.Get(x, y) == 0)
                    Add(x, y, tiles[i] | (tiles[i + 1] << 8), light);
            }
        }
        return true;
    }

    bool SlowStep(double deadline)
    {
        LightStore known = Sync.known!;
        int checks = 0;
        while (slowY < slowY1)
        {
            if (Sync.Blocked)
                return false;
            if (known.Get(slowX, slowY) == 0 && MapAccess.IsRevealed(slowX, slowY))
                Add(slowX, slowY, Codec.UNKNOWN_TYPE, Sync.FULL_LIGHT);
            if (++slowX >= slowX1)
            {
                slowX = slowX0;
                slowY++;
            }
            if ((++checks & 255) == 0 && Util.NowMs > deadline)
                return false;
        }
        slow = false;
        return true;
    }

    public bool Step(double deadline)
    {
        while (true)
        {
            if (Sync.Blocked)
                return false;
            if (slow && !SlowStep(deadline))
                return false;
            if (index >= chunks.Length)
            {
                onPacket(writer.Take(), true);
                Util.Log("scan: " + chunks.Length + " map parts read, " + count + " new tiles shared");
                return true;
            }
            Sync.Area a = Sync.ChunkArea(chunks[index++]);
            if (a.x0 < a.x1 && a.y0 < a.y1 && !MapAccess.ChunkEmpty(a.cx, a.cy) && !ReadChunk(a))
            {
                slow = true;
                slowX0 = a.x0;
                slowX1 = a.x1;
                slowY1 = a.y1;
                slowX = a.x0;
                slowY = a.y0;
            }
            if (Util.NowMs > deadline)
                return false;
        }
    }
}

public class ApplyJob : ISyncJob
{
    readonly TileReader tiles;
    bool pending;
    int written;

    public ApplyJob(byte[] bytes) => tiles = new TileReader(bytes);

    public bool Step(double deadline)
    {
        LightStore? known = Sync.known;
        int checks = 0;
        while (true)
        {
            if (!pending)
            {
                if (!tiles.Next())
                {
                    Util.Log("received: " + written + " new tiles written to the map");
                    return true;
                }
                pending = true;
            }
            int x = tiles.X, y = tiles.Y;
            if (!MapAccess.IsRevealed(x, y))
            {
                // Wait for the map's redraw queue: a tile written past it would never show.
                if (!MapAccess.QueueRoom())
                    return false;
                MapAccess.Write(x, y, tiles.Type, tiles.Light);
                MapAccess.QueueRedraw(x, y);
                written++;
                Sync.received++;
                Sync.lastReceived = Util.NowMs;
            }
            if (known != null && known.Get(x, y) == 0)
                known.Set(x, y, tiles.Light);
            pending = false;
            if ((++checks & 127) == 0 && Util.NowMs > deadline)
                return false;
        }
    }
}

public class UnionSendJob : ISyncJob
{
    readonly int to;
    readonly int[] keys;
    readonly TileWriter writer = new();
    int k;
    int i;

    public UnionSendJob(int to)
    {
        this.to = to;
        keys = new int[Sync.known!.chunks.Count];
        Sync.known.chunks.Keys.CopyTo(keys, 0);
        Array.Sort(keys);
    }

    public bool Step(double deadline)
    {
        LightStore store = Sync.known!;
        int checks = 0;
        while (k < keys.Length)
        {
            if (Sync.Blocked)
                return false;
            int key = keys[k];
            byte[] lights = store.chunks[key];
            int cx = key % store.cw * MapAccess.CHUNK, cy = key / store.cw * MapAccess.CHUNK;
            while (i < 4096)
            {
                int n = i++;
                byte light = lights[n];
                if (light == 0)
                    continue;
                int x = cx + (n & 63), y = cy + (n >> 6);
                writer.Add(x, y, MapAccess.TypeFromWorld(x, y), light);
                if (writer.Full)
                {
                    byte[] data = writer.Take();
                    Sync.outbox.Enqueue(() => Sync.Deliver(data, to, -1));
                    return false;
                }
                if ((++checks & 127) == 0 && Util.NowMs > deadline)
                    return false;
            }
            k++;
            i = 0;
        }
        byte[] rest = writer.Take();
        if (rest.Length > 0)
            Sync.outbox.Enqueue(() => Sync.Deliver(rest, to, -1));
        return true;
    }
}

public static class Sync
{
    public readonly struct Area
    {
        public readonly int cx, cy, x0, x1, y0, y1;

        public Area(int cx, int cy, int x0, int x1, int y0, int y1)
        {
            this.cx = cx;
            this.cy = cy;
            this.x0 = x0;
            this.x1 = x1;
            this.y0 = y0;
            this.y1 = y1;
        }
    }

    const byte KIND_TILES = 0;
    const byte KIND_DATA = 1;

    const double FRAME_BUDGET_MS = 3;
    const int MAX_OUTBOX = 6;
    const int FULL_SCANS = 3;
    public const byte FULL_LIGHT = 255;

    // The original counted frames at 60 per second. These are the same times, measured.
    const double SEND_EVERY_MS = 2000.0 / 60;
    const double MARK_EVERY_MS = 500;
    const double FIRST_SYNC_MS = 10000;
    const double JOIN_DELAY_MS = 10000;
    const double NOTIFY_DELAY_MS = 2000;
    const double WATCH_EVERY_MS = 1000;
    const double RETRY_MS = 1000;
    const int JOINED = 10;

    public static Mod? mod;
    public static LightStore? known;
    public static bool scanning;
    public static long received;
    public static double lastReceived;
    public static readonly Queue<Action> outbox = new();

    static readonly ConcurrentQueue<(byte kind, byte[] payload, int from)> inbox = new();
    static readonly HashSet<int> dirty = new();
    static readonly List<ISyncJob> jobs = new();
    static readonly List<ISyncJob> applies = new();
    static readonly bool[] present = new bool[255];
    static readonly double[] joinAt = new double[255];
    static double timerStart, lastSend, lastMark, lastWatch;
    static int fullScans;

    static int Mode => Main.netMode;

    // The host (3) is both of these: it plays, and it passes the map on to the others.
    static bool IsClient => Mode == 1;
    static bool IsServer => (Mode & NetmodeID.Server) != 0;
    static bool HasLocalPlayer => (Mode & NetmodeID.MultiplayerClient) != 0;

    public static bool Blocked => outbox.Count >= MAX_OUTBOX;

    static bool Joined()
    {
        if (Mode != 1)
            return true;
        RemoteConnection connection = Netplay.Connection;
        return !connection.IsNull && connection.State >= JOINED;
    }

    public static Area ChunkArea(int c)
    {
        int cw = (Main.maxTilesX + MapAccess.CHUNK - 1) / MapAccess.CHUNK;
        int cx = c % cw * MapAccess.CHUNK, cy = c / cw * MapAccess.CHUNK;
        return new Area(cx, cy, Math.Max(MapAccess.XMin, cx), Math.Min(MapAccess.XMax, cx + MapAccess.CHUNK),
            Math.Max(MapAccess.YMin, cy), Math.Min(MapAccess.YMax, cy + MapAccess.CHUNK));
    }

    public static void Reset()
    {
        known = null;
        dirty.Clear();
        jobs.Clear();
        applies.Clear();
        outbox.Clear();
        timerStart = Util.NowMs;
        scanning = false;
        received = 0;
        fullScans = 0;
        Array.Clear(present);
        Array.Clear(joinAt);
    }

    static double IntervalMs => UniversalMapConfig.IntervalMinutes * 60000.0;

    static bool Ready()
    {
        if (Main.gameMenu || Mode == 0)
            return false;
        int w = Main.maxTilesX, h = Main.maxTilesY;
        if (w <= 0 || h <= 0)
            return false;
        if (known == null || known.w != w || known.h != h)
        {
            known = new LightStore(w, h);
            dirty.Clear();
            if (HasLocalPlayer)
                MarkAll();
            // The first sync comes soon after joining, whatever the interval.
            timerStart = Util.NowMs - Math.Max(0, IntervalMs - FIRST_SYNC_MS);
        }
        return true;
    }

    static void MarkAll()
    {
        int cw = (Main.maxTilesX + MapAccess.CHUNK - 1) / MapAccess.CHUNK, ch = (Main.maxTilesY + MapAccess.CHUNK - 1) / MapAccess.CHUNK;
        for (int i = 0; i < cw * ch; i++)
            dirty.Add(i);
    }

    static void MarkAround()
    {
        Player p = Main.player[Main.myPlayer];
        if (p.IsNull || !p.active)
            return;
        float zoomX = Main.GameViewMatrix.Zoom.X;
        float zoom = Math.Max(0.25f, zoomX != 0 ? zoomX : 1);
        int rx = (int)Math.Ceiling(Main.screenWidth / zoom / 32) + 48, ry = (int)Math.Ceiling(Main.screenHeight / zoom / 32) + 48;
        int px = (int)Math.Floor(p.position.X / 16), py = (int)Math.Floor(p.position.Y / 16);
        int cw = (Main.maxTilesX + MapAccess.CHUNK - 1) / MapAccess.CHUNK, ch = (Main.maxTilesY + MapAccess.CHUNK - 1) / MapAccess.CHUNK;
        int x0 = Math.Max(0, (px - rx) >> 6), x1 = Math.Min(cw - 1, (px + rx) >> 6);
        int y0 = Math.Max(0, (py - ry) >> 6), y1 = Math.Min(ch - 1, (py + ry) >> 6);
        for (int cy = y0; cy <= y1; cy++)
        {
            for (int cx = x0; cx <= x1; cx++)
                dirty.Add(cy * cw + cx);
        }
    }

    public static void Update()
    {
        if (!Ready())
        {
            inbox.Clear();
            return;
        }
        ReadInbox();
        double now = Util.NowMs;
        if (outbox.Count > 0 && now - lastSend >= SEND_EVERY_MS)
        {
            lastSend = now;
            try
            {
                outbox.Dequeue()();
            }
            catch (Exception e)
            {
                Util.LogOnce("send:" + e.Message, "Failed to send map data: " + e);
            }
        }
        if (HasLocalPlayer)
        {
            if (now - lastMark >= MARK_EVERY_MS)
            {
                lastMark = now;
                MarkAround();
            }
            if (now - timerStart >= IntervalMs)
            {
                if (Joined())
                {
                    timerStart = now;
                    StartScan(fullScans++ < FULL_SCANS);
                }
                else
                {
                    timerStart = now - IntervalMs + RETRY_MS;
                }
            }
            Notify(now);
        }
        if (IsServer && now - lastWatch >= WATCH_EVERY_MS)
        {
            lastWatch = now;
            WatchPlayers(now);
        }
        RunJobs(now);
    }

    static void RunJobs(double now)
    {
        double deadline = now + FRAME_BUDGET_MS;
        RunQueue(applies, deadline);
        RunQueue(jobs, deadline);
    }

    static void RunQueue(List<ISyncJob> list, double deadline)
    {
        while (list.Count > 0 && Util.NowMs < deadline)
        {
            bool done;
            try
            {
                done = list[0].Step(deadline);
            }
            catch (Exception e)
            {
                Util.LogOnce("job:" + e.Message, "Map sync step failed: " + e);
                list[0].Abort();
                done = true;
            }
            if (!done)
                break;
            list.RemoveAt(0);
        }
    }

    static void Notify(double now)
    {
        if (received == 0 || now - lastReceived < NOTIFY_DELAY_MS)
            return;
        if (UniversalMapConfig.ShowNotifications)
            Util.Green("Received", received.ToString("N0", CultureInfo.InvariantCulture));
        received = 0;
    }

    static bool StartScan(bool force)
    {
        if (scanning)
            return false;
        if (force)
            MarkAll();
        int[] chunks = new int[dirty.Count];
        dirty.CopyTo(chunks);
        Array.Sort(chunks);
        dirty.Clear();
        if (chunks.Length == 0)
            return true;
        scanning = true;
        int me = Main.myPlayer;
        jobs.Add(new ScanJob(chunks, (payload, last) =>
        {
            if (payload.Length > 0)
            {
                if (IsClient)
                    outbox.Enqueue(() => Packet(KIND_TILES, payload).Send());
                else
                    Relay(payload, me);
            }
            if (last)
                scanning = false;
        }));
        return true;
    }

    public static void SyncNow()
    {
        if (Mode == 0)
        {
            Util.Orange("MultiplayerOnly");
            return;
        }
        if (!Ready() || !Joined())
            return;
        if (StartScan(true))
        {
            timerStart = Util.NowMs;
            Util.Green("Sharing");
        }
    }

    static ModPacket Packet(byte kind, byte[] payload)
    {
        ModPacket packet = mod!.GetPacket(payload.Length + 64);
        packet.Write(kind);
        packet.Write(payload.Length);
        packet.Write(payload);
        return packet;
    }

    public static void Deliver(byte[] data, int to, int from)
    {
        ModPacket packet = Packet(KIND_DATA, data);
        if (to >= 0)
        {
            packet.Send(to, -1);
            return;
        }
        packet.Send(-1, from);
        // The host is a player too: what others sent also goes on its own map.
        if (Mode == 3 && from != Main.myPlayer)
            Apply(data);
    }

    static void Relay(byte[] payload, int from)
    {
        bool needsWorld = false;
        var reader = new TileReader(payload);
        while (reader.Next())
        {
            if (reader.Type == Codec.UNKNOWN_TYPE)
            {
                needsWorld = true;
                break;
            }
        }
        if (!needsWorld)
        {
            outbox.Enqueue(() => Deliver(payload, -1, from));
            return;
        }
        var writer = new TileWriter();
        reader = new TileReader(payload);
        while (reader.Next())
            writer.Add(reader.X, reader.Y, reader.Type == Codec.UNKNOWN_TYPE ? MapAccess.TypeFromWorld(reader.X, reader.Y) : reader.Type, reader.Light);
        byte[] fixedData = writer.Take();
        outbox.Enqueue(() => Deliver(fixedData, -1, from));
    }

    static void Merge(byte[] payload, int from)
    {
        LightStore store = known!;
        var writer = new TileWriter();
        var reader = new TileReader(payload);
        while (reader.Next())
        {
            if (store.Get(reader.X, reader.Y) != 0)
                continue;
            store.Set(reader.X, reader.Y, reader.Light);
            writer.Add(reader.X, reader.Y, reader.Type, reader.Light);
            if (writer.Full)
                Relay(writer.Take(), from);
        }
        byte[] fresh = writer.Take();
        if (fresh.Length > 0)
            Relay(fresh, from);
    }

    static void Apply(byte[] payload)
    {
        applies.Add(new ApplyJob(payload));
    }

    static void WatchPlayers(double now)
    {
        for (int i = 0; i < 255; i++)
        {
            Player player = Main.player[i];
            bool active = !player.IsNull && player.active && !(Mode == 3 && i == Main.myPlayer);
            if (active && !present[i])
                joinAt[i] = now + JOIN_DELAY_MS;
            if (!active)
                joinAt[i] = 0;
            present[i] = active;
        }
        for (int i = 0; i < 255; i++)
        {
            if (joinAt[i] == 0 || now < joinAt[i])
                continue;
            joinAt[i] = 0;
            jobs.Add(new UnionSendJob(i));
        }
    }

    // Nothing guarantees which thread a packet arrives on, and all of this state belongs to the
    // game thread, so a packet only goes in a queue here. Update picks it up on the next tick.
    public static void Handle(BinaryReader reader, int from)
    {
        byte kind = reader.ReadByte();
        inbox.Enqueue((kind, reader.ReadBytes(reader.ReadInt32()), from));
    }

    static void ReadInbox()
    {
        while (inbox.TryDequeue(out (byte kind, byte[] payload, int from) packet))
        {
            if (packet.kind == KIND_TILES && IsServer)
                Merge(packet.payload, packet.from);
            else if (packet.kind == KIND_DATA && IsClient)
                Apply(packet.payload);
        }
    }
}
