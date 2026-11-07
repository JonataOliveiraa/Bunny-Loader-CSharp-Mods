using System.ComponentModel;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader.Config;

namespace ColoredDamageTypesMod.Common.Configs;

public class ColoredDamageTypesConfig : ModConfig
{
    public static ColoredDamageTypesConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Toggles")]
    [DefaultValue(true)]
    public bool ChangeTooltipColor;

    [DefaultValue(true)]
    public bool ChangeDamageColor;

    [DefaultValue(true)]
    public bool ShowDamageNumbers;

    [DefaultValue(true)]
    public bool ShowMultiplayerDamageNumbers;

    [Header("Condense")]
    [Range(0, 100)]
    [Increment(1)]
    [DefaultValue(0)]
    public int CondenseDamageHits;

    [DefaultValue(true)]
    public bool ShowCondenseDamageOnKill;

    // #AA0000
    [Header("DamageColors")]
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "170, 0, 0, 255")]
    public Color MeleeDamage;

    // #FF0A32
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 10, 50, 255")]
    public Color MeleeDamageCrit;

    // #96E19B
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "150, 225, 155, 255")]
    public Color RangedDamage;

    // #1ECD5A
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "30, 205, 90, 255")]
    public Color RangedDamageCrit;

    // #78BEFF
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "120, 190, 255, 255")]
    public Color MagicDamage;

    // #3782FF
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "55, 130, 255, 255")]
    public Color MagicDamageCrit;

    // #A06E3C
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "160, 110, 60, 255")]
    public Color ThrowingDamage;

    // #DC8700
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "220, 135, 0, 255")]
    public Color ThrowingDamageCrit;

    // #FF73C3
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 115, 195, 255")]
    public Color SummonDamage;

    // #FA50FA
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "250, 80, 250, 255")]
    public Color SummonDamageCrit;

    // #9673FF
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "150, 115, 255, 255")]
    public Color SentryDamage;

    // #7846FF
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "120, 70, 255, 255")]
    public Color SentryDamageCrit;

    // #8CA08C
    [Header("TooltipColors")]
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "140, 160, 140, 255")]
    public Color TooltipDefense;

    // #EB1919
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "235, 25, 25, 255")]
    public Color TooltipMelee;

    // #00B43C
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "0, 180, 60, 255")]
    public Color TooltipRanged;

    // #0F87FF
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "15, 135, 255, 255")]
    public Color TooltipMagic;

    // #FF7819
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 120, 25, 255")]
    public Color TooltipThrowing;

    // #FF73C3
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 115, 195, 255")]
    public Color TooltipSummon;

    // #9673FF
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "150, 115, 255, 255")]
    public Color TooltipSentry;

    // #FFFFFF
    [Header("ModCompat")]
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 255, 255, 255")]
    public Color ModdedTooltip;

    // #FFA050
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 160, 80, 255")]
    public Color ModdedDamage;

    // #FF641E
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 100, 30, 255")]
    public Color ModdedDamageCrit;

    // These three show the class of the held weapon. A change only counts while holding a modded weapon.
    // #FFFFFF
    [Header("HeldClass")]
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 255, 255, 255")]
    public Color HeldTooltip;

    // #FFA050
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 160, 80, 255")]
    public Color HeldDamage;

    // #FF641E
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "255, 100, 30, 255")]
    public Color HeldDamageCrit;

    [Header("Debug")]
    [DefaultValue(false)]
    public bool DebugMode;

    [DefaultValue(false)]
    public bool DebugModeTooltips;

    [DefaultValue(false)]
    public bool DebugModeFields;

    [DefaultValue(false)]
    public bool DebugModeMultiplayer;

    public override void OnChanged() => DamageTypes.OnHeldColorsChanged();

    // The JavaScript version had this as a button: /cdt reset.
    public bool ResetHeldClass()
    {
        Color[]? defaults = DamageTypes.HeldDefaults();
        if (defaults == null)
            return false;
        var pending = (ColoredDamageTypesConfig)Clone();
        pending.HeldTooltip = defaults[DamageEntry.Tooltip];
        pending.HeldDamage = defaults[DamageEntry.Damage];
        pending.HeldDamageCrit = defaults[DamageEntry.Crit];
        SaveChanges(pending);
        return true;
    }

    // The JavaScript version had this as a button: /cdt pillar.
    public void ApplyPillarPreset()
    {
        var pending = (ColoredDamageTypesConfig)Clone();
        pending.TooltipMelee = pending.MeleeDamage = new Color(0xFE, 0x79, 0x02);
        pending.MeleeDamageCrit = new Color(0xFD, 0x3E, 0x03);
        pending.TooltipRanged = pending.RangedDamage = new Color(0x22, 0xDD, 0x97);
        pending.RangedDamageCrit = new Color(0x21, 0xA0, 0x8D);
        pending.TooltipMagic = pending.MagicDamage = new Color(0xFE, 0x7E, 0xE5);
        pending.MagicDamageCrit = new Color(0xFF, 0x1F, 0xAE);
        pending.TooltipSummon = pending.SummonDamage = new Color(0x88, 0xE2, 0xFF);
        pending.SummonDamageCrit = new Color(0x0E, 0x9A, 0xE6);
        pending.TooltipThrowing = pending.ThrowingDamage = new Color(0xA1, 0x72, 0x4A);
        pending.ThrowingDamageCrit = new Color(0xAF, 0xA5, 0x67);
        SaveChanges(pending);
    }
}
