using System;
using System.Collections.Generic;
using System.Linq;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.ID;

namespace BestiaryDisplaysMod.Common;

public sealed class Creature
{
    public readonly string credit;
    public readonly int id;
    public readonly string name;
    public readonly BestiaryEntry entry;

    public Creature(string credit, int id, string name, BestiaryEntry entry)
    {
        this.credit = credit;
        this.id = id;
        this.name = name;
        this.entry = entry;
    }
}

public static class Bestiary
{
    const int MinNetId = -65;

    static Dictionary<string, int>? creditToNet;
    static List<Creature>? creatures;
    static readonly HashSet<int> broken = new();
    static readonly Dictionary<int, NPC> samples = new();
    static readonly HashSet<int> loaded = new();

    static string CreditOf(int netId)
    {
        var table = new GameDictionary(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds);
        if (table.IsNull || !table.TryGetValue(netId, out IntPtr credit) || credit == IntPtr.Zero)
            return "";
        return GameString.Read(credit) ?? "";
    }

    static int MaxNetId() => Math.Max((int)NPCID.Count, Main.npcFrameCount.Length);

    static Dictionary<string, int> CreditToNet()
    {
        if (creditToNet != null)
            return creditToNet;
        creditToNet = new Dictionary<string, int>();
        int max = MaxNetId();
        for (int id = MinNetId; id < max; id++)
        {
            string credit = CreditOf(id);
            if (credit.Length > 0)
                creditToNet.TryAdd(credit, id);
        }
        return creditToNet;
    }

    public static int? NetIdOf(string? creditId)
    {
        if (string.IsNullOrEmpty(creditId))
            return null;
        return CreditToNet().TryGetValue(creditId, out int id) ? id : null;
    }

    public static string NameOf(int netId) => Lang.GetNPCNameValue(netId) ?? "";

    public static string DisplayName(string creditId)
    {
        int? id = NetIdOf(creditId);
        if (id == null)
            return creditId;
        string name = NameOf(id.Value);
        return name.Length > 0 ? name : creditId;
    }

    static bool EntryUnlocked(BestiaryEntry entry) => Util.Safe("bestiary unlock", () => UnlockState.Of(entry) > 0, false);

    // The original walked the entries and read each one's NetId from an info element with no
    // proxy. The database's own table gives the same entries, by id, through the typed API.
    static List<Creature> FromDatabase()
    {
        var found = new List<Creature>();
        var table = new GameDictionary(Main.BestiaryDB._byNpcId);
        if (table.IsNull)
            return found;
        var seen = new HashSet<IntPtr>();
        int max = MaxNetId();
        for (int id = MinNetId; id < max; id++)
        {
            if (!table.TryGetValue(id, out IntPtr entry) || entry == IntPtr.Zero || !seen.Add(entry))
                continue;
            string credit = CreditOf(id);
            string name = NameOf(id);
            if (credit.Length > 0 && name.Length > 0)
                found.Add(new Creature(credit, id, name, BestiaryEntry.From(new GameObject(entry))));
        }
        return found;
    }

    static List<Creature> FromCredits()
    {
        var found = new List<Creature>();
        foreach ((string credit, int id) in CreditToNet())
        {
            BestiaryEntry entry = Main.BestiaryDB.FindEntryByNPCID(id);
            string name = NameOf(id);
            if (!entry.IsNull && name.Length > 0)
                found.Add(new Creature(credit, id, name, entry));
        }
        return found;
    }

    public static List<Creature> Creatures(bool includeLocked)
    {
        if (creatures == null || creatures.Count == 0)
        {
            List<Creature> found = Util.Safe("bestiary list", FromDatabase, new List<Creature>());
            if (found.Count == 0)
                found = Util.Safe("list by credits", FromCredits, new List<Creature>());
            creatures = found.OrderBy(static c => c.name, StringComparer.InvariantCulture).ToList();
        }
        return includeLocked ? creatures.ToList() : creatures.Where(static c => EntryUnlocked(c.entry)).ToList();
    }

    public static NPC Sample(int netId)
    {
        if (samples.TryGetValue(netId, out NPC sample))
            return sample;
        var table = new GameDictionary(ContentSamples.NpcsByNetId);
        if (table.IsNull || !table.TryGetValue(netId, out IntPtr pointer) || pointer == IntPtr.Zero)
            return default;
        sample = NPC.From(new GameObject(pointer));
        samples[netId] = sample;
        return sample;
    }

    public static int DrawType(int netId)
    {
        NPC sample = Sample(netId);
        return sample.IsNull ? netId : sample.type;
    }

    public static Texture2D Texture(int netId)
    {
        int type = DrawType(netId);
        if (type <= 0 || type >= TextureAssets.Npc.Length)
            return default;
        if (loaded.Add(type))
            Main.instance.LoadNPC(type);
        Texture2D tex = TextureAssets.Npc[type].Value;
        return !tex.IsNull && tex.Width > 0 ? tex : default;
    }

    public static int FrameCount(int netId)
    {
        int type = DrawType(netId);
        if (type < 0 || type >= Main.npcFrameCount.Length)
            return 1;
        return Math.Max(1, Main.npcFrameCount[type]);
    }

    public static Point FrameSize(int netId)
    {
        Texture2D tex = Texture(netId);
        if (tex.IsNull)
            return new Point(32, 32);
        return new Point(tex.Width, Math.Max(1, tex.Height / FrameCount(netId)));
    }

    public static Color? TintOf(int netId)
    {
        NPC sample = Sample(netId);
        if (sample.IsNull)
            return null;
        Color c = sample.color;
        return c.R != 0 || c.G != 0 || c.B != 0 || c.A != 0 ? c : null;
    }

    public sealed class Dummy
    {
        public readonly int netId;
        NPC npc;
        GameHandle? handle;

        public Dummy(int netId)
        {
            this.netId = netId;
            if (broken.Contains(netId))
                return;
            Util.Safe("creature " + netId, Create);
            if (npc.IsNull)
                broken.Add(netId);
        }

        void Create()
        {
            NPC created = NPC.New();
            // An object kept between frames: the game's collector does not see C# fields.
            handle = new GameHandle(created.Pointer);
            NPC sample = Sample(netId);
            if (sample.IsNull)
                created.SetDefaults(netId);
            else
                created.SetDefaults(netId, sample.GetMatchingSpawnParams());
            created.IsABestiaryIconDummy = true;
            if (NPCID.Sets.NPCBestiaryDrawOffset.TryGetValue(netId, out NPCID.Sets.NPCBestiaryDrawModifiers modifiers))
            {
                if (modifiers.Velocity != 0)
                    created.velocity = new Vector2(modifiers.Velocity, 0);
                if (modifiers.Direction.HasValue)
                    created.direction = modifiers.Direction.Value;
            }
            created.FindFrame();
            npc = created;
        }

        public void Advance()
        {
            if (npc.IsNull)
                return;
            try
            {
                npc.FindFrame();
            }
            catch (Exception)
            {
                broken.Add(netId);
                npc = default;
            }
        }

        public Rectangle? Frame => npc.IsNull ? null : npc.frame;

        public void Release()
        {
            handle?.Dispose();
            handle = null;
            npc = default;
        }
    }
}

static class UnlockState
{
    public static int Of(BestiaryEntry entry)
    {
        IBestiaryUICollectionInfoProvider provider = entry.UIInfoProvider;
        return provider.IsNull ? 0 : (int)provider.GetEntryUICollectionInfo().UnlockState;
    }
}
