using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;
using WeaponOutLiteMod.Common.Players;

namespace WeaponOutLiteMod.Common.Configs;

public class WeaponOutConfig : ModConfig
{
    public static WeaponOutConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    // OnChanged does not say which option changed, so this tells a change of ShowHeldItem.
    static bool lastShowHeldItem;

    [Header("PlayerHeader")]
    [DefaultValue(true)]
    public bool EnableWeaponOutVisuals;

    [DefaultValue(true)]
    public bool ShowHeldItem;

    [Range(0f, 10f)]
    [Increment(0.5f)]
    [DefaultValue(3f)]
    public float CombatDelayTimerMax;

    [DefaultValue(false)]
    public bool CombatStanceAlwaysOn;

    [DefaultValue(true)]
    public bool CombatStanceWhenHurt;

    [Header("ItemPoseHeader")]
    [DefaultValue(LabelledItemPose.Hold)]
    public LabelledItemPose SmallItemPose;

    [DefaultValue(LabelledItemPose.TwoHandCarry)]
    public LabelledItemPose LargeItemPose;

    [DefaultValue(LabelledItemPose.OffHand)]
    public LabelledItemPose VanityItemPose;

    [DefaultValue(LabelledItemPose.HoldForward)]
    public LabelledItemPose PotionPose;

    [DefaultValue(LabelledItemPose.CombatReady)]
    public LabelledItemPose SmallToolPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandBerserk)]
    public LabelledItemPose LargeToolPose;

    [Header("MeleePoseHeader")]
    [DefaultValue(LabelledItemPose.CombatReady)]
    public LabelledItemPose SmallMeleePose;

    [DefaultValue(LabelledItemPose.CombatTwoHand)]
    public LabelledItemPose LargeMeleePose;

    [DefaultValue(LabelledItemPose.CombatHold)]
    public LabelledItemPose RapierPose;

    [DefaultValue(LabelledItemPose.CombatPoleReady)]
    public LabelledItemPose SpearPose;

    [DefaultValue(LabelledItemPose.Hold)]
    public LabelledItemPose YoyoPose;

    [DefaultValue(true)]
    public bool YoyoHalfScale;

    [DefaultValue(LabelledItemPose.CombatFlailReady)]
    public LabelledItemPose FlailPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandPowerTool)]
    public LabelledItemPose PowerToolPose;

    [DefaultValue(LabelledItemPose.Hold)]
    public LabelledItemPose WhipPose;

    [Header("RangedPoseHeader")]
    [DefaultValue(LabelledItemPose.CombatUpright)]
    public LabelledItemPose ThrownPose;

    [DefaultValue(LabelledItemPose.Hold)]
    public LabelledItemPose ThrownThinPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandBow)]
    public LabelledItemPose BowPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandRifleDown)]
    public LabelledItemPose RepeaterPose;

    [DefaultValue(true)]
    public bool BowDrawAmmo;

    [DefaultValue(LabelledItemPose.Hold)]
    public LabelledItemPose PistolPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandRifleHip)]
    public LabelledItemPose GunPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandBoltAction)]
    public LabelledItemPose GunManualPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandPumpAction)]
    public LabelledItemPose ShotgunPose;

    [DefaultValue(LabelledItemPose.OffHandShoulder)]
    public LabelledItemPose LauncherPose;

    [Header("MagicPoseHeader")]
    [DefaultValue(LabelledItemPose.OffHandPole)]
    public LabelledItemPose StaffPose;

    [DefaultValue(LabelledItemPose.HoldTome)]
    public LabelledItemPose MagicBookPose;

    [DefaultValue(LabelledItemPose.FloatingOffHand)]
    public LabelledItemPose MagicItemPose;

    [Header("LocalGraphics")]
    [DefaultValue(true)]
    public bool EnableWeaponPhysics;

    [DefaultValue(true)]
    public bool EnableSheathingAnim;

    [DefaultValue(true)]
    public bool EnableItemScaling;

    [Range(0, 64)]
    [Increment(2)]
    [DefaultValue(50)]
    public int SmallSwordThreshold;

    [Range(0, 64)]
    [Increment(2)]
    [DefaultValue(36)]
    public int SmallGunThreshold;

    [Range(64, 256)]
    [Increment(2)]
    [DefaultValue(80)]
    public int GiantItemThreshold;

    [Range(0, 100)]
    [Increment(1)]
    [DefaultValue(100)]
    public int GiantItemScalePercent;

    [DefaultValue(LabelledItemPose.TwoHandCarry)]
    public LabelledItemPose GiantItemPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandBerserk)]
    public LabelledItemPose GiantWeaponPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandBow)]
    public LabelledItemPose GiantBowPose;

    [DefaultValue(LabelledItemPose.CombatTwoHandRifleUp)]
    public LabelledItemPose GiantGunPose;

    [DefaultValue(LabelledItemPose.FloatingAimed)]
    public LabelledItemPose GiantMagicPose;

    [DefaultValue(LabelledItemPose.OffHandPole)]
    public LabelledItemPose GiantDamagingPose;

    [Header("Experimental")]
    [DefaultValue(true)]
    public bool EnableMenuDisplay;

    [DefaultValue(true)]
    public bool EnableProjSpears;

    [DefaultValue(true)]
    public bool EnableProjYoyos;

    [Header("HoldOverride")]
    [DefaultValue(true)]
    public bool EnableWeaponQuickWeaponCategorisation;

    public static DrawItemPoseID PoseId(LabelledItemPose option)
    {
        int index = (int)option;
        return (uint)index < (uint)PoseIDs.Labelled.Length ? PoseIDs.Labelled[index] : DrawItemPoseID.None;
    }

    // Changes one option and saves it, from outside the config screen.
    public void SetShowHeldItem(bool value)
    {
        var pending = (WeaponOutConfig)Clone();
        pending.ShowHeldItem = value;
        SaveChanges(pending);
    }

    public override void OnLoaded() => lastShowHeldItem = ShowHeldItem;

    public override void OnChanged()
    {
        Classifier.InvalidateClassifier();
        if (ShowHeldItem == lastShowHeldItem)
            return;
        lastShowHeldItem = ShowHeldItem;
        Player player = Main.LocalPlayer;
        if (!player.IsNull && !string.IsNullOrEmpty(player.name))
            WeaponOutPlayer.Of(player).isShowingHeldItem = ShowHeldItem;
    }
}
