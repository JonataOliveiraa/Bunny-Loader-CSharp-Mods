using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using static BossChecklistMod.Common.Data;
using static BossChecklistMod.Common.ModText;

namespace BossChecklistMod.Common;

public static class World
{
    public static HashSet<string> hidden = new();
    public static HashSet<string> marked = new();
    public static HashSet<string> downed = new();
}

public static class EntryUI
{
    public static bool hiddenMode;
}

// What a despawn or limb message knows about the NPC. A multiplayer client only has the
// type and the name; a callback from another mod then gets a fresh NPC of that type.
public sealed class ShownNpc
{
    public readonly int type;
    public readonly string FullName;
    readonly NPC npc;

    public ShownNpc(int type, string fullName, NPC npc = default)
    {
        this.type = type;
        FullName = fullName;
        this.npc = npc;
    }

    public NPC Npc
    {
        get
        {
            if (!npc.IsNull)
                return npc;
            NPC sample = NPC.New();
            sample.SetDefaults(type, default);
            return sample;
        }
    }
}

public sealed class DropInfo
{
    public int itemId, stackMin, stackMax;
    public float dropRate;
    public List<string> conditions = new();
    public bool shown = true;
}

public sealed class ItemSample
{
    public int type, headSlot = -1, shoot, buffType, mountType = -1, createTile = -1, placeStyle, rare;
    public string name = "";
    public bool expert, expertOnly, master, masterOnly, vanity;
}

// Keeps the insertion order the original's Map had.
public sealed class CollectibleMap
{
    public readonly List<int> Keys = new();
    readonly Dictionary<int, int> types = new();

    public int Count => Keys.Count;
    public bool Has(int id) => types.ContainsKey(id);
    public int Get(int id) => types.TryGetValue(id, out int type) ? type : -1;

    public void Set(int id, int type)
    {
        if (!types.ContainsKey(id))
            Keys.Add(id);
        types[id] = type;
    }

    public void SortByType()
    {
        List<int> sorted = Keys.OrderBy(id => types[id]).ToList();
        Keys.Clear();
        Keys.AddRange(sorted);
    }
}

public sealed class RegisteredMod
{
    public readonly int[] counts = new int[3];
    public string display;
    public Mod? mod;
    public bool iconSearched;
    public string? iconFile;

    public RegisteredMod(string display) => this.display = display;
}

// Reads the loose arguments of Mod.Call, which other mods pass as plain objects.
public static class EntryArgs
{
    static bool IsNumber(object? v) => v is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal or Enum;

    public static double Number(object? v) => IsNumber(v) ? Convert.ToDouble(v) : 0;

    public static List<int> ListOfInt(object? v)
    {
        var list = new List<int>();
        if (IsNumber(v))
        {
            list.Add(Convert.ToInt32(v));
        }
        else if (v is IEnumerable values and not string)
        {
            foreach (object? value in values)
            {
                if (IsNumber(value))
                    list.Add(Convert.ToInt32(value));
            }
        }
        return list;
    }

    public static Dictionary<string, object?> Dictionary(object? v)
    {
        var extra = new Dictionary<string, object?>();
        if (v is IDictionary values)
        {
            foreach (DictionaryEntry entry in values)
            {
                if (entry.Key is string key)
                    extra[key] = entry.Value;
            }
        }
        return extra;
    }

    public static Func<string>? TextOf(object? v)
    {
        switch (v)
        {
            case null:
                return null;
            case Func<string> text:
                return text;
            case Func<LocalizedText> localized:
                return () => localized().ToString();
            case string s:
                return () => HasKey(s) || s.Contains('.') ? Text(s) : s;
            case LocalizedText localized:
                return () => localized.ToString();
            default:
                return () => Convert.ToString(v) ?? "";
        }
    }

    // A message callback may take the NPC. Everything else is a text without it.
    public static Func<ShownNpc, string?>? MessageOf(object? v)
    {
        switch (v)
        {
            case null:
                return null;
            case Func<NPC, string> text:
                return npc => text(npc.Npc);
            case Func<NPC, LocalizedText> localized:
                return npc => localized(npc.Npc).ToString();
            default:
                Func<string>? plain = TextOf(v);
                return plain == null ? null : _ => plain();
        }
    }
}

public sealed class EntryInfo
{
    public readonly int type;
    public readonly string modSource, internalName, Key;
    public readonly double progression;
    public List<int> npcIDs;
    public readonly List<string> relatedEntries = new();
    public readonly List<DropInfo> loot = new();
    public readonly List<int> lootItemTypes = new();
    public readonly List<int> bagExclusives = new();
    public readonly CollectibleMap collectibles = new();
    public readonly Dictionary<int, Func<ShownNpc, string?>?> limbs = new();
    public readonly List<int> spawnItem;
    public readonly Action<SpriteBatch, Rectangle, Color>? customDrawing;
    public readonly bool vanilla;
    public string? invasionSet;

    public Func<ShownNpc, string?> despawnFn;
    public Func<Texture2D>? portrait;
    public Func<List<Texture2D>>? headFn;
    public readonly object? overrideHeads;

    readonly Func<bool> downedFn;
    readonly Func<string> nameFn, spawnFn;
    readonly Func<bool> availableFn;
    readonly string? modDisplay;

    public EntryInfo(int type, string modSource, string internalName, double progression, object? downed, List<int> npcIDs, Dictionary<string, object?> extra)
    {
        if (downed is not Func<bool> downedCheck)
            throw new ArgumentException(L("LogMessage.DownedIsNull", modSource + " " + internalName));
        this.type = type;
        this.modSource = modSource;
        this.internalName = internalName;
        Key = modSource + " " + internalName;
        this.progression = progression;
        downedFn = downedCheck;
        this.npcIDs = npcIDs;
        extra.TryGetValue("displayName", out object? displayName);
        extra.TryGetValue("spawnInfo", out object? spawnInfo);
        nameFn = EntryArgs.TextOf(displayName) ?? (() => Tracker.SpaceCaps(internalName));
        spawnFn = EntryArgs.TextOf(spawnInfo) ?? (() => L("BossSpawnInfo.Unknown"));

        extra.TryGetValue("collectibles", out object? col);
        if (col is IDictionary map)
        {
            foreach (DictionaryEntry entry in map)
                collectibles.Set(Convert.ToInt32(entry.Key), Convert.ToInt32(entry.Value));
        }
        else
        {
            foreach (int id in EntryArgs.ListOfInt(col))
            {
                if (!collectibles.Has(id))
                    collectibles.Set(id, GENERIC);
            }
        }

        extra.TryGetValue("limbs", out object? limbList);
        if (limbList is IDictionary limbMap)
        {
            foreach (DictionaryEntry entry in limbMap)
                limbs[Convert.ToInt32(entry.Key)] = EntryArgs.MessageOf(entry.Value);
        }
        else
        {
            foreach (int id in EntryArgs.ListOfInt(limbList))
                limbs[id] = null;
        }

        extra.TryGetValue("availability", out object? availability);
        availableFn = availability as Func<bool> ?? (() => true);
        extra.TryGetValue("spawnItems", out object? spawnItems);
        spawnItem = EntryArgs.ListOfInt(spawnItems);
        extra.TryGetValue("customPortrait", out object? customPortrait);
        customDrawing = customPortrait as Action<SpriteBatch, Rectangle, Color>;
        extra.TryGetValue("despawnMessage", out object? despawn);
        despawnFn = EntryArgs.MessageOf(despawn) ?? (_ => null);
        extra.TryGetValue("overrideHeadTextures", out overrideHeads);
        extra.TryGetValue("modDisplayName", out object? modDisplayName);
        modDisplay = modDisplayName as string;
        vanilla = modSource == "Terraria";
    }

    // The callbacks below come from other mods. Their labels are only built on a failure:
    // these run for every row of the book.
    void Report(string what, Exception e) => Util.LogOnce(what + " " + Key + Util.Message(e), what + " " + Key + " failed: " + Util.Message(e));

    public string DisplayName
    {
        get
        {
            try
            {
                return Resolve(nameFn());
            }
            catch (Exception e)
            {
                Report("name", e);
                return internalName;
            }
        }
    }

    public string DisplaySpawnInfo
    {
        get
        {
            try
            {
                return spawnFn();
            }
            catch (Exception e)
            {
                Report("spawn info", e);
                return L("BossSpawnInfo.Unknown");
            }
        }
    }

    public string ModDisplayName =>
        vanilla || modSource == "Unknown" ? modSource : StripTags(modDisplay ?? modSource);

    public bool Downed()
    {
        try
        {
            return downedFn();
        }
        catch (Exception e)
        {
            Report("downed", e);
            return false;
        }
    }

    public bool Available()
    {
        try
        {
            return availableFn();
        }
        catch (Exception e)
        {
            Report("available", e);
            return true;
        }
    }

    public bool hidden => World.hidden.Contains(Key);

    public bool MarkedAsDowned => World.marked.Contains(Key);

    public bool IsAutoDownedOrMarked => (Cfg.Log.AutomaticChecklist && Downed()) || MarkedAsDowned;

    public int GetIndex => Tracker.sorted.IndexOf(this);

    public bool IsUpNext => Tracker.FindNextEntry() == GetIndex;

    public int RecordIndex => Tracker.recordKeys.IndexOf(Key);

    public int TreasureBag
    {
        get
        {
            if (type == EVENT)
                return 0;
            foreach (int id in lootItemTypes)
            {
                if (ItemID.Sets.BossBag[id])
                    return id;
            }
            return 0;
        }
    }

    public int Relic
    {
        get
        {
            foreach (int id in collectibles.Keys)
            {
                if (collectibles.Get(id) == RELIC)
                    return id;
            }
            return 0;
        }
    }

    public int ExpertItem
    {
        get
        {
            foreach (int id in bagExclusives)
            {
                if (EXPERT_ITEMS.Contains(id) || Tracker.Sample(id).expert)
                    return id;
            }
            return 0;
        }
    }

    public List<int> CollectibleDrops
    {
        get
        {
            var drops = new List<int>();
            foreach (int id in lootItemTypes)
            {
                if (collectibles.Has(id) && !drops.Contains(id))
                    drops.Add(id);
            }
            return drops;
        }
    }

    public bool VisibleOnChecklist()
    {
        BossLogConfiguration log = Cfg.Log;
        if (log.OnlyShowBossContent && type != BOSS)
            return false;
        if (EntryUI.hiddenMode)
            return true;
        bool done = IsAutoDownedOrMarked;
        bool unsupported = modSource == "Unknown" && log.HideUnsupported;
        bool unavailable = !Available() && log.HideUnavailable && !done;
        if (unsupported || unavailable || hidden)
            return false;
        if (type == BOSS && log.FilterBosses == BossFilterMode.HideWhenCompleted && done)
            return false;
        if (type == BOSS)
            return true;
        EntryFilterMode filter = type == MINIBOSS ? log.FilterMiniBosses : log.FilterEvents;
        return filter == EntryFilterMode.HideWhenCompleted ? !done : filter != EntryFilterMode.Hide;
    }

    public bool VisibleOnPageContent()
    {
        if (Cfg.Log.ProgressiveChecklist && !IsAutoDownedOrMarked && !IsUpNext)
            return false;
        return VisibleOnChecklist();
    }

    public List<Texture2D> Heads()
    {
        if (headFn == null)
        {
            Texture2D blank = Tracker.BlankHead();
            return blank.IsNull ? new List<Texture2D>() : new List<Texture2D> { blank };
        }
        return Util.Safe("heads " + Key, () =>
        {
            List<Texture2D> heads = headFn();
            heads.RemoveAll(head => head.IsNull);
            return heads;
        }, new List<Texture2D>());
    }

    public Texture2D Portrait() => portrait != null ? portrait() : default;

    public string? DespawnMessage(ShownNpc npc)
    {
        MessageMode mode = Cfg.Feature.DespawnMessageType;
        if (mode == MessageMode.Unique)
        {
            string? text = Util.Safe("despawn " + Key, () => despawnFn(npc), null);
            if (!string.IsNullOrEmpty(text))
                return Resolve(text);
        }
        if (mode != MessageMode.Disabled)
            return L(Tracker.AnyAlive() ? "ChatMessages.Despawn.Generic" : "ChatMessages.Loss.Generic");
        return null;
    }

    public string? LimbMessage(ShownNpc npc)
    {
        MessageMode mode = Cfg.Feature.LimbMessages;
        int npcType = npc.type;
        if (mode == MessageMode.Unique)
        {
            limbs.TryGetValue(npcType, out Func<ShownNpc, string?>? fn);
            string? text = fn != null ? Util.Safe("limb " + Key, () => fn(npc), null) : null;
            if (!string.IsNullOrEmpty(text))
                return Resolve(text);
            if (LIMB_NAMES.TryGetValue(npcType, out string? limbName) && HasKey("ChatMessages.Defeated." + limbName))
                return L("ChatMessages.Defeated." + limbName);
        }
        if (mode != MessageMode.Disabled)
        {
            string suffix = npcType == 36 || npcType == 396 ? LIMB_NAMES[npcType] : "";
            return L("ChatMessages.Defeated.Generic" + suffix);
        }
        return null;
    }

    public Dictionary<string, object?> ToDictionary() => new()
    {
        ["key"] = Key,
        ["modSource"] = modSource,
        ["displayName"] = DisplayName,
        ["progression"] = progression,
        ["downed"] = (Func<bool>)Downed,
        ["isBoss"] = type == BOSS,
        ["isMiniboss"] = type == MINIBOSS,
        ["isEvent"] = type == EVENT,
        ["npcIDs"] = new List<int>(npcIDs),
        ["spawnInfo"] = (Func<string>)(() => DisplaySpawnInfo),
        ["spawnItems"] = new List<int>(spawnItem),
        ["treasureBag"] = TreasureBag,
        ["relic"] = Relic,
        ["dropRateInfo"] = loot.Select(d => new Dictionary<string, object?>
        {
            ["itemId"] = d.itemId,
            ["stackMin"] = d.stackMin,
            ["stackMax"] = d.stackMax,
            ["dropRate"] = d.dropRate,
            ["conditions"] = new List<string>(d.conditions),
            ["shown"] = d.shown,
        }).ToList(),
        ["loot"] = new List<int>(lootItemTypes),
        ["collectibles"] = new List<int>(collectibles.Keys),
    };

    public override string ToString() => progression + " " + Key;
}

public static class Tracker
{
    public static readonly List<EntryInfo> sorted = new();
    public static readonly Dictionary<string, RegisteredMod> registeredMods = new();
    public static readonly HashSet<int> entryCache = new();
    public static readonly HashSet<int> lootCache = new();
    public static List<string> recordKeys = new();
    public static bool finalized;

    static readonly List<(string kind, string mod, Dictionary<string, object?> values)> orphans = new();
    static readonly Dictionary<string, EntryInfo> byKey = new();
    static readonly Dictionary<int, EntryInfo> limbOf = new();
    static readonly Dictionary<int, EntryInfo> bossOf = new();

    static double nextFrame = -1;
    static bool nextDirty;
    static int nextAll = -1;
    static readonly int[] nextBy = { -1, -1, -1 };

    static readonly Regex Capitals = new("([A-Z])");

    public static string SpaceCaps(string s) => Capitals.Replace(s, " $1").Trim();

    static readonly Dictionary<string, Asset<Texture2D>?> assets = new();

    // Paths come with or without an extension, and a mod's own path needs no mod name.
    static Asset<Texture2D>? FindTexture(string path)
    {
        int dot = path.LastIndexOf('.');
        if (dot > path.LastIndexOf('/'))
            path = path[..dot];
        int slash = path.IndexOf('/');
        bool named = slash > 0 && (path[..slash] == "Terraria" || ModLoader.HasMod(path[..slash]));
        string name = named ? path : Util.ModId + "/" + path;
        if (ModContent.RequestIfExists(name, out Asset<Texture2D> asset))
            return asset;
        Util.LogOnce("texture " + name, "texture " + name + " was not found");
        return null;
    }

    public static Texture2D ModTexture(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return default;
        if (!assets.TryGetValue(path, out Asset<Texture2D>? asset))
            assets[path] = asset = FindTexture(path);
        return asset == null ? default : asset.Value;
    }

    public static Texture2D BossHeadTexture(int index)
    {
        if (index < 0 || index >= TextureAssets.NpcHeadBoss.Length)
            return default;
        Asset<Texture2D>? asset = TextureAssets.NpcHeadBoss[index];
        return asset == null ? default : asset.Value;
    }

    public static Texture2D BlankHead() => TextureAssets.NpcHead[0].Value;

    // Reading the table loads the texture when it is not loaded yet.
    public static Texture2D ItemTexture(int type) =>
        type > 0 && type < TextureAssets.Item.Length ? TextureAssets.Item[type].Value : default;

    static Texture2D ExtraTexture(int i)
    {
        if (i < 0 || i >= TextureAssets.Extra.Length)
            return default;
        Asset<Texture2D>? asset = TextureAssets.Extra[i];
        return asset == null ? default : asset.Value;
    }

    static int HeadIndex(int id) => id >= 0 && id < NPCID.Sets.BossHeadTextures.Length ? NPCID.Sets.BossHeadTextures[id] : -1;

    static List<Texture2D> OneHead(Texture2D texture) => new() { texture };

    static List<Texture2D> HeadsOfNpcs(EntryInfo e)
    {
        var heads = new List<Texture2D>();
        foreach (int id in e.npcIDs)
        {
            if (SKIP_HEADS.Contains(id))
                continue;
            Texture2D texture = BossHeadTexture(HeadIndex(id));
            if (!texture.IsNull)
                heads.Add(texture);
        }
        return heads.Count > 0 ? heads : OneHead(BlankHead());
    }

    static Func<List<Texture2D>> VanillaHeads(EntryInfo e, HeadSpec? head)
    {
        if (head == null)
            return () => HeadsOfNpcs(e);
        if (head.Item != 0)
            return () => OneHead(ItemTexture(head.Item));
        if (head.Extra != 0)
            return () => OneHead(ExtraTexture(head.Extra));
        if (head.Boss != -1)
            return () => OneHead(BossHeadTexture(head.Boss));
        if (head.Mod != null)
        {
            string path = "Assets/Resources/BossTextures/" + head.Mod;
            return () => OneHead(ModTexture(path));
        }
        if (head.Npcs != null)
            return () => head.Npcs.Select(id => BossHeadTexture(HeadIndex(id))).ToList();
        return () => OneHead(BlankHead());
    }

    static Func<List<Texture2D>> DefaultHeads(EntryInfo e) =>
        () => e.type == EVENT ? OneHead(BlankHead()) : HeadsOfNpcs(e);

    static Texture2D TextureOf(object? value) => value switch
    {
        string path => ModTexture(path),
        Texture2D texture => texture,
        Asset<Texture2D> asset => asset.Value,
        _ => default,
    };

    static IEnumerable<object?> Items(object? value) =>
        value is IEnumerable values and not string ? values.Cast<object?>() : new[] { value };

    static Func<List<Texture2D>> ResolveHeads(EntryInfo e)
    {
        object? o = e.overrideHeads;
        if (o == null)
            return DefaultHeads(e);
        if (o is Delegate fn)
        {
            return () =>
            {
                List<Texture2D> icons = Items(fn.DynamicInvoke()).Select(TextureOf).ToList();
                icons.RemoveAll(icon => icon.IsNull);
                return icons;
            };
        }
        List<object?> list = Items(o).ToList();
        return () =>
        {
            List<Texture2D> icons = list.Select(TextureOf).ToList();
            icons.RemoveAll(icon => icon.IsNull);
            return icons.Count > 0 ? icons : OneHead(BlankHead());
        };
    }

    static Func<Texture2D>? ResolvePortrait(object? value) => value == null ? null : () => TextureOf(value);

    static Func<ShownNpc, string?> VanillaDespawn(string key) => npc =>
    {
        if (!AnyAlive())
            return HasKey("ChatMessages.Loss." + key) ? "ChatMessages.Loss." + key : "ChatMessages.Loss.Generic";
        return Main.dayTime && DAY_DESPAWNERS.Contains(npc.type) ? "ChatMessages.Despawn.Day" : "ChatMessages.Despawn.Generic";
    };

    static Func<bool>? VanillaAvailable(string? available) => available switch
    {
        "corruption" => () => !WorldGen.crimson || Main.drunkWorld,
        "crimson" => () => WorldGen.crimson || Main.drunkWorld,
        "xmas" => () => Main.xMas,
        _ => null,
    };

    public static void Init()
    {
        if (sorted.Count > 0)
            return;
        foreach (VanillaEntry v in VANILLA)
        {
            var extra = new Dictionary<string, object?>
            {
                ["displayName"] = (Func<string>)(() =>
                {
                    string t = Resolve(v.name);
                    if (t.Length > 0 && t != v.name && !t.StartsWith("Mods.", StringComparison.Ordinal))
                        return t;
                    if (v.name.StartsWith("NPCName.", StringComparison.Ordinal))
                    {
                        string npcName = NPCName(v.ids[0]);
                        if (npcName.Length > 0)
                            return npcName;
                    }
                    return v.en;
                }),
                ["spawnInfo"] = (Func<string>)(() => L("BossSpawnInfo." + v.key)),
                ["spawnItems"] = SPAWN_ITEMS.TryGetValue(v.key, out int[]? items) ? items.ToArray() : Array.Empty<int>(),
                ["limbs"] = v.limbs ?? Array.Empty<int>(),
                ["availability"] = VanillaAvailable(v.available),
            };
            var e = new EntryInfo(v.type, "Terraria", v.key, v.prog, v.downed, v.ids.ToList(), extra);
            if (COLLECTIBLES.TryGetValue(v.key, out (int id, int type)[]? collectibles))
            {
                foreach ((int id, int type) in collectibles)
                {
                    if (!e.collectibles.Has(id))
                        e.collectibles.Set(id, type);
                }
            }
            if (v.despawn)
                e.despawnFn = VanillaDespawn(v.key);
            e.headFn = VanillaHeads(e, v.head);
            if (v.portrait != null)
                e.portrait = ResolvePortrait("Assets/Resources/BossTextures/" + v.portrait);
            e.invasionSet = v.set;
            sorted.Add(e);
        }
    }

    static void CountMod(string modName, int type)
    {
        if (modName == "Terraria" || modName == "Unknown")
            return;
        if (!registeredMods.TryGetValue(modName, out RegisteredMod? registered))
            registeredMods[modName] = registered = new RegisteredMod(modName);
        registered.counts[type]++;
    }

    public static EntryInfo AddEntry(int type, Mod mod, string internalName, double progression, object? downed, List<int> ids, Dictionary<string, object?> extra)
    {
        string modName = ModName(mod);
        CountMod(modName, type);
        if (registeredMods.TryGetValue(modName, out RegisteredMod? registered))
        {
            registered.mod = mod;
            registered.display = ModDisplay(mod) ?? modName;
        }
        string key = modName + " " + internalName;
        if (sorted.Any(e => e.Key == key))
            throw new InvalidOperationException(L("LogMessage.DuplicateEntry", modName, internalName));
        extra["modDisplayName"] = ModDisplay(mod);
        var entry = new EntryInfo(type, modName, internalName, progression, downed, ids, extra);
        entry.headFn = ResolveHeads(entry);
        extra.TryGetValue("portrait", out object? portrait);
        entry.portrait = ResolvePortrait(portrait);
        sorted.Add(entry);
        if (Cfg.Log.ModCallLogVerbose)
            Util.Log(L("LogMessage.RegisteredEntry", key));
        return entry;
    }

    public static void AddOrphan(string kind, Mod mod, IDictionary? values)
    {
        if (values == null)
        {
            Util.Log(L("LogMessage.OldCall_Orphan", kind, ModName(mod)));
            return;
        }
        var checkedValues = new Dictionary<string, object?>();
        foreach (DictionaryEntry entry in values)
        {
            string key = Convert.ToString(entry.Key) ?? "";
            if (!key.Contains(' '))
                throw new ArgumentException("Invalid entry key: " + key);
            checkedValues[key] = entry.Value;
        }
        orphans.Add((kind, ModDisplay(mod) ?? ModName(mod), checkedValues));
    }

    public static void Finalize_()
    {
        if (finalized)
            return;
        Util.Safe("event pools", FinalizeEventPools);
        Util.Safe("orphan data", FinalizeOrphans);
        Util.Safe("collectible types", FinalizeCollectibles);
        Util.Safe("loot tables", FinalizeLoot);
        FinalizeData();
    }

    static void FinalizeEventPools()
    {
        foreach (EntryInfo e in sorted)
        {
            if (e.invasionSet == null)
                continue;
            GameArray<bool> set = NPCID.Sets.BelongsToInvasionOldOnesArmy;
            var ids = new List<int>();
            for (int i = 0; i < set.Length; i++)
            {
                if (set[i])
                    ids.Add(i);
            }
            if (ids.Count > 0)
                e.npcIDs = ids;
        }
    }

    static void FinalizeOrphans()
    {
        foreach ((string kind, string mod, Dictionary<string, object?> values) in orphans)
        {
            int count = 0;
            foreach ((string key, object? value) in values)
            {
                EntryInfo? e = FindEntryFromKey(key);
                if (e == null)
                {
                    Util.Log(L("LogMessage.InvalidOrphanKey", kind, mod, key));
                    continue;
                }
                List<int> ids = EntryArgs.ListOfInt(value);
                count += ids.Count;
                if (kind == "SubmitEntryLoot")
                {
                    e.lootItemTypes.AddRange(ids);
                }
                else if (kind == "SubmitEntryCollectibles")
                {
                    foreach (int id in ids)
                    {
                        if (!e.collectibles.Has(id))
                            e.collectibles.Set(id, GENERIC);
                    }
                }
                else if (kind == "SubmitEntrySpawnItems")
                {
                    e.spawnItem.AddRange(ids);
                }
                else if (kind == "SubmitEventNPCs")
                {
                    if (e.type == EVENT)
                        e.npcIDs.AddRange(ids);
                    else
                        Util.Log(L("LogMessage.InvalidEventEntry", e.Key, kind));
                }
            }
            if (Cfg.Log.ModCallLogVerbose)
                Util.Log(L("LogMessage.SuccessfulOrphanData", mod, count, kind));
        }
    }

    static void FinalizeCollectibles()
    {
        foreach (EntryInfo e in sorted)
        {
            if (e.vanilla)
                continue;
            foreach (int id in e.collectibles.Keys.ToArray())
                e.collectibles.Set(id, CollectibleType(id, e.collectibles.Get(id)));
            e.collectibles.SortByType();
        }
    }

    static void FinalizeLoot()
    {
        ItemDropDatabase db = Main.ItemDropsDB;
        foreach (EntryInfo e in sorted)
        {
            foreach (int id in e.npcIDs)
            {
                List<DropInfo> rates = db.IsNull ? new List<DropInfo>() : Util.Safe("drops " + id, () => Report(db, id), new List<DropInfo>());
                e.loot.AddRange(rates);
                foreach (DropInfo r in rates)
                {
                    if (r.itemId > 0 && !e.lootItemTypes.Contains(r.itemId))
                        e.lootItemTypes.Add(r.itemId);
                }
                if (e.Key == "Terraria TorchGod")
                    e.lootItemTypes.Add(5043);
                else if (e.Key == "Terraria BrainofCthulhu")
                    e.lootItemTypes.Add(1329);
            }
            int bag = e.TreasureBag;
            if (bag != 0 && BAG_EXCLUSIVES.TryGetValue(bag, out int[]? exclusive))
            {
                foreach (int id in exclusive)
                {
                    if (!e.bagExclusives.Contains(id))
                        e.bagExclusives.Add(id);
                }
            }
            e.lootItemTypes.AddRange(e.bagExclusives);
        }
    }

    static void FinalizeData()
    {
        // OrderBy is stable, like the original's sort with its index tiebreak.
        List<EntryInfo> order = sorted.OrderBy(e => e.progression).ToList();
        sorted.Clear();
        sorted.AddRange(order);
        foreach (EntryInfo a in sorted)
        {
            foreach (EntryInfo b in sorted)
            {
                if (a == b || a.type == b.type)
                    continue;
                if (a.npcIDs.Any(id => b.npcIDs.Contains(id)) && !a.relatedEntries.Contains(b.Key))
                    a.relatedEntries.Add(b.Key);
            }
        }
        entryCache.Clear();
        lootCache.Clear();
        recordKeys = new List<string>();
        byKey.Clear();
        limbOf.Clear();
        bossOf.Clear();
        foreach (EntryInfo e in sorted)
        {
            byKey[e.Key] = e;
            if (e.type == BOSS)
                recordKeys.Add(e.Key);
            foreach (int id in e.npcIDs)
            {
                entryCache.Add(id);
                if (e.type == BOSS)
                    bossOf.TryAdd(id, e);
            }
            foreach (int id in e.limbs.Keys)
                limbOf.TryAdd(id, e);
            foreach (int id in e.lootItemTypes)
                lootCache.Add(id);
            foreach (int id in e.collectibles.Keys)
                lootCache.Add(id);
        }
        finalized = true;
        if (!Cfg.Log.ModCallLogVerbose)
            return;
        foreach ((string name, RegisteredMod r) in registeredMods)
        {
            var names = new[] { new List<string>(), new List<string>(), new List<string>() };
            foreach (EntryInfo e in sorted)
            {
                if (e.modSource == name)
                    names[e.type].Add(e.internalName);
            }
            if (names[0].Count > 0)
                Util.Log(L("LogMessage.RegisteredBosses", r.display, r.counts[0], "[" + string.Join(", ", names[0]) + "]"));
            if (names[1].Count > 0)
                Util.Log(L("LogMessage.RegisteredMiniBosses", r.display, r.counts[1], "[" + string.Join(", ", names[1]) + "]"));
            if (names[2].Count > 0)
                Util.Log(L("LogMessage.RegisteredEvents", r.display, r.counts[2], "[" + string.Join(", ", names[2]) + "]"));
        }
    }

    public static EntryInfo? FindEntryFromKey(string key)
    {
        if (byKey.TryGetValue(key, out EntryInfo? entry))
            return entry;
        return sorted.Find(e => e.Key == key);
    }

    public static EntryInfo? FindBossEntryByNPC(int type)
    {
        if (!entryCache.Contains(type))
            return null;
        return bossOf.TryGetValue(type, out EntryInfo? entry) ? entry : null;
    }

    public static EntryInfo? IsEntryLimb(int type) => limbOf.TryGetValue(type, out EntryInfo? entry) ? entry : null;

    public static int FindNextEntry(int? type = null)
    {
        double frame = Main.GameUpdateCount + (EntryUI.hiddenMode ? 0.5 : 0);
        if (nextFrame != frame || nextDirty)
        {
            nextFrame = frame;
            nextDirty = false;
            nextAll = -1;
            nextBy[0] = nextBy[1] = nextBy[2] = -1;
            for (int i = 0; i < sorted.Count; i++)
            {
                EntryInfo e = sorted[i];
                if (e.IsAutoDownedOrMarked || !e.Available() || !e.VisibleOnChecklist())
                    continue;
                if (nextAll < 0)
                    nextAll = i;
                if (nextBy[e.type] < 0)
                    nextBy[e.type] = i;
            }
        }
        return type == null ? nextAll : nextBy[type.Value];
    }

    public static void Invalidate() => nextDirty = true;

    public static bool IsRegisteredMusicBox(int type) => VANILLA_MUSIC_BOXES.Contains(type) || OTHERWORLD_MUSIC_BOXES.Contains(type);

    public static bool AnyAlive()
    {
        for (int i = 0; i < 255; i++)
        {
            Player p = Main.player[i];
            if (!p.IsNull && p.active && !p.dead)
                return true;
        }
        return false;
    }

    const int ExpertRare = -12, MasterRare = -13;
    static readonly Dictionary<int, ItemSample> samples = new();

    // Item.master and Item.masterOnly do not exist on the phone: the rarity tells, as in the original.
    public static ItemSample Sample(int type)
    {
        if (samples.TryGetValue(type, out ItemSample? sample))
            return sample;
        sample = Util.Safe("sample " + type, () =>
        {
            Item item = Item.New();
            item.SetDefaults(type, default);
            int rare = item.rare;
            return new ItemSample
            {
                type = type,
                name = item.Name ?? "",
                expert = item.expert,
                expertOnly = item.expertOnly,
                master = rare == MasterRare,
                masterOnly = false,
                headSlot = item.headSlot,
                vanity = item.vanity,
                shoot = item.shoot,
                buffType = item.buffType,
                mountType = item.mountType,
                createTile = item.createTile,
                placeStyle = item.placeStyle,
                rare = rare,
            };
        }, new ItemSample { type = type });
        samples[type] = sample;
        return sample;
    }

    static List<DropInfo> Report(ItemDropDatabase db, int npcId)
    {
        var report = new List<DropInfo>();
        List<IItemDropRule> rules = db.GetRulesForNPCID(npcId, false);
        if (rules == null || rules.Count == 0)
            return report;
        var list = new List<DropRateInfo>();
        var feed = new DropRateInfoChainFeed(1f);
        foreach (IItemDropRule rule in rules)
        {
            try
            {
                rule.ReportDroprates(list, feed);
            }
            catch (Exception e)
            {
                Util.LogOnce("droprates " + npcId, "drop rates of NPC " + npcId + " failed: " + Util.Message(e));
            }
        }
        foreach (DropRateInfo d in list)
        {
            var info = new DropInfo { itemId = d.itemId, stackMin = d.stackMin, stackMax = d.stackMax, dropRate = d.dropRate };
            if (d.conditions != null)
            {
                foreach (IItemDropRuleCondition c in d.conditions)
                {
                    info.conditions.Add(c.GetType().Name);
                    if (!c.CanShowItemDropInUI())
                        info.shown = false;
                }
            }
            report.Add(info);
        }
        return report;
    }

    static int CollectibleType(int id, int current)
    {
        ItemSample s = Sample(id);
        if (s.name.Length == 0 && s.type == 0)
            return current;
        if (s.headSlot > 0 && s.vanity)
            return MASK;
        if (IsRegisteredMusicBox(id))
            return MUSIC;
        bool pet = (Main.projPet[s.shoot] && Main.vanityPet[s.buffType]) || (ProjectileID.Sets.LightPet[s.shoot] && Main.lightPet[s.buffType]);
        if (s.master && (pet || s.mountType > -1))
            return PET;
        if (s.createTile > 0)
        {
            TileObjectData data = TileObjectData.GetTileData(s.createTile, s.placeStyle, 0);
            if (!data.IsNull)
            {
                int w = data.Width, h = data.Height;
                if (w == 3 && h == 3 && s.createTile == 240)
                    return TROPHY;
                if (s.master && w == 3 && h == 4)
                    return RELIC;
            }
            return GENERIC;
        }
        return GENERIC;
    }

    public static string ModName(Mod? mod) => mod == null ? "Unknown" : mod.Name;

    public static string? ModDisplay(Mod? mod) => mod == null ? null : mod.DisplayName;

    public static bool IsMasterOnly(EntryInfo e, int id) => id == e.Relic || e.collectibles.Get(id) == PET;

    public static string TypeName(int t) => t >= 0 && t < TYPE_NAMES.Length ? TYPE_NAMES[t] : "Boss";
}
