using System;
using System.Collections.Generic;

namespace WeaponOutLiteMod.Common;

public static class DrawDepthID
{
    public const int Back = -2;
    public const int OffHand = -1;
    public const int Hand = 0;
    public const int Front = 1;
}

public enum PoseGroup
{
    Unassigned = 0, Ignore = 1, Item = 10, Potion = 20, LargeItem = 50, VanityItem = 60, PowerTool = 70,
    Thrown = 90, ThrownThin = 95, Whips = 100, SmallMelee = 120, SmallTool = 125, LargeMelee = 140, LargeTool = 145,
    Spear = 150, Flail = 155, Rapier = 160, Yoyo = 165, Bow = 200, Pistol = 220, Gun = 240, GunManual = 250,
    Shotgun = 255, Repeater = 260, Launcher = 280, Staff = 300, MagicBook = 310, MagicItem = 320,
    GiantItem = 900, GiantWeapon = 920, GiantBow = 940, GiantGun = 950, GiantMagic = 960, GiantDamaging = 970,
}

public enum DrawItemPoseID
{
    Custom = -1, Unassigned = 0, None = 1,

    Back = 10, BackDownward = 20, BackUpright = 30, BackFlail = 40, BackBow = 50, WaistSheathe = 500, WaistHolster = 510,

    HoldInHand = 10000, HoldInHandFront = 10010, HoldReversedInHand = 10020, HoldSpellTome = 10030,

    HoldInOffHand = 20000, BackFlailShoulder = 20010, PoleOffHand = 20020, BackGunShoulder = 20030, HoldRifleOffHandUpright = 20040,

    CarryInBothHands = 30000, JoustingLance = 30050,

    FloatingBack = 40000, FloatingBackUpright = 40010, FloatingBackAimed = 40020, FloatingInFront = 40100,
    FloatingOffHand = 40110, FloatingOffHandAimed = 40120,
    FloatingOrbitClockwiseFast = 41000, FloatingOrbitAntiClockwiseFast = 41001,
    FloatingOrbitClockwiseSlow = 41010, FloatingOrbitAntiClockwiseSlow = 41011,

    StanceInHand = 50000, StanceMeleeReady = 50050, StanceMeleeUpright = 50060, StanceMeleeRaised = 50070,
    StanceMeleeTrail = 50080, StanceRapierFootForward = 50100, StanceRapierHold = 50110, StancePoleReady = 50200,
    StancePoleUpright = 50210, StanceFlailReady = 50220, StanceBowInHand = 50300, StancePistolCowboy = 50400,

    StancePoleShoulder = 60200, StancePistolHoldUp = 60400, StanceLauncherShoulder = 60450,

    StanceTwoHand = 70000, StanceTwoHandBrave = 70010, StanceTwoHandTail = 70020, StanceTwoHandHighlander = 70030,
    StanceTwoHandBerserk = 70040, StanceTwoHandGuard = 70050, StancePoleTwoHand = 70200, StanceMachineTool = 70250,
    StanceBowHunt = 70300, StanceRifleHipReady = 70400, StanceRifleHoldUp = 70410, StanceRiflePointDown = 70420,
    StanceRiflePointDown2 = 70430, StanceRifleBoltAction = 70440, StanceRiflePumpAction = 70450,
}

// The poses a player can choose. The values are consecutive so the option cycles in this order;
// PoseIDs.Labelled gives each one's DrawItemPoseID.
public enum LabelledItemPose
{
    None,
    Back,
    BackUpright,
    BackDownward,
    BackBow,
    Waist,
    WaistHolster,
    Hold,
    HoldForward,
    HoldReverse,
    HoldTome,
    OffHand,
    OffHandFlailShoulder,
    OffHandPole,
    OffHandShoulder,
    OffHandUpright,
    TwoHandCarry,
    TwoHandJoustingLance,
    FloatingBack,
    FloatingBackUpright,
    FloatingBackAimed,
    FloatingFront,
    FloatingOffHand,
    FloatingAimed,
    OrbitClockwiseFast,
    OrbitAntiClockFast,
    OrbitClockwiseSlow,
    OrbitAntiClockSlow,
    CombatHold,
    CombatReady,
    CombatUpright,
    CombatRaised,
    CombatTrail,
    CombatRapier,
    CombatRapierHold,
    CombatPoleUpright,
    CombatPoleReady,
    CombatFlailReady,
    CombatBowHold,
    CombatPistol,
    CombatOffHandPoleShoulder,
    CombatOffHandPistol,
    CombatOffHandShoulder,
    CombatTwoHand,
    CombatTwoHandBrave,
    CombatTwoHandTail,
    CombatTwoHandHighlander,
    CombatTwoHandBerserk,
    CombatTwoHandGuard,
    CombatTwoHandPoleTwoHand,
    CombatTwoHandPowerTool,
    CombatTwoHandBow,
    CombatTwoHandRifleHip,
    CombatTwoHandRifleUp,
    CombatTwoHandRifleDown,
    CombatTwoHandRifleDown2,
    CombatTwoHandBoltAction,
    CombatTwoHandPumpAction,
}

public static class PoseIDs
{
    // Indexed by LabelledItemPose, in the same order.
    public static readonly DrawItemPoseID[] Labelled =
    {
        DrawItemPoseID.None,
        DrawItemPoseID.Back,
        DrawItemPoseID.BackUpright,
        DrawItemPoseID.BackDownward,
        DrawItemPoseID.BackBow,
        DrawItemPoseID.WaistSheathe,
        DrawItemPoseID.WaistHolster,
        DrawItemPoseID.HoldInHand,
        DrawItemPoseID.HoldInHandFront,
        DrawItemPoseID.HoldReversedInHand,
        DrawItemPoseID.HoldSpellTome,
        DrawItemPoseID.HoldInOffHand,
        DrawItemPoseID.BackFlailShoulder,
        DrawItemPoseID.PoleOffHand,
        DrawItemPoseID.BackGunShoulder,
        DrawItemPoseID.HoldRifleOffHandUpright,
        DrawItemPoseID.CarryInBothHands,
        DrawItemPoseID.JoustingLance,
        DrawItemPoseID.FloatingBack,
        DrawItemPoseID.FloatingBackUpright,
        DrawItemPoseID.FloatingBackAimed,
        DrawItemPoseID.FloatingInFront,
        DrawItemPoseID.FloatingOffHand,
        DrawItemPoseID.FloatingOffHandAimed,
        DrawItemPoseID.FloatingOrbitClockwiseFast,
        DrawItemPoseID.FloatingOrbitAntiClockwiseFast,
        DrawItemPoseID.FloatingOrbitClockwiseSlow,
        DrawItemPoseID.FloatingOrbitAntiClockwiseSlow,
        DrawItemPoseID.StanceInHand,
        DrawItemPoseID.StanceMeleeReady,
        DrawItemPoseID.StanceMeleeUpright,
        DrawItemPoseID.StanceMeleeRaised,
        DrawItemPoseID.StanceMeleeTrail,
        DrawItemPoseID.StanceRapierFootForward,
        DrawItemPoseID.StanceRapierHold,
        DrawItemPoseID.StancePoleUpright,
        DrawItemPoseID.StancePoleReady,
        DrawItemPoseID.StanceFlailReady,
        DrawItemPoseID.StanceBowInHand,
        DrawItemPoseID.StancePistolCowboy,
        DrawItemPoseID.StancePoleShoulder,
        DrawItemPoseID.StancePistolHoldUp,
        DrawItemPoseID.StanceLauncherShoulder,
        DrawItemPoseID.StanceTwoHand,
        DrawItemPoseID.StanceTwoHandBrave,
        DrawItemPoseID.StanceTwoHandTail,
        DrawItemPoseID.StanceTwoHandHighlander,
        DrawItemPoseID.StanceTwoHandBerserk,
        DrawItemPoseID.StanceTwoHandGuard,
        DrawItemPoseID.StancePoleTwoHand,
        DrawItemPoseID.StanceMachineTool,
        DrawItemPoseID.StanceBowHunt,
        DrawItemPoseID.StanceRifleHipReady,
        DrawItemPoseID.StanceRifleHoldUp,
        DrawItemPoseID.StanceRiflePointDown,
        DrawItemPoseID.StanceRiflePointDown2,
        DrawItemPoseID.StanceRifleBoltAction,
        DrawItemPoseID.StanceRiflePumpAction,
    };

    public static readonly PoseGroup[] PoseGroupOrder = Enum.GetValues<PoseGroup>();

    static readonly Dictionary<string, PoseGroup> GroupsByName = new();
    static readonly Dictionary<string, DrawItemPoseID> PosesByName = new();

    static PoseIDs()
    {
        foreach (PoseGroup group in PoseGroupOrder)
            GroupsByName[group.ToString()] = group;
        foreach (DrawItemPoseID pose in Enum.GetValues<DrawItemPoseID>())
            PosesByName[pose.ToString()] = pose;
    }

    public static string PoseGroupName(PoseGroup value) => Enum.IsDefined(value) ? value.ToString() : nameof(PoseGroup.Unassigned);

    // A saved name that is missing or unknown counts as Unassigned.
    public static PoseGroup GroupNumber(string? name) => name != null && GroupsByName.TryGetValue(name, out PoseGroup group) ? group : PoseGroup.Unassigned;

    public static DrawItemPoseID PoseNumber(string? name) => name != null && PosesByName.TryGetValue(name, out DrawItemPoseID pose) ? pose : DrawItemPoseID.Unassigned;

    public static string PoseName(DrawItemPoseID value) => Enum.IsDefined(value) ? value.ToString() : nameof(DrawItemPoseID.Unassigned);

    public static bool TryFindLabelled(string name, out LabelledItemPose pose)
    {
        foreach (LabelledItemPose value in Enum.GetValues<LabelledItemPose>())
        {
            if (string.Equals(value.ToString(), name, StringComparison.OrdinalIgnoreCase))
            {
                pose = value;
                return true;
            }
        }
        pose = LabelledItemPose.None;
        return false;
    }

    public static bool TryFindPose(string name, out DrawItemPoseID pose)
    {
        foreach (KeyValuePair<string, DrawItemPoseID> entry in PosesByName)
        {
            if (string.Equals(entry.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                pose = entry.Value;
                return true;
            }
        }
        pose = DrawItemPoseID.Unassigned;
        return false;
    }
}
