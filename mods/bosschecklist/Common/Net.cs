using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ModLoader;

namespace BossChecklistMod.Common;

public enum PacketKind : byte
{
    Join, Allow, WorldRecords, Track, Record, WorldRecord, MiniKill, Reset, World, Hide, Mark, Chat,
}

// The original sent JSON text. Both ends run this port, so each message is written as
// plain binary. The hosted game's local player reads the same bytes without the network.
public static class Net
{
    public delegate void Handler(BinaryReader reader, int from, bool fromServer);

    static Mod? mod;
    static readonly Dictionary<PacketKind, Handler> handlers = new();

    public static void Install(Mod owner) => mod = owner;

    public static void On(PacketKind kind, Handler handler) => handlers[kind] = handler;

    public static void ToServer(PacketKind kind, Action<BinaryWriter> write)
    {
        if (!Util.IsClient || mod == null)
            return;
        Util.Safe("send " + kind, () =>
        {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)kind);
            write(packet);
            packet.Send();
        });
    }

    public static void ToClient(int who, PacketKind kind, Action<BinaryWriter> write)
    {
        if (!Util.IsServer || mod == null || who < 0 || who >= 255)
            return;
        if (Util.Mode == 3 && who == Main.myPlayer)
        {
            if (!handlers.TryGetValue(kind, out Handler? handler))
                return;
            Util.Safe("local " + kind, () =>
            {
                using var stream = new MemoryStream();
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                    write(writer);
                stream.Position = 0;
                using var reader = new BinaryReader(stream);
                handler(reader, -1, true);
            });
            return;
        }
        Util.Safe("send " + kind, () =>
        {
            ModPacket packet = mod.GetPacket();
            packet.Write((byte)kind);
            write(packet);
            packet.Send(who, -1);
        });
    }

    public static void ToAll(PacketKind kind, Action<BinaryWriter> write)
    {
        if (!Util.IsServer)
            return;
        foreach (Player p in ActivePlayers())
            ToClient(p.whoAmI, kind, write);
    }

    public static void Handle(BinaryReader reader, int from)
    {
        var kind = (PacketKind)reader.ReadByte();
        if (!handlers.TryGetValue(kind, out Handler? handler))
            return;
        bool fromServer = Util.IsClient;
        Util.Safe("packet " + kind, () => handler(reader, fromServer ? -1 : from, fromServer));
    }

    public static List<Player> ActivePlayers()
    {
        var active = new List<Player>();
        for (int i = 0; i < 255; i++)
        {
            Player p = Main.player[i];
            if (!p.IsNull && p.active)
                active.Add(p);
        }
        return active;
    }

    public static void WriteStrings(BinaryWriter writer, IReadOnlyCollection<string> values)
    {
        writer.Write(values.Count);
        foreach (string value in values)
            writer.Write(value);
    }

    public static List<string> ReadStrings(BinaryReader reader)
    {
        int count = reader.ReadInt32();
        var values = new List<string>(count);
        for (int i = 0; i < count; i++)
            values.Add(reader.ReadString());
        return values;
    }
}
