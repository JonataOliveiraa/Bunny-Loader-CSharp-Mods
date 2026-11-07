using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace TooltipIconMod.Common;

public static class TooltipIcons
{
    static readonly Dictionary<(string Mod, string Name), Icon> icons = new();

    static (string, string) Vanilla(string name) => ("Terraria", name);

    public static void Initialize()
    {
        if (icons.Count > 0)
            return;
        void Add(string name, Icon icon) => icons[Vanilla(name)] = icon;
        Add("Damage", new WeaponIcon("MeleeDamage", "MeleeDamage", "RangedDamage", "MagicDamage", "SummonDamage"));
        Add("CritChance", new WeaponIcon("CriticalChance", null, null, null, null));
        Add("Speed", new WeaponIcon("MeleeSpeed", "MeleeSpeed", "RangedSpeed", "MagicSpeed", null));
        Add("Knockback", new WeaponIcon("Knockback", null, null, null, null));
        Add("FishingPower", new NormalIcon("Tools/FishingPower"));
        var bait = new ItemIcon(2674, new Vector2(3, 3));
        Add("NeedsBait", bait);
        Add("BaitPower", bait);
        Add("Equipable", new EquipableIcon());
        var vanity = new NormalIcon("Equipables/Vanity");
        Add("Vanity", vanity);
        Add("VanityLegal", vanity);
        Add("Defense", new NormalIcon("Equipables/Defense"));
        Add("PickPower", new NormalIcon("Tools/PickaxePower"));
        Add("AxePower", new NormalIcon("Tools/AxePower"));
        Add("HammerPower", new NormalIcon("Tools/HammerPower"));
        Add("TileBoost", new TileBoostIcon());
        Add("HealLife", new NormalIcon("Consumables/HealLife"));
        Add("HealMana", new NormalIcon("Consumables/HealMana"));
        Add("UseMana", new NormalIcon("UseMana"));
        Add("Placeable", new PlaceableIcon());
        Add("Ammo", new NormalIcon("Consumables/Ammo"));
        Add("Consumable", new ConsumableIcon());
        Add("Material", new NormalIcon("Material"));
        Add("BuffTime", new BuffTimeIcon());
        Add("PrefixDamage", new WeaponPrefixIcon("MeleeDamage", "MeleeDamage", "RangedDamage", "MagicDamage", "SummonDamage"));
        Add("PrefixSpeed", new WeaponPrefixIcon("MeleeSpeed", "MeleeSpeed", "RangedSpeed", "MagicSpeed", null));
        Add("PrefixCritChance", new WeaponPrefixIcon("CriticalChance", null, null, null, null));
        Add("PrefixUseMana", new PrefixIcon("UseMana", true));
        Add("PrefixSize", new PrefixIcon("Weapons/Size"));
        Add("PrefixShootSpeed", new PrefixIcon("Weapons/ShootSpeed"));
        Add("PrefixKnockback", new WeaponPrefixIcon("Knockback", null, null, null, null));
        Add("PrefixAccDefense", new PrefixIcon("Equipables/Defense"));
        Add("PrefixAccMaxMana", new PrefixIcon("Consumables/HealMana"));
        Add("PrefixAccCritChance", new PrefixIcon("Weapons/CriticalChance"));
        Add("PrefixAccDamage", new PrefixIcon("Weapons/MeleeDamage"));
        Add("PrefixAccMoveSpeed", new PrefixIcon("Equipables/MoveSpeed"));
        Add("PrefixAccMeleeSpeed", new PrefixIcon("Weapons/MeleeSpeed"));
        Add("Expert", new FitIcon("Misc/Expert"));
        Add("Master", new FitIcon("Misc/Master"));
        Add("JourneyResearch", new FitIcon("Misc/Journey"));
    }

    public static bool HasIcon(string mod, string name) => icons.ContainsKey((mod, name));

    public static Icon? GetIcon(string mod, string name) => icons.TryGetValue((mod, name), out Icon? icon) ? icon : null;

    public static bool AddIcon(string mod, string name, Icon icon) => icons.TryAdd((mod, name), icon);

    static T Of<T>(string name) where T : Icon => (T)icons[Vanilla(name)];

    public static bool AddDamageClassIcons(DamageClass cls, object? damage, object? speed, object? knockback, object? crit)
    {
        return Of<WeaponIcon>("Damage").AddModDamageClassTexture(cls, damage)
            && Of<WeaponIcon>("Speed").AddModDamageClassTexture(cls, speed)
            && Of<WeaponIcon>("Knockback").AddModDamageClassTexture(cls, knockback)
            && Of<WeaponIcon>("CritChance").AddModDamageClassTexture(cls, crit)
            && Of<WeaponIcon>("PrefixDamage").AddModDamageClassTexture(cls, damage)
            && Of<WeaponIcon>("PrefixSpeed").AddModDamageClassTexture(cls, speed)
            && Of<WeaponIcon>("PrefixKnockback").AddModDamageClassTexture(cls, knockback)
            && Of<WeaponIcon>("PrefixCritChance").AddModDamageClassTexture(cls, crit);
    }

    public static void AddConsumableIcon(Func<Item, DrawableTooltipLine, bool> condition, object texture) => Of<ConsumableIcon>("Consumable").AddModIcon(condition, texture);

    public static void AddEquipableIcon(Func<Item, DrawableTooltipLine, bool> condition, object texture) => Of<EquipableIcon>("Equipable").AddModIcon(condition, texture);

    public static void AddBuffTimeIcon(Func<Item, DrawableTooltipLine, bool> condition, object texture) => Of<BuffTimeIcon>("BuffTime").AddModIcon(condition, texture);
}
