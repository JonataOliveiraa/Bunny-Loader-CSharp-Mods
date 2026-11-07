using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ColoredDamageTypesMod.Common.Configs;
using ColoredDamageTypesMod.Common.GlobalProjectiles;
using ColoredDamageTypesMod.Content.DamageClasses;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ColoredDamageTypesMod.Common;

// The colors of one modded damage class.
public sealed class DamageEntry
{
    public const int Tooltip = 0, Damage = 1, Crit = 2, FieldCount = 3;

    public string Dtname = "";
    public readonly Color[] Colors = new Color[FieldCount];
}

public static class DamageTypes
{
    const string FileName = "coloreddamagetypes.CrossModDamageTypes.txt";

    static ColoredDamageTypesConfig Config => ColoredDamageTypesConfig.Instance;

    public static DamageClass Sentry => ModContent.GetInstance<SentryClass>();

    public static void Log(string text) => ModContent.GetInstance<ColoredDamageTypes>().Logger.Info(text);

    public static readonly List<DamageClass> DamageClasses = new();
    public static readonly Dictionary<string, DamageEntry> CrossModDamageConfig_Orig = new();
    public static readonly Dictionary<string, DamageEntry> CrossModDamageConfig = new();

    public static string KeyOf(DamageClass? cls) => cls == null ? "" : cls.FullName;

    // Projectile types whose class is fixed, whatever they report.
    static readonly Dictionary<int, Func<DamageClass>> Overrides = new()
    {
        [ProjectileID.SolarWhipSword] = () => DamageClass.Melee,
        [ProjectileID.DD2BallistraProj] = () => Sentry,
        [ProjectileID.Pygmy] = () => DamageClass.Summon,
        [ProjectileID.Pygmy2] = () => DamageClass.Summon,
        [ProjectileID.Pygmy3] = () => DamageClass.Summon,
        [ProjectileID.Pygmy4] = () => DamageClass.Summon,
        [ProjectileID.BabySpider] = () => Sentry,
        [ProjectileID.UFOLaser] = () => DamageClass.Summon,
    };
    static readonly HashSet<int> Bypass = new() { ProjectileID.Bullet, ProjectileID.Bee, ProjectileID.GiantBee };
    static readonly Dictionary<int, HashSet<int>> ItemShootFamily = new()
    {
        [ProjectileID.BeeArrow] = new HashSet<int> { ProjectileID.Bee, ProjectileID.GiantBee },
    };
    static readonly HashSet<int> SpawnedByWeapon = new() { ProjectileID.BookOfSkullsSkull };

    static DamageClass? ModdedClassOf(Item item)
    {
        if (DamageClasses.Count == 0)
            return null;
        DamageClass own = item.DamageType;
        DamageClass? counts = null;
        foreach (DamageClass cls in DamageClasses)
        {
            if (own == cls)
                return cls;
            if (item.CountsAsClass(cls))
                counts = cls;
        }
        return counts;
    }

    static DamageClass? ModdedClassOf(Projectile projectile)
    {
        if (DamageClasses.Count == 0)
            return null;
        DamageClass own = projectile.DamageType;
        DamageClass? counts = null;
        foreach (DamageClass cls in DamageClasses)
        {
            if (own == cls)
                return cls;
            if (projectile.CountsAsClass(cls))
                counts = cls;
        }
        return counts;
    }

    public static DamageClass GetItemType(Item item)
    {
        ColoredDamageTypesConfig config = Config;
        if (config.DebugMode && config.DebugModeFields)
            Log($"Item: {item.type} DamageType: {KeyOf(item.DamageType)} melee {item.melee} ranged {item.ranged} magic {item.magic} summon {item.summon} sentry {item.sentry}");

        DamageClass? modded = ModdedClassOf(item);
        if (modded != null)
            return modded;

        if (item.CountsAsClass(DamageClass.Melee) && !item.CountsAsClass(DamageClass.Magic) && !item.CountsAsClass(DamageClass.Throwing))
            return DamageClass.Melee;
        if (item.CountsAsClass(DamageClass.Ranged) && !item.CountsAsClass(DamageClass.Magic) && !item.CountsAsClass(DamageClass.Throwing))
            return DamageClass.Ranged;
        if (item.CountsAsClass(DamageClass.Magic))
            return DamageClass.Magic;
        if (item.CountsAsClass(DamageClass.Throwing))
            return DamageClass.Throwing;
        if (item.CountsAsClass(DamageClass.Summon))
            return DamageClass.Summon;
        if (item.CountsAsClass(DamageClass.SummonMeleeSpeed))
            return DamageClass.Summon;
        return DamageClass.Generic;
    }

    static int ParentKind(Projectile projectile)
    {
        int parent = projectile.type - 1;
        for (int j = 0; j < 1000; j++)
        {
            Projectile p = Main.projectile[j];
            if (!p.active || p.type != parent)
                continue;
            if (p.sentry)
                return 2;
            if (p.minion)
                return 1;
        }
        return 0;
    }

    // The type of the weapon that fired a projectile, or 0. Only the debug log uses it.
    static int WeaponOf(Projectile projectile, int spawnWeaponType)
    {
        if (Bypass.Contains(projectile.type))
            return 0;
        Player owner = Main.player[projectile.owner];
        if (owner.IsNull)
            return 0;
        Item held = owner.HeldItem;
        if (!held.IsNull && (held.shoot == projectile.type
            || (ItemShootFamily.TryGetValue(held.shoot, out HashSet<int>? family) && family.Contains(projectile.type))))
            return held.type;
        if (SpawnedByWeapon.Contains(projectile.type) && spawnWeaponType > 0)
            return spawnWeaponType;
        for (int i = 0; i < owner.inventory.Length; i++)
        {
            Item item = owner.inventory[i];
            if (item.shoot == projectile.type)
                return item.type;
        }
        return 0;
    }

    public static DamageClass GetProjectileType(Projectile projectile, ProjectileChanges? global)
    {
        if (Overrides.TryGetValue(projectile.type, out Func<DamageClass>? fixedClass))
            return fixedClass();

        if (Config.DebugMode)
        {
            int weapon = WeaponOf(projectile, global?.SpawnWeaponType ?? 0);
            Log($"Projectile: {projectile.type} weapon: {(weapon > 0 ? weapon.ToString() : "none")} DamageType: {KeyOf(projectile.DamageType)}");
        }

        DamageClass? modded = ModdedClassOf(projectile);
        if (modded != null)
            return modded;

        int parent;
        if (global != null)
        {
            global.ParentKind ??= ParentKind(projectile);
            parent = global.ParentKind.Value;
        }
        else
        {
            parent = ParentKind(projectile);
        }

        if (parent == 2 || projectile.sentry)
            return Sentry;
        if (parent == 1 || projectile.minion)
            return DamageClass.Summon;

        if (projectile.CountsAsClass(DamageClass.Melee) && !projectile.CountsAsClass(DamageClass.Magic) && !projectile.CountsAsClass(DamageClass.Throwing))
            return DamageClass.Melee;
        if (projectile.CountsAsClass(DamageClass.Ranged) && !projectile.CountsAsClass(DamageClass.Magic) && !projectile.CountsAsClass(DamageClass.Throwing))
            return DamageClass.Ranged;
        if (projectile.CountsAsClass(DamageClass.Magic))
            return DamageClass.Magic;
        if (projectile.CountsAsClass(DamageClass.Throwing))
            return DamageClass.Throwing;
        if (projectile.CountsAsClass(DamageClass.Summon))
            return DamageClass.Summon;
        if (projectile.CountsAsClass(DamageClass.SummonMeleeSpeed))
            return DamageClass.Summon;
        return DamageClass.Generic;
    }

    public static int ColorIdOf(DamageClass cls)
    {
        if (cls == DamageClass.Generic)
            return 0;
        if (cls == DamageClass.Melee)
            return 1;
        if (cls == DamageClass.Ranged)
            return 2;
        if (cls == DamageClass.Magic)
            return 3;
        if (cls == DamageClass.Summon)
            return 4;
        if (cls == Sentry)
            return 5;
        if (cls == DamageClass.Throwing)
            return 6;
        int i = DamageClasses.IndexOf(cls);
        return i != -1 ? 8 + i : 0;
    }

    public static DamageClass ClassOfColorId(int id)
    {
        switch (id)
        {
            case 0: return DamageClass.Generic;
            case 1: return DamageClass.Melee;
            case 2: return DamageClass.Ranged;
            case 3: return DamageClass.Magic;
            case 4: return DamageClass.Summon;
            case 5: return Sentry;
            case 6: return DamageClass.Throwing;
            default:
                int index = id - 8;
                return index >= 0 && index < DamageClasses.Count ? DamageClasses[index] : DamageClass.Generic;
        }
    }

    public static Color? CheckDamageColor(DamageClass cls, bool crit, bool sentry)
    {
        ColoredDamageTypesConfig c = Config;
        if (cls == DamageClass.Melee)
            return crit ? c.MeleeDamageCrit : c.MeleeDamage;
        if (cls == DamageClass.Ranged)
            return crit ? c.RangedDamageCrit : c.RangedDamage;
        if (cls == DamageClass.Magic)
            return crit ? c.MagicDamageCrit : c.MagicDamage;
        if (cls == DamageClass.Throwing)
            return crit ? c.ThrowingDamageCrit : c.ThrowingDamage;
        if (cls == Sentry)
            return crit ? c.SentryDamageCrit : c.SentryDamage;
        if (cls == DamageClass.Summon)
        {
            if (sentry)
                return crit ? c.SentryDamageCrit : c.SentryDamage;
            return crit ? c.SummonDamageCrit : c.SummonDamage;
        }
        if (cls == DamageClass.Generic)
            return null;

        if (!CrossModDamageConfig.TryGetValue(KeyOf(cls), out DamageEntry? entry))
            return null;
        return entry.Colors[crit ? DamageEntry.Crit : DamageEntry.Damage];
    }

    public static Color? TooltipColorOf(Item item)
    {
        ColoredDamageTypesConfig c = Config;
        DamageClass cls = GetItemType(item);
        if (cls == DamageClass.Melee)
            return c.TooltipMelee;
        if (cls == DamageClass.Ranged)
            return c.TooltipRanged;
        if (cls == DamageClass.Magic)
            return c.TooltipMagic;
        if (cls == DamageClass.Throwing)
            return c.TooltipThrowing;
        if (cls == DamageClass.Summon)
            return item.sentry ? c.TooltipSentry : c.TooltipSummon;
        if (cls != DamageClass.Generic && CrossModDamageConfig.TryGetValue(KeyOf(cls), out DamageEntry? entry))
            return entry.Colors[DamageEntry.Tooltip];
        return null;
    }

    static bool SameRgb(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B;

    static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    static Color? ParseHex(string value)
    {
        if (value.Length != 7 || value[0] != '#' || !int.TryParse(value.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int n))
            return null;
        return new Color((n >> 16) & 255, (n >> 8) & 255, n & 255, 255);
    }

    // A color from another mod's Call: "#RRGGBB", a Color, or three numbers.
    static Color? ColorOf(object? value)
    {
        switch (value)
        {
            case string text:
                return ParseHex(text);
            case Color color:
                return new Color((int)color.R, color.G, color.B, 255);
            case Array array when array.Length >= 3:
                int[] rgb = new int[3];
                for (int i = 0; i < 3; i++)
                {
                    if (array.GetValue(i) is not IConvertible number || number is string || number is bool)
                        return null;
                    rgb[i] = Math.Clamp(Convert.ToInt32(number, CultureInfo.InvariantCulture), 0, 255);
                }
                return new Color(rgb[0], rgb[1], rgb[2], 255);
            default:
                return null;
        }
    }

    static string DtName(string key)
    {
        string[] parts = key.Split('/', '.');
        return parts[^1];
    }

    public static bool AddDamageType(object? cls, object? tooltip, object? damage, object? crit)
    {
        if (cls is not DamageClass damageClass)
            return false;
        if (ColorOf(tooltip) is not Color tt || ColorOf(damage) is not Color dmg || ColorOf(crit) is not Color cr)
            return false;

        string key = KeyOf(damageClass);
        var entry = new DamageEntry { Dtname = DtName(key) };
        entry.Colors[DamageEntry.Tooltip] = tt;
        entry.Colors[DamageEntry.Damage] = dmg;
        entry.Colors[DamageEntry.Crit] = cr;
        if (!DamageClasses.Contains(damageClass))
            DamageClasses.Add(damageClass);
        CrossModDamageConfig_Orig[key] = entry;
        if (loaded)
            Build();
        return true;
    }

    static bool loaded;
    static readonly Dictionary<string, Color?[]> saved = new();
    static readonly Dictionary<string, Color[]> savedDefaults = new();
    static string file = "";

    static Color ModdedDefault(int field)
    {
        ColoredDamageTypesConfig c = Config;
        return field switch
        {
            DamageEntry.Tooltip => c.ModdedTooltip,
            DamageEntry.Damage => c.ModdedDamage,
            _ => c.ModdedDamageCrit,
        };
    }

    static bool Edited(string key, int field, Color value)
    {
        if (savedDefaults.TryGetValue(key, out Color[]? defaults))
            return !SameRgb(value, defaults[field]);
        return !SameRgb(value, ModdedDefault(field));
    }

    // Keeps what the player changed; the colors the mods registered fill the rest.
    static void Build()
    {
        CrossModDamageConfig.Clear();
        var defaults = new Dictionary<string, Color[]>();
        foreach (KeyValuePair<string, DamageEntry> pair in CrossModDamageConfig_Orig)
        {
            saved.TryGetValue(pair.Key, out Color?[]? mine);
            var entry = new DamageEntry { Dtname = pair.Value.Dtname };
            for (int field = 0; field < DamageEntry.FieldCount; field++)
            {
                Color? value = mine?[field];
                entry.Colors[field] = value is Color color && Edited(pair.Key, field, color) ? color : pair.Value.Colors[field];
            }
            CrossModDamageConfig[pair.Key] = entry;
            defaults[pair.Key] = (Color[])pair.Value.Colors.Clone();
        }
        savedDefaults.Clear();
        foreach (KeyValuePair<string, Color[]> pair in defaults)
            savedDefaults[pair.Key] = pair.Value;
        heldShown = null;
        if (CrossModDamageConfig.Count == 0 || file.Length == 0)
            return;
        try
        {
            var lines = new List<string>();
            foreach (KeyValuePair<string, DamageEntry> pair in CrossModDamageConfig)
                lines.Add($"color\t{pair.Key}\t{Hex(pair.Value.Colors[0])}\t{Hex(pair.Value.Colors[1])}\t{Hex(pair.Value.Colors[2])}");
            foreach (KeyValuePair<string, Color[]> pair in savedDefaults)
                lines.Add($"default\t{pair.Key}\t{Hex(pair.Value[0])}\t{Hex(pair.Value[1])}\t{Hex(pair.Value[2])}");
            File.WriteAllLines(file, lines);
        }
        catch (Exception e)
        {
            Log("Could not write " + file + ": " + e.Message);
        }
    }

    static void ReadFile()
    {
        saved.Clear();
        savedDefaults.Clear();
        try
        {
            if (!File.Exists(file))
                return;
            foreach (string line in File.ReadAllLines(file))
            {
                string[] parts = line.Split('\t');
                if (parts.Length != 5)
                    continue;
                Color? a = ParseHex(parts[2]), b = ParseHex(parts[3]), c = ParseHex(parts[4]);
                if (parts[0] == "color")
                    saved[parts[1]] = new[] { a, b, c };
                else if (parts[0] == "default" && a is Color da && b is Color db && c is Color dc)
                    savedDefaults[parts[1]] = new[] { da, db, dc };
            }
        }
        catch (Exception e)
        {
            Log(FileName + " is invalid, ignored: " + e.Message);
            saved.Clear();
            savedDefaults.Clear();
        }
    }

    public static void LoadModdedDamageTypes()
    {
        ColoredDamageTypesConfig config = Config;
        foreach (Mod mod in ModLoader.Mods)
        {
            foreach (DamageClass cls in mod.GetContent<DamageClass>())
            {
                if (cls == Sentry || DamageClasses.Contains(cls))
                    continue;
                AddDamageType(cls, config.ModdedTooltip, config.ModdedDamage, config.ModdedDamageCrit);
            }
        }

        file = Path.Combine(Main.SavePath ?? "", FileName);
        ReadFile();
        loaded = true;
        Build();
    }

    static string? heldShown;
    static readonly Color[] heldValues = new Color[DamageEntry.FieldCount];

    public static string? HeldModdedKey()
    {
        if (Main.gameMenu || DamageClasses.Count == 0)
            return null;
        Player player = Main.player[Main.myPlayer];
        if (player.IsNull)
            return null;
        Item item = player.HeldItem;
        if (item.IsNull || item.type <= 0 || item.damage <= 0)
            return null;
        DamageClass? cls = ModdedClassOf(item);
        return cls != null ? KeyOf(cls) : null;
    }

    // Shows the held weapon's colors in the Held Item Class section.
    public static void SyncHeldClass()
    {
        string? key = HeldModdedKey();
        if (key == null || key == heldShown)
            return;
        if (!CrossModDamageConfig.TryGetValue(key, out DamageEntry? entry))
            return;
        heldShown = key;
        ColoredDamageTypesConfig c = Config;
        c.HeldTooltip = heldValues[DamageEntry.Tooltip] = entry.Colors[DamageEntry.Tooltip];
        c.HeldDamage = heldValues[DamageEntry.Damage] = entry.Colors[DamageEntry.Damage];
        c.HeldDamageCrit = heldValues[DamageEntry.Crit] = entry.Colors[DamageEntry.Crit];
    }

    public static Color[]? HeldDefaults()
    {
        string? key = HeldModdedKey();
        return key != null && CrossModDamageConfig_Orig.TryGetValue(key, out DamageEntry? entry) ? entry.Colors : null;
    }

    // C# configs say something changed, not what: compare with what the section was showing.
    public static void OnHeldColorsChanged()
    {
        string? key = HeldModdedKey();
        if (key == null || key != heldShown || !CrossModDamageConfig.ContainsKey(key))
            return;
        ColoredDamageTypesConfig c = Config;
        bool changed = false;
        saved.TryGetValue(key, out Color?[]? mine);
        void Apply(int field, Color value)
        {
            if (SameRgb(value, heldValues[field]))
                return;
            heldValues[field] = value;
            mine ??= new Color?[DamageEntry.FieldCount];
            mine[field] = value;
            changed = true;
        }
        Apply(DamageEntry.Tooltip, c.HeldTooltip);
        Apply(DamageEntry.Damage, c.HeldDamage);
        Apply(DamageEntry.Crit, c.HeldDamageCrit);
        if (!changed)
            return;
        saved[key] = mine!;
        Build();
        heldShown = key;
    }
}
