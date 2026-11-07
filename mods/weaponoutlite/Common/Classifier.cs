using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using WeaponOutLiteMod.Common.Configs;
using WeaponOutLiteMod.Common.Poses;
using G = WeaponOutLiteMod.Common.PoseGroup;
using ID = WeaponOutLiteMod.Common.DrawItemPoseID;

namespace WeaponOutLiteMod.Common;

public struct PoseGroupData
{
    public G poseGroup;
    public Pose drawItemPose;
    public bool poseIsDefault;
}

public static class Classifier
{
    static bool GroupOption(G group, WeaponOutConfig c, out LabelledItemPose option)
    {
        switch (group)
        {
            case G.Item: option = c.SmallItemPose; return true;
            case G.LargeItem: option = c.LargeItemPose; return true;
            case G.VanityItem: option = c.VanityItemPose; return true;
            case G.Potion: option = c.PotionPose; return true;
            case G.PowerTool: option = c.PowerToolPose; return true;
            case G.Yoyo: option = c.YoyoPose; return true;
            case G.SmallMelee: option = c.SmallMeleePose; return true;
            case G.SmallTool: option = c.SmallToolPose; return true;
            case G.LargeMelee: option = c.LargeMeleePose; return true;
            case G.LargeTool: option = c.LargeToolPose; return true;
            case G.Thrown: option = c.ThrownPose; return true;
            case G.ThrownThin: option = c.ThrownThinPose; return true;
            case G.Spear: option = c.SpearPose; return true;
            case G.Rapier: option = c.RapierPose; return true;
            case G.Flail: option = c.FlailPose; return true;
            case G.Whips: option = c.WhipPose; return true;
            case G.Bow: option = c.BowPose; return true;
            case G.Repeater: option = c.RepeaterPose; return true;
            case G.Pistol: option = c.PistolPose; return true;
            case G.Gun: option = c.GunPose; return true;
            case G.GunManual: option = c.GunManualPose; return true;
            case G.Shotgun: option = c.ShotgunPose; return true;
            case G.Launcher: option = c.LauncherPose; return true;
            case G.Staff: option = c.StaffPose; return true;
            case G.MagicBook: option = c.MagicBookPose; return true;
            case G.MagicItem: option = c.MagicItemPose; return true;
            case G.GiantItem: option = c.GiantItemPose; return true;
            case G.GiantWeapon: option = c.GiantWeaponPose; return true;
            case G.GiantBow: option = c.GiantBowPose; return true;
            case G.GiantGun: option = c.GiantGunPose; return true;
            case G.GiantMagic: option = c.GiantMagicPose; return true;
            case G.GiantDamaging: option = c.GiantDamagingPose; return true;
        }
        option = default;
        return false;
    }

    public static Pose SelectItemPose(Player p, Item item)
    {
        PoseGroupData data = GetItemPoseGroupData(item);
        if (data.drawItemPose.GetID() != ID.Unassigned)
            return data.drawItemPose;
        if (data.poseGroup == G.Ignore)
            return ItemPoses.PoseOf(ID.None);
        WeaponOutConfig c = WeaponOutConfig.Instance;
        if (!GroupOption(data.poseGroup, c, out LabelledItemPose option))
            return data.drawItemPose;
        return ItemPoses.PoseOf(WeaponOutConfig.PoseId(option));
    }

    public static PoseGroupData GetItemPoseGroupData(Item item, bool allowOverride = true)
    {
        G poseGroup = G.Unassigned;
        ID drawItemPose = ID.Unassigned;
        bool poseIsDefault = false;
        int type = item.type;

        if (allowOverride)
        {
            if (State.priorityItemHoldPose.TryGetValue(type, out ID priority))
                drawItemPose = priority;
            if (Overrides.FindStyleOverride(type, out StyleOverride itemOverride))
            {
                drawItemPose = itemOverride.drawItemPose;
                poseGroup = itemOverride.poseGroup;
            }
        }

        if (drawItemPose == ID.Unassigned && poseGroup == G.Unassigned)
        {
            if (allowOverride && State.customItemHoldStyles.Contains(type))
            {
                drawItemPose = ID.Custom;
            }
            else if (State.priorityItemHoldGroups.TryGetValue(type, out G priorityGroup))
            {
                poseGroup = priorityGroup;
            }
            else if (State.compatibilityItemHoldGroups.TryGetValue(type, out G compatibilityGroup))
            {
                poseGroup = compatibilityGroup;
            }
            else
            {
                poseGroup = CachedDrawStyleType(item);
                poseIsDefault = true;
            }
        }
        return new PoseGroupData { poseGroup = poseGroup, drawItemPose = ItemPoses.PoseOf(drawItemPose), poseIsDefault = poseIsDefault };
    }

    static readonly Dictionary<(int, float), G> styleCache = new();
    static int cachedGiantItemThreshold = -1;
    static int cachedGiantItemScalePercent = -1;
    static int cachedSmallGunThreshold = -1;
    static int cachedSmallSwordThreshold = -1;
    static int cachedRevision = -1;

    public static void InvalidateClassifier() => styleCache.Clear();

    static G CachedDrawStyleType(Item item)
    {
        WeaponOutConfig c = WeaponOutConfig.Instance;
        if (c.GiantItemThreshold != cachedGiantItemThreshold || c.GiantItemScalePercent != cachedGiantItemScalePercent
            || c.SmallGunThreshold != cachedSmallGunThreshold || c.SmallSwordThreshold != cachedSmallSwordThreshold
            || Overrides.Revision != cachedRevision)
        {
            styleCache.Clear();
            cachedGiantItemThreshold = c.GiantItemThreshold;
            cachedGiantItemScalePercent = c.GiantItemScalePercent;
            cachedSmallGunThreshold = c.SmallGunThreshold;
            cachedSmallSwordThreshold = c.SmallSwordThreshold;
            cachedRevision = Overrides.Revision;
        }

        (int, float) key = (item.type, item.scale);
        if (styleCache.TryGetValue(key, out G group))
            return group;
        try
        {
            group = CalculateDrawStyleType(item);
        }
        catch (Exception e)
        {
            State.LogOnce("classify:" + item.type, "could not classify the item " + item.type + ": " + e);
            group = G.Unassigned;
        }
        if (group != G.Unassigned)
            styleCache[key] = group;
        return group;
    }

    // An empty handle when the type has no texture.
    public static Texture2D ItemTexture(int type)
    {
        TextureAssetArray assets = TextureAssets.Item;
        return (uint)type < (uint)assets.Length ? assets[type].Value : default;
    }

    public static int ItemFrames(int type)
    {
        if ((uint)type >= (uint)Main.itemAnimations.Length)
            return 1;
        Terraria.DataStructures.DrawAnimation animation = Main.itemAnimations[type];
        return animation.IsNull ? 1 : Math.Max(1, animation.FrameCount);
    }

    static bool DamageCounts(Item item, DamageClass cls)
    {
        DamageClass? damageType = item.DamageType;
        return damageType != null ? damageType.CountsAsClass(cls) : item.CountsAsClass(cls);
    }

    static bool UsesSound(Item item, LegacySoundStyle target)
    {
        LegacySoundStyle sound = item.UseSound;
        if (sound.IsNull || target.IsNull)
            return false;
        return sound == target || (sound.SoundId == target.SoundId && sound.Style == target.Style);
    }

    // Template projectiles, one per type: only their defaults are read (and the Prism colors, see Renderer).
    // The handle keeps each one alive: the game's collector does not see C# memory.
    static readonly Dictionary<int, GameHandle> projectileCache = new();

    public static Projectile GetProjectile(int type)
    {
        if (!projectileCache.TryGetValue(type, out GameHandle? handle))
        {
            Projectile created = Projectile.New();
            created.SetDefaults(type);
            handle = new GameHandle(created.Pointer);
            projectileCache[type] = handle;
        }
        return new Projectile(handle.Target);
    }

    static bool At(GameArray<bool> set, int type) => (uint)type < (uint)set.Length && set[type];

    static G CalculateDrawStyleType(Item item)
    {
        Texture2D texture = ItemTexture(item.type);
        if (texture.IsNull)
            return G.Unassigned;

        int frames = ItemFrames(item.type);
        double itemScale = item.scale;
        double w = texture.Width * itemScale;
        double h = texture.Height * itemScale / frames;

        WeaponOutConfig c = WeaponOutConfig.Instance;
        int giantItemThreshold = c.GiantItemThreshold;
        double giantItemScale = c.GiantItemScalePercent / 100.0;
        if (giantItemThreshold < Math.Max(w, h))
        {
            w *= giantItemScale;
            h *= giantItemScale;
        }

        if (giantItemThreshold < Math.Max(w, h))
        {
            if (DamageCounts(item, DamageClass.Melee) || DamageCounts(item, DamageClass.SummonMeleeSpeed))
                return G.GiantWeapon;
            if (DamageCounts(item, DamageClass.Ranged) && h > w * 1.25)
                return G.GiantBow;
            if (DamageCounts(item, DamageClass.Ranged))
                return G.GiantGun;
            if (DamageCounts(item, DamageClass.Magic) || DamageCounts(item, DamageClass.Summon))
                return G.GiantMagic;
            if (item.damage > 0)
                return G.GiantDamaging;
            return G.GiantItem;
        }

        if (At(ItemID.Sets.Yoyo, item.type))
            return G.Yoyo;
        bool[] spears = ItemID.Sets.Spears;
        if ((uint)item.type < (uint)spears.Length && spears[item.type])
            return G.Spear;
        if (item.shoot > 0 && GetProjectile(item.shoot).aiStyle == 19)
            return G.Spear;
        if (At(Item.staff, item.type) && !DamageCounts(item, DamageClass.Melee))
            return G.Staff;
        if (At(ProjectileID.Sets.IsAWhip, item.shoot))
            return G.Whips;

        if (item.damage > 0)
        {
            if (item.useStyle == ItemUseStyleID.HoldUp)
                return G.MagicItem;

            if (item.noUseGraphic)
            {
                if (!item.CountsAsClass(DamageClass.Ranged))
                {
                    if (item.useStyle == ItemUseStyleID.Thrust || item.useStyle == ItemUseStyleID.Rapier)
                        return G.Rapier;
                }

                if (item.CountsAsClass(DamageClass.SummonMeleeSpeed) && item.noMelee)
                    return G.Whips;

                if (item.useStyle == ItemUseStyleID.Shoot)
                {
                    if (item.CountsAsClass(DamageClass.Melee))
                    {
                        if (h <= w * 0.8)
                            return G.PowerTool;

                        if (UsesSound(item, SoundID.DD2_MonkStaffSwing))
                            return G.Spear;

                        if (item.channel && w == 30 && h == 26 && item.scale == 1 && item.noMelee
                            && item.DamageType == DamageClass.MeleeNoSpeed
                            && item.useAnimation == 25 && item.useTime == 25
                            && UsesSound(item, SoundID.Item1) && item.shootSpeed == 16)
                        {
                            return G.Yoyo;
                        }
                        Projectile p = GetProjectile(item.shoot);
                        int ai = p.aiStyle;
                        if (ai == 19 || (p.ownerHitCheck && ai != 15))
                            return G.Spear;
                        if (ai != 15 && !item.channel && item.shootSpeed > 0 && item.shootSpeed < 8)
                            return G.Spear;
                        // The original also tested ProjectileID.Sets.HeldProjDoesNotUsePlayerGfxOffY here.
                        // The phone's game has no such set: it always read false.
                        if (ai == 15)
                            return G.Flail;

                        if (p.extraUpdates >= 1)
                            return G.Whips;

                        return G.Flail;
                    }

                    if (w > h && w > c.SmallGunThreshold)
                    {
                        if (UsesSound(item, SoundID.Item36))
                            return G.Shotgun;
                        return item.autoReuse ? G.Gun : G.GunManual;
                    }

                    if (item.CountsAsClass(DamageClass.Magic))
                        return G.MagicItem;
                }

                if (w == h && w >= 40)
                    return G.Spear;

                if (h > w * 1.25 && h >= 32 && !item.CountsAsClass(DamageClass.Melee))
                    return G.Bow;

                if (h >= w * 1.5 || w >= h * 1.5)
                    return G.ThrownThin;
                return G.Thrown;
            }

            if (item.useStyle == ItemUseStyleID.Swing || item.useStyle == ItemUseStyleID.Thrust || item.useStyle == ItemUseStyleID.Rapier)
            {
                if (item.noMelee && DamageCounts(item, DamageClass.Magic))
                    return G.Staff;
                bool isTool = item.axe > 0 || item.pick > 0 || item.hammer > 0;
                if (h + w <= c.SmallSwordThreshold * 2)
                    return isTool ? G.SmallTool : G.SmallMelee;
                return isTool ? G.LargeTool : G.LargeMelee;
            }

            if (item.useStyle == ItemUseStyleID.Shoot)
            {
                if (w > h)
                {
                    if (w <= c.SmallGunThreshold)
                        return G.Pistol;
                    if (item.useAmmo == AmmoID.Arrow)
                        return G.Repeater;
                    if (item.useAmmo == AmmoID.Rocket)
                        return G.Launcher;
                    if (UsesSound(item, SoundID.Item36))
                        return G.Shotgun;
                    return item.autoReuse ? G.Gun : G.GunManual;
                }
                return item.CountsAsClass(DamageClass.Magic) ? G.MagicBook : G.Bow;
            }
        }

        if (item.healLife > 0 || item.healMana > 0 || (item.buffType != 0 && item.maxStack > 1))
            return G.Potion;

        if (item.vanity || item.accessory || item.dye != 0 || IsEquip(item))
            return G.VanityItem;

        if (w > 20 && h > 14)
            return G.LargeItem;

        return G.Item;
    }

    static bool IsEquip(Item item) =>
        item.headSlot != -1 || item.bodySlot != -1 || item.legSlot != -1 || item.handOnSlot != -1 || item.handOffSlot != -1
        || item.backSlot != -1 || item.frontSlot != -1 || item.shoeSlot != -1 || item.waistSlot != -1 || item.wingSlot != -1
        || item.shieldSlot != -1 || item.neckSlot != -1 || item.faceSlot != -1 || item.balloonSlot != -1 || item.beardSlot != -1;
}
