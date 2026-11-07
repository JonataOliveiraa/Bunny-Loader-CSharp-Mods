using System;
using System.Diagnostics;
using System.IO;
using BunnyLoader.Interop;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace PingModPort.Common;

public class PingModPlayer : ModPlayer
{
    public int currentPing;
    public int currentIconStyle;
}

public static unsafe class Ping
{
    const int MSG_REQUEST = 0;
    const int MSG_RESPONSE = 1;
    const int MSG_SYNC = 2;
    const long TIMEOUT_MS = 10000;
    const int JOINED = 10;

    static Mod? mod;
    static int sequence;
    static long sentAt;
    static int waiting = -1;
    // The next check, in real time: the phone's frame rate says nothing about seconds.
    static long nextAt;

    static int connectionStateOffset = -1;
    static bool connectionStateBroken;

    public static void Install(Mod theMod) => mod = theMod;

    public static long NowMs => Stopwatch.GetTimestamp() * 1000 / Stopwatch.Frequency;

    // Math.round from the original: halves go up.
    public static int Round(double value) => (int)Math.Floor(value + 0.5);

    public static string T(string key) => Language.GetTextValue("Mods.pingmod." + key) ?? key;

    public static PingModPlayer? StateOf(int whoAmI)
    {
        try
        {
            Player player = Main.player[whoAmI];
            return player.IsNull ? null : player.GetModPlayer<PingModPlayer>();
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static int MyPing() => StateOf(Main.myPlayer)?.currentPing ?? 0;

    public static void Reset()
    {
        PingModPlayer? me = StateOf(Main.myPlayer);
        if (me != null)
            me.currentPing = 0;
        waiting = -1;
        nextAt = 0;
    }

    // A client, or the phone that hosts the world and plays in it.
    public static bool IsPlayingOnline => Main.netMode == 1 || Main.netMode == 3;

    // Only a real client has to wait. The host (3) sends as soon as it is playing.
    static bool Joined() => Main.netMode != 1 || ConnectionState() >= JOINED;

    // Netplay.Connection.State: RemoteConnection has no members in the C# API.
    static int ConnectionState()
    {
        if (connectionStateBroken)
            return 0;
        try
        {
            RemoteConnection connection = Netplay.Connection;
            if (connection.IsNull)
                return 0;
            return *(int*)(connection.Pointer + GameInterop.Offset(ref connectionStateOffset, RemoteConnection.Class, "State"));
        }
        catch (Exception e)
        {
            connectionStateBroken = true;
            mod?.Logger.Error("RemoteConnection.State is not readable: " + e.Message);
            return 0;
        }
    }

    static bool ClientJoined(int i)
    {
        try
        {
            return Netplay.Clients[i].State >= JOINED;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static void SendToJoined(int index, int ping, int style, int ignore = -1)
    {
        int count = Math.Min(255, Netplay.Clients.IsNull ? 0 : Netplay.Clients.Length);
        for (int i = 0; i < count; i++)
        {
            // The host's own slot is the host itself: nobody is there to receive it.
            if (i == ignore || (i == Main.myPlayer && Main.netMode == 3) || !ClientJoined(i))
                continue;
            ModPacket packet = mod!.GetPacket();
            packet.Write(MSG_SYNC);
            packet.Write(index);
            packet.Write(ping);
            packet.Write(style);
            packet.Send(i);
        }
    }

    static long IntervalMs => Math.Max(1, PingConfig.Instance?.PingSyncTimer ?? 3) * 1000L;

    public static void Tick()
    {
        if (!IsPlayingOnline || mod == null || !Joined())
            return;
        long now = NowMs;
        // The host measures nothing: its ping stays 0, and it only tells the others its icon.
        if (Main.netMode == 3)
        {
            if (now < nextAt)
                return;
            nextAt = now + IntervalMs;
            SendSync();
            return;
        }
        if (waiting >= 0 && now - sentAt > TIMEOUT_MS)
            waiting = -1;
        if (now < nextAt || waiting >= 0)
            return;
        nextAt = now + IntervalMs;
        sequence = (sequence + 1) & 0xffff;
        waiting = sequence;
        sentAt = now;
        ModPacket packet = mod.GetPacket();
        packet.Write(MSG_REQUEST);
        packet.Write(sequence);
        packet.Send();
    }

    public static void SendSync()
    {
        if (!IsPlayingOnline || mod == null || !Joined())
            return;
        PingModPlayer? me = StateOf(Main.myPlayer);
        if (me == null)
            return;
        me.currentIconStyle = PingConfig.StyleIndex;
        if (Main.netMode == 3)
        {
            SendToJoined(Main.myPlayer, me.currentPing, me.currentIconStyle);
            return;
        }
        ModPacket packet = mod.GetPacket();
        packet.Write(MSG_SYNC);
        packet.Write(Main.myPlayer);
        packet.Write(me.currentPing);
        packet.Write(me.currentIconStyle);
        packet.Send();
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        // The phone's host has netMode 3 and answers and relays like a dedicated server.
        bool server = (Main.netMode & NetmodeID.Server) != 0;
        int type = reader.ReadInt32();
        if (type == MSG_REQUEST && server)
        {
            int seq = reader.ReadInt32();
            ModPacket packet = mod!.GetPacket();
            packet.Write(MSG_RESPONSE);
            packet.Write(seq);
            packet.Send(whoAmI);
        }
        else if (type == MSG_RESPONSE && Main.netMode == 1)
        {
            int seq = reader.ReadInt32();
            if (seq != waiting)
                return;
            waiting = -1;
            PingModPlayer? me = StateOf(Main.myPlayer);
            if (me != null)
                me.currentPing = Math.Max(0, Round((NowMs - sentAt) / 2.0));
            SendSync();
        }
        else if (type == MSG_SYNC)
        {
            int index = server ? whoAmI : reader.ReadInt32();
            if (server)
                reader.ReadInt32();
            int ping = reader.ReadInt32();
            int style = reader.ReadInt32();
            if (index < 0 || index >= 255)
                return;
            PingModPlayer? state = StateOf(index);
            if (state != null)
            {
                state.currentPing = ping;
                state.currentIconStyle = style;
            }
            if (server)
                SendToJoined(index, ping, style, whoAmI);
        }
    }
}
