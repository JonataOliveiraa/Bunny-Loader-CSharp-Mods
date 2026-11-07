using System;
using System.Collections.Generic;
using Terraria;

namespace WeaponOutLiteMod.Common.Poses;

public class Custom : Pose
{
    static readonly HashSet<Delegate> failed = new();

    public override DrawItemPoseID GetID() => DrawItemPoseID.Custom;

    public override int DrawDepth(Player p, Item i, double timer)
    {
        int drawDepth = 0;
        List<Func<Player, Item, int, double, int>> funcs = State.customDrawDepthFuncs;
        for (int k = 0; k < funcs.Count; k++)
            drawDepth = Call(funcs[k], drawDepth, p, i, timer);
        return drawDepth;
    }

    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        List<Func<Player, Item, int, double, int>> funcs = State.customUpdateIdleBodyFrameFuncs;
        for (int k = 0; k < funcs.Count; k++)
            bodyFrame = Call(funcs[k], bodyFrame, p, i, timer);
        return bodyFrame;
    }

    // These come from other mods: an error in one is logged once and leaves the value as it was.
    static int Call(Func<Player, Item, int, double, int> fn, int value, Player p, Item i, double timer)
    {
        try
        {
            return fn(p, i, value, timer);
        }
        catch (Exception e)
        {
            if (failed.Add(fn))
                State.Log("error in a registered Custom function: " + e);
            return value;
        }
    }
}

public static class ItemPoses
{
    static Dictionary<int, Pose>? poses;

    static Dictionary<int, Pose> Load()
    {
        Pose[] all =
        {
            new Unassigned(), new None(), new OnBack(), new OnBackBow(), new OnBackDownward(), new OnBackFlail(), new OnBackFlailShoulder(),
            new OnBackGunShoulder(), new OnBackUpright(), new OnWaistHolster(), new OnWaistSheathe(), new HoldInHand(), new HoldInHandFront(),
            new HoldReversedInHand(), new HoldSpellTome(), new HoldInOffHand(), new HoldRifleOffHandUpright(), new PoleOffHand(),
            new CarryInBothHands(), new JoustingLance(), new FloatingBack(), new FloatingBackUpright(), new FloatingBackAimed(),
            new FloatingInFront(), new FloatingOffHand(), new FloatingOffHandAimed(),

            new StanceInHand(), new StanceMeleeReady(), new StanceMeleeTrail(), new StanceMeleeRaised(), new StanceMeleeUpright(),
            new StanceRapierFootForward(), new StanceRapierHold(), new StanceMeleeTwoHand(), new StanceMeleeTwoHandBerserk(),
            new StanceMeleeTwoHandBrave(), new StanceMeleeTwoHandGuard(), new StanceMeleeTwoHandHighlander(), new StanceMeleeTwoHandTail(),

            new StancePoleReady(), new StancePoleShoulder(), new StancePoleTwoHand(), new StancePoleUpright(), new StanceFlailReady(),
            new StanceBowInHand(), new StanceBowHunt(), new StancePistolCowboy(), new StancePistolHoldUp(), new StanceRifleHipReady(),
            new StanceRifleHoldUp(), new StanceRiflePointDown(), new StanceRiflePointDown2(), new StanceRifleBoltAction(),
            new StanceRiflePumpAction(), new StanceLauncherShoulder(), new StanceMachineTool(),

            FloatingOrbit.AntiClockwiseFast(), FloatingOrbit.AntiClockwiseSlow(), FloatingOrbit.ClockwiseFast(), FloatingOrbit.ClockwiseSlow(),
            new Custom(),
        };
        var map = new Dictionary<int, Pose>(all.Length);
        foreach (Pose pose in all)
            map[(int)pose.GetID()] = pose;
        return map;
    }

    public static Dictionary<int, Pose> All() => poses ??= Load();

    public static Pose PoseOf(DrawItemPoseID id)
    {
        Dictionary<int, Pose> all = All();
        return all.TryGetValue((int)id, out Pose? pose) ? pose : all[(int)DrawItemPoseID.Unassigned];
    }
}
