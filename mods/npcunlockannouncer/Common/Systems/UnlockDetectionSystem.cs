using System;
using System.Diagnostics;
using NPCUnlockAnnouncerMod.Common.Configs;
using NPCUnlockAnnouncerMod.Common.UI;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace NPCUnlockAnnouncerMod.Common.Systems;

public class UnlockDetectionSystem : ModSystem
{
    const int MaxNpcs = 200;

    // A client gives the server this long to send the NPCs already in the world.
    const double ClientWarmupSeconds = 3;

    readonly bool[] active = new bool[MaxNpcs];
    readonly int[] types = new int[MaxNpcs];
    bool ready;
    long readyAt;

    void Reset()
    {
        Array.Clear(active);
        Array.Clear(types);
        ready = false;
        readyAt = Stopwatch.GetTimestamp() + (Main.netMode == NetmodeID.MultiplayerClient ? (long)(ClientWarmupSeconds * Stopwatch.Frequency) : 0);
    }

    public override void OnWorldLoad() => Reset();

    public override void OnWorldUnload() => Reset();

    public override void PostUpdateNPCs()
    {
        if (Main.dedServ)
            return;
        for (int i = 0; i < MaxNpcs; i++)
        {
            NPC npc = Main.npc[i];
            bool town = !npc.IsNull && npc.active && npc.townNPC;
            if (town)
            {
                int type = npc.type;
                if (ready && (!active[i] || types[i] != type))
                    AppearedSafe(type);
                active[i] = true;
                types[i] = type;
            }
            else
            {
                active[i] = false;
                types[i] = 0;
            }
        }
        if (Stopwatch.GetTimestamp() >= readyAt)
            ready = true;
    }

    static void AppearedSafe(int type) => Util.Safe("announcement", () => Appeared(type));

    static void Appeared(int type)
    {
        string key = Lore.NpcKey(type);
        int count = WorldUnlockData.Count(key);
        AnnouncerConfig config = AnnouncerConfig.Instance;
        bool show = config.Frequency switch
        {
            AnnounceFrequency.EveryTime => true,
            AnnounceFrequency.SetAmount => count < config.MaxAnnouncementsPerNPC,
            _ => count == 0,
        };
        if (!show)
            return;
        WorldUnlockData.Increment(key);
        (string title, string subtitle) = Lore.GetLore(type);
        Banner.Show(type, title, subtitle);
    }
}
