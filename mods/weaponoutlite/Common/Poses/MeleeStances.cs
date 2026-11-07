using System;
using Terraria;
using Terraria.ID;
using WeaponOutLiteMod.Common.Configs;
using static WeaponOutLiteMod.Common.PoseMath;
using static WeaponOutLiteMod.Common.Poses.StanceHelpers;
using ArmStretch = Terraria.Player.CompositeArmStretchAmount;
using ID = WeaponOutLiteMod.Common.DrawItemPoseID;

namespace WeaponOutLiteMod.Common.Poses;

public static class StanceHelpers
{
    const double Pi = Math.PI;

    // The largest whole number the original's timers could hold: past any animation's duration.
    public const double ShieldTimerValue = 9007199254740991;

    public static bool Resting(Player p, double timer) => timer == 0 || p.grapCount > 0 || p.pulley;

    public static void SheatheArm(Player p, int bodyFrame, double t, double scale, ArmStretch stretch)
    {
        double rotation = Anim.ArmRaiseLower(t) * scale;
        if (bodyFrame == 5)
            rotation = 1 - rotation;
        SetArmFront(p, true, stretch, Pi * rotation * p.direction);
    }

    public static PoseData SheatheBlend(PoseData data, in PoseData idle, Player p, double t, double height, double width, double originX, double originY)
    {
        if (t > 0.5)
        {
            data.ApplyFlip(p);
            if (width < height * 1.5 && height < width * 1.5)
                data.rotation -= MH.PiOver2 * 2;
            else
                data.SetOrigin(originX, originY, p);
        }
        return LerpData(data, idle, t);
    }

    public static void ShieldGuard(ref PoseData data, Player p)
    {
        if (p.shieldParryTimeLeft > 0)
            data.Move(8 - 8.0 / p.shieldParryTimeLeft, -4);
        else
            data.Move(8, -4);
        data.rotation += Pi * 0.5;
    }

    public static void ShieldArms(Player p, ArmStretch backStretch)
    {
        SetArmBack(p, true, backStretch, Pi * -0.5 * p.direction);
        SetArmFront(p, true, ArmStretch.Full, Pi * -0.8 * p.direction);
    }

    public static double ShieldTimer(Player p, double timer) => p.shieldRaised && timer != 0 ? ShieldTimerValue : timer;

    // A solid tile that is not a platform.
    public static bool TileSolid(int x, int y)
    {
        if (!WorldGen.InWorld(x, y))
            return false;
        Tile tile = Main.tile[x, y];
        if (!tile.active())
            return false;
        int type = tile.type;
        return Main.tileSolid[type] && !Main.tileSolidTop[type];
    }

    public static void TileDust(int x, int y) => WorldGen.KillTile_MakeTileDust(x, y, Main.tile[x, y]);
}

public class StanceInHand : OnWaistSheathe
{
    public override ID GetID() => ID.StanceInHand;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(20, timer) > 0.4)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(20, timer);
        if (t > 0)
            SheatheArm(p, bodyFrame, t, -0.375, t > 0.5 ? ArmStretch.ThreeQuarters : ArmStretch.Full);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.05, 0.9, p);
        if (bodyFrame == 0)
        {
            data.rotation += Pi * 0.5;
            data.Move(-8, 8);
        }
        else if (bodyFrame == 5)
        {
            data.rotation += Pi * -2.25;
            data.Move(-9, -7);
        }
        else if (bodyFrame > 5)
        {
            data.rotation += Pi * 0.35;
            data.Move(-5, 5).WithHandOffset(p);
        }
        else
        {
            data.Hide();
        }
        double t = Anim.EaseInEaseOutNormal(20, timer);
        if (t > 0)
        {
            data.Move((26 + width / 4) * t, -10 * t);
            data.rotation += Pi * 2;
            if (t > 0.5)
            {
                data.ApplyFlip(p);
                if (width < height * 1.5 && height < width * 1.5)
                    data.rotation -= Pi;
                else
                    data.SetOrigin(0.05, 0.5 - 0.35 * 2, p);
            }
            data = LerpData(data, idle, t);
        }
        return data;
    }
}

public class StanceMeleeReady : OnWaistSheathe
{
    public override ID GetID() => ID.StanceMeleeReady;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(20, timer) > 0.15)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(20, timer);
        if (t > 0)
        {
            SheatheArm(p, bodyFrame, t, -0.5, ArmStretch.ThreeQuarters);
            return bodyFrame;
        }
        if (p.shieldRaised)
            SetArmFront(p, true, ArmStretch.Quarter, Pi * 0.5 * p.direction);
        else if (bodyFrame > 5)
            SetArmFront(p, true, ArmStretch.Full, Pi * 0.125 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.5 - 0.4 * (width / height), 0.9, p).RotateFaceForward(p, height, width);
        data.rotation += 3 * MH.PiOver2;
        if (p.shieldRaised)
        {
            data.Move(-7, -3);
            data.rotation -= Pi * 0.5;
        }
        else if (bodyFrame == 0 || IsMountPoseActive(p))
        {
            data.Move(-8, 13);
            data.rotation += Pi * 0.6;
        }
        else if (bodyFrame > 5)
        {
            data.Move(-13, 10).WithWaistOffset(p);
            data.rotation += Pi * 0.75;
        }
        else if (bodyFrame == 5)
        {
            data.Move(-6, -8);
            data.rotation -= Pi * 2.25;
        }
        double t = Anim.EaseOutNormal(20, timer);
        if (t > 0)
        {
            data.Move(26 * t, -18 * t);
            data = SheatheBlend(data, idle, p, t, height, width, 0.5 - 0.4 * (width / height), 0.5 - 0.4 * 2);
        }
        return data;
    }
}

public class StanceMeleeTrail : OnWaistSheathe
{
    public override ID GetID() => ID.StanceMeleeTrail;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(20, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(20, timer);
        if (t > 0)
        {
            SheatheArm(p, bodyFrame, t, -0.5, ArmStretch.ThreeQuarters);
            return bodyFrame;
        }
        if (p.shieldRaised)
            SetArmFront(p, true, ArmStretch.Quarter, Pi * 0.5 * p.direction);
        else if (bodyFrame == 0)
            SetArmFront(p, true, ArmStretch.Full, Pi * 0.075 * p.direction);
        else if (bodyFrame == 5)
            SetArmFront(p, true, ArmStretch.Full, Pi * 0.25 * p.direction);
        else if (bodyFrame > 5)
            SetArmFront(p, true, ArmStretch.ThreeQuarters, Pi * 0.125 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.5 - 0.4 * (width / height), 0.9, p).ApplyFlip(p).RotateFaceForward(p, height, width);
        data.rotation += Pi;
        if (p.shieldRaised)
        {
            data.Move(-6, -6);
            data.rotation += Pi * 1.75;
        }
        else if (bodyFrame == 0)
        {
            data.Move(-4, 8);
            data.rotation += Pi * 1.75;
        }
        else if (bodyFrame > 5)
        {
            data.Move(-5, 6).WithWaistOffset(p);
            data.rotation += Pi * 1.825;
        }
        else if (bodyFrame == 5)
        {
            data.Move(-9, 6);
            data.rotation += Pi * 1.925;
        }
        return LerpData(data, idle, Anim.EaseOutNormal(20, timer));
    }
}

public class StanceMeleeRaised : OnWaistSheathe
{
    public override ID GetID() => ID.StanceMeleeRaised;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(20, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        return p.shieldRaised || p.bodyFrame.Y < 5 * p.bodyFrame.Height ? DrawDepthID.Front : DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(20, timer);
        if (t > 0)
        {
            SheatheArm(p, bodyFrame, t, -0.5, ArmStretch.ThreeQuarters);
            return bodyFrame;
        }
        if (bodyFrame == 0)
            SetArmFront(p, true, ArmStretch.Full, Pi * 0.1 * p.direction);
        else if (bodyFrame == 5)
            SetArmFront(p, true, ArmStretch.Full, Pi * 0.375 * p.direction);
        else if (bodyFrame > 5)
            SetArmFront(p, true, ArmStretch.ThreeQuarters, Pi * 0.125 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        double t = Anim.EaseOutNormal(20, timer);
        data.SetOrigin(0.5 - 0.4 * (width / height), 0.9, p);
        if (bodyFrame < 5 || t > 0)
            data.ApplyFlip(p);
        data.RotateFaceForward(p, height, width);
        data.rotation += Pi;
        if (p.shieldRaised)
        {
            data.Move(-5, 5);
            data.rotation += Pi * 2.3;
        }
        else if (bodyFrame == 0)
        {
            data.Move(-8, 8);
            data.rotation += Pi * 2.25;
        }
        else if (bodyFrame > 5)
        {
            data.Move(-10, 10).WithWaistOffset(p);
            data.rotation += Pi * 1.125;
        }
        else if (bodyFrame == 5)
        {
            data.Move(-16, 2);
            data.rotation += Pi * 1.375;
        }
        return LerpData(data, idle, t);
    }
}

public class StanceMeleeUpright : OnWaistSheathe
{
    public override ID GetID() => ID.StanceMeleeUpright;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(20, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        return p.bodyFrame.Y == 5 * p.bodyFrame.Height ? DrawDepthID.Hand : DrawDepthID.Front;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(20, timer);
        if (t > 0)
        {
            SheatheArm(p, bodyFrame, t, -0.5, ArmStretch.ThreeQuarters);
            return bodyFrame;
        }
        if (p.shieldRaised)
            SetArmFront(p, true, ArmStretch.Quarter, Pi * 0.5 * p.direction);
        else if (bodyFrame == 0)
            SetArmFront(p, true, ArmStretch.Full, Pi * 0.25 * p.direction);
        else if (bodyFrame > 5)
            SetArmFront(p, true, ArmStretch.ThreeQuarters, Pi * 0.3 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.5 - 0.4 * (width / height), 0.9, p).RotateFaceForward(p, height, width);
        data.rotation -= MH.PiOver2;
        if (p.shieldRaised)
        {
            data.Move(-5, -5);
            data.rotation -= Pi * 0.5;
        }
        else if (bodyFrame == 0)
        {
            data.Move(-9.5, 6.25);
        }
        else if (bodyFrame > 5)
        {
            data.Move(-9.5, 3.25).WithWaistOffset(p);
        }
        else if (bodyFrame == 5)
        {
            data.Move(-6, -8);
        }
        double t = Anim.EaseOutNormal(20, timer);
        if (t > 0)
        {
            data.Move(26 * t, -18 * t);
            data = SheatheBlend(data, idle, p, t, height, width, 0.5 - 0.4 * (width / height), 0.5 - 0.4 * 2);
        }
        return data;
    }
}

public abstract class RapierStance : OnWaistSheathe
{
    protected abstract double StandingArm { get; }

    public override int DrawDepth(Player p, Item i, double timer) => Resting(p, timer) ? base.DrawDepth(p, i, timer) : DrawDepthID.OffHand;

    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        HideShield(p);
        double sheathing = Anim.EaseInNormal(30, timer);
        if (bodyFrame == 0)
        {
            SetLegFrameIfIdle(p, 9);
            ArmStretch back = ArmStretch.Full;
            if (sheathing > 0)
                back = ArmStretch.ThreeQuarters;
            if (sheathing > 0.2)
                back = ArmStretch.Full;
            SetArmBack(p, true, back, Pi * (StandingArm + sheathing * 0.25) * p.direction);
        }
        if (bodyFrame > 5)
        {
            ArmStretch back = ArmStretch.ThreeQuarters;
            if (bodyFrame % 7 < 4)
                back = bodyFrame >= 14 ? ArmStretch.Quarter : ArmStretch.Full;
            SetArmBack(p, true, back, Pi * -0.375 * p.direction);
        }
        return bodyFrame;
    }

    protected static void StepOffset(ref PoseData data, int bodyFrame)
    {
        if (bodyFrame % 7 < 4)
        {
            if (bodyFrame >= 14)
                data.Move(-2, -1);
            else
                data.Move(2, 1);
        }
    }

    protected static PoseData Sheathe(PoseData data, in PoseData idle, Player p, double height, double width, double timer)
    {
        double t = Anim.LinearNormal(30, timer);
        if (t > 0)
        {
            data.Move(16 * t, -12 * t);
            data.rotation -= Pi * 2.75 * t;
            data = SheatheBlend(data, idle, p, t, height, width, 0.1, 0.5 - 0.4 * 2);
        }
        return data;
    }
}

public class StanceRapierFootForward : RapierStance
{
    protected override double StandingArm => -0.375;
    public override ID GetID() => ID.StanceRapierFootForward;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.1, 0.9, p).RotateFaceForward(p, height, width);
        data.rotation -= MH.PiOver4;
        if (bodyFrame == 0)
        {
            data.Move(14, 4);
            data.rotation += Pi * 0.3;
        }
        else if (bodyFrame > 5)
        {
            data.Move(12, 4).WithWaistOffset(p);
            StepOffset(ref data, bodyFrame);
            data.rotation += Pi * 0.25;
        }
        else if (bodyFrame == 5)
        {
            data.Move(7, -8);
            data.rotation += Pi * 0.175;
        }
        return Sheathe(data, idle, p, height, width, timer);
    }
}

public class StanceRapierHold : RapierStance
{
    protected override double StandingArm => -0.38;
    public override ID GetID() => ID.StanceRapierHold;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.05, 0.85, p).RotateFaceForward(p, height, width);
        data.rotation -= MH.PiOver4;
        if (bodyFrame == 0)
        {
            data.Move(14, 4);
        }
        else if (bodyFrame > 5)
        {
            data.Move(12, 3).WithWaistOffset(p);
            StepOffset(ref data, bodyFrame);
        }
        else if (bodyFrame == 5)
        {
            data.Move(7, -8);
            data.rotation += Pi * 0.15;
        }
        return Sheathe(data, idle, p, height, width, timer);
    }
}

public class StanceMeleeTwoHand : OnBack
{
    public override ID GetID() => ID.StanceTwoHand;
    static bool Rest(Player p, double timer) => Resting(p, timer) || p.shieldRaised;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Rest(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(30, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Rest(p, timer))
            return bodyFrame;
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        if (bodyFrame == 0 && t < 0.3)
        {
            SetLegFrameIfIdle(p, 9);
            SetArmBack(p, true, ArmStretch.ThreeQuarters, Pi * (-0.3 - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * (-0.2 - s) * p.direction);
        }
        else
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * (-0.15 - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * (-0.1 - s) * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Rest(p, timer))
            return idle;

        data.SetOrigin(0.1, 0.9, p);
        if (bodyFrame == 0)
        {
            data.Move(8, 10);
        }
        else if (bodyFrame >= 5)
        {
            data.Move(4, 13);
            data.rotation += Pi * 0.15;
            if (IsMountPoseActive(p))
                data.Move(2, -8);
        }
        else if (IsMountPoseActive(p))
        {
            data.Move(10, 4);
        }
        double t = Anim.OverEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(8 * Math.Sin(t * Pi), -height * Math.Sin(t * 0.5 * Pi));
            data = LerpData(data, idle, t);
        }
        return data.WithWaistOffset(p);
    }
}

public class StanceMeleeTwoHandBerserk : OnBack
{
    public override ID GetID() => ID.StanceTwoHandBerserk;
    static bool Rest(Player p, double timer) => timer == 0 || p.shieldRaised;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Rest(p, timer) || Anim.LinearNormal(30, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Back;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Rest(p, timer))
            return bodyFrame;
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        double s = 0;
        if (t > 0)
        {
            s = Anim.ArmRaiseLower(t);
            s = t > 0.5 ? s - 0.5 : s * 0.5;
        }
        SetArmBack(p, true, ArmStretch.Full, Pi * (-0.3 - s) * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Rest(p, timer))
            return idle;

        data.SetOrigin(0.1, 0.9, p).Move(14, 2);
        data.rotation += Pi * -0.5;
        double t = Anim.OverEaseNormal(30, timer);
        if (t > 0)
        {
            data.Move(16 * Math.Sin(t * Pi), -24 * Math.Sin(t * Pi * 0.5));
            data = LerpData(data, idle, t);
        }
        return data.WithWaistOffset(p);
    }
}

public class StanceMeleeTwoHandBrave : OnBack
{
    public override ID GetID() => ID.StanceTwoHandBrave;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        timer = ShieldTimer(p, timer);
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(30, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        double vX = p.velocity.X, vY = p.velocity.Y;
        if (WeaponOutConfig.Instance.EnableWeaponPhysics)
        {
            if (vY != 0 && vY < 2)
                return DrawDepthID.Back;
            if (p.bodyFrame.Y >= p.bodyFrame.Height * 5 && vY == 0)
            {
                if (Clamp(vX * p.direction * 0.625, 0, 1) > 0.7)
                    return DrawDepthID.Back;
            }
        }
        else if (p.bodyFrame.Y >= p.bodyFrame.Height)
        {
            return DrawDepthID.Back;
        }
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        double vX = p.velocity.X, vY = p.velocity.Y;
        if (p.shieldRaised)
        {
            ShieldArms(p, ArmStretch.ThreeQuarters);
        }
        else if (bodyFrame == 0 && t < 0.4)
        {
            SetLegFrameIfIdle(p, 9);
            SetArmBack(p, true, ArmStretch.Full, Pi * (-0.15 - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * (-0.15 - s) * p.direction);
        }
        else if (vY == 0)
        {
            double motionNormal = 1;
            if (WeaponOutConfig.Instance.EnableWeaponPhysics)
                motionNormal = Clamp(Math.Max(vX * p.direction - 1, 0) * 0.625, 0, 1);
            double physics = -motionNormal * 0.25;
            ArmStretch back = motionNormal > 0.2 ? ArmStretch.Quarter : ArmStretch.Full;
            SetArmBack(p, true, back, Pi * (-0.1 - physics - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * (-0.15 + physics - s) * p.direction);
        }
        else
        {
            double motionNormal = 0;
            if (WeaponOutConfig.Instance.EnableWeaponPhysics)
                motionNormal = Clamp(vY * 0.1, -1, 1);
            double physics = -motionNormal * 0.15;
            SetArmBack(p, true, ArmStretch.Full, Pi * (-1 - physics * 1.5 - s * 0.75) * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * (-1.1 - physics - s * 0.5) * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.1, 0.9, p);
        double vX = p.velocity.X, vY = p.velocity.Y;
        if (p.shieldRaised)
        {
            timer = ShieldTimerValue;
            ShieldGuard(ref data, p);
        }
        else if (bodyFrame == 0 || IsMountPoseActive(p))
        {
            data.Move(4, 14);
            data.rotation += Pi * 0.2;
        }
        else if (bodyFrame >= 5)
        {
            if (vY == 0)
            {
                data.Move(4, 14);
                data.rotation += Pi * 0.2;
                double motionNormal = 1;
                if (WeaponOutConfig.Instance.EnableWeaponPhysics)
                    motionNormal = Clamp(Math.Max(vX * p.direction - 1, 0) * 0.625, 0, 1);
                data.Move(8 * motionNormal, -10 * motionNormal);
                data.rotation -= 3.25 * motionNormal;
            }
            else
            {
                data.Move(-6, -12);
                data.rotation += Pi * -0.75;
                double motionNormal = 0;
                if (WeaponOutConfig.Instance.EnableWeaponPhysics)
                    motionNormal = Clamp(vY * 0.1, -1, 1);
                data.Move(6 * (motionNormal + 1), motionNormal + 1);
                data.rotation += 0.5 * motionNormal;
            }
        }
        double t = Anim.OverEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(24 * Math.Sin(t * Pi), -height * Math.Sin(t * Pi * 0.5));
            data = LerpData(data, idle, t);
        }
        return data.WithWaistOffset(p);
    }
}

public class StanceMeleeTwoHandGuard : OnBack
{
    public override ID GetID() => ID.StanceTwoHandGuard;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        timer = ShieldTimer(p, timer);
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(30, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        if (p.shieldRaised)
        {
            ShieldArms(p, ArmStretch.Quarter);
        }
        else if (bodyFrame == 0 && t < 0.4)
        {
            SetLegFrameIfIdle(p, 17);
            SetArmBack(p, true, ArmStretch.Full, Pi * (-0.2 - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.ThreeQuarters, Pi * (-0.5 - s) * p.direction);
        }
        else
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * (-0.1 - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.None, Pi * (-0.25 - s) * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.1, 0.9, p);
        if (p.shieldRaised)
        {
            timer = ShieldTimerValue;
            ShieldGuard(ref data, p);
        }
        else if (bodyFrame == 0 || IsMountPoseActive(p))
        {
            data.Move(8, 6);
            data.rotation += Pi * 0.4;
        }
        else if (bodyFrame >= 5)
        {
            data.Move(2, 9);
            data.rotation += Pi * 0.3;
        }
        double t = Anim.OverEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(16 * Math.Sin(t * Pi), -height / 2 * Math.Sin(t * Pi));
            data = LerpData(data, idle, t);
        }
        return data.WithWaistOffset(p);
    }
}

public class StanceMeleeTwoHandHighlander : OnBack
{
    public override ID GetID() => ID.StanceTwoHandHighlander;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        timer = ShieldTimer(p, timer);
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(30, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Back;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        if (p.shieldRaised)
        {
            ShieldArms(p, ArmStretch.Quarter);
        }
        else if (bodyFrame == 0 && t < 0.4)
        {
            SetLegFrameIfIdle(p, 17);
            SetArmBack(p, true, ArmStretch.None, Pi * (-0.6 - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * (-0.425 - s) * p.direction);
        }
        else
        {
            SetArmBack(p, true, ArmStretch.None, Pi * (-0.8 - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * (-0.475 - s) * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.1, 0.9, p);
        if (p.shieldRaised)
        {
            timer = ShieldTimerValue;
            ShieldGuard(ref data, p);
        }
        else if (bodyFrame == 0)
        {
            data.Move(11, 1);
            data.rotation += Pi * -0.23;
        }
        else if (bodyFrame >= 5 || IsMountPoseActive(p))
        {
            double motionNormal = 0;
            if (WeaponOutConfig.Instance.EnableWeaponPhysics)
                motionNormal = Clamp((double)p.velocity.X * p.direction * 0.1, -1, 1);
            data.Move(9, 2);
            data.rotation += Pi * -0.325 - motionNormal * 0.15;
        }
        double t = Anim.OverEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(-width * Math.Sin(t * Pi * 0.25), -(32 + height / 2) * Math.Sin(t * Pi * 0.5));
            data = LerpData(data, idle, t);
        }
        return data.WithWaistOffset(p);
    }
}

public class StanceMeleeTwoHandTail : OnBack
{
    public override ID GetID() => ID.StanceTwoHandTail;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || IsMountPoseActive(p) || Anim.LinearNormal(45, timer) > 0.2)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        double t = Anim.LinearNormal(45, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        if (bodyFrame == 0)
        {
            SetLegFrameIfIdle(p, 9);
            HideShield(p);
            SetArmBack(p, true, ArmStretch.Full, Pi * (-0.125 - s * 1.5) * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * (-0.15 - s) * p.direction);
        }
        else if (p.velocity.Y == 0)
        {
            SetArmFront(p, true, ArmStretch.Full, Pi * (0.1 - s) * p.direction);
        }
        else
        {
            HideShield(p);
            if (bodyFrame == 5)
            {
                SetArmBack(p, true, ArmStretch.Full, Pi * (-0.1 - s * 1.5) * p.direction);
                SetArmFront(p, true, ArmStretch.Full, Pi * (-0.175 - s) * p.direction);
            }
            else
            {
                SetArmBack(p, true, ArmStretch.ThreeQuarters, Pi * (-0.25 - s * 1.5) * p.direction);
                SetArmFront(p, true, ArmStretch.Full, Pi * (-0.25 - s) * p.direction);
            }
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        double t = Anim.OverEaseNormal(45, timer);
        data.SetOrigin(0.1, 0.9, p).RotateFaceForward(p, height, width);
        data.rotation -= MH.PiOver4;
        if (bodyFrame == 0)
        {
            data.Move(6, 10);
            data.rotation += Pi * 1.15;
        }
        else if (bodyFrame >= 5 || IsMountPoseActive(p))
        {
            if (p.velocity.Y == 0)
            {
                double length = Math.Sqrt(width * width + height * height);
                Microsoft.Xna.Framework.Vector2 mc = p.MountedCenter;
                double wx = mc.X + (-p.width / 2.0 - length * 0.8) * p.direction + 8 * p.direction;
                double wy = mc.Y + (p.height / 2.0) * p.gravDir + 8 * p.gravDir;
                int tx = (int)Math.Floor(wx / 16), ty = (int)Math.Floor(wy / 16);
                int lastX = tx, lastY = ty, drop = 0;
                bool solid = false;
                if (WeaponOutConfig.Instance.EnableWeaponPhysics)
                {
                    for (int k = 0; k < length / 32; k++)
                    {
                        int nx = tx + k * p.direction, ny = ty + k * (int)p.gravDir;
                        lastX = nx;
                        lastY = ny;
                        drop = k;
                        if (TileSolid(nx, ny))
                        {
                            solid = true;
                            break;
                        }
                    }
                    if (drop > 0)
                        length /= 1 + drop;
                }
                switch (bodyFrame)
                {
                    case 7: case 8: case 9: case 14: case 15: case 16: length = (length + 8) * 0.75; break;
                }
                double lengthScale = Math.Max((length - 20) / 5, 1);
                int chance = (int)(64 * 2 / Math.Truncate(Math.Max(lengthScale * Math.Abs((double)p.velocity.X), 4)));
                if (timer > 30 && solid && Main.rand.Next(chance) == 0)
                    TileDust(lastX, lastY);

                double lengthOffset = 0.35 - 0.35 / lengthScale;
                data.rotation += Pi * (0.88 + lengthOffset);
                double handleOffset = data.rotation - 3.6;
                data.Move(-4 + handleOffset * 6, 10 + handleOffset * 4);
            }
            else if (bodyFrame == 5)
            {
                data.Move(6, 12);
                data.rotation += Pi * 1.2;
            }
            else
            {
                data.Move(8, 8);
                data.rotation += Pi * 1.05;
            }
        }
        if (t > 0)
        {
            data.Move(26 * t, -18 * t + (width + height) * -0.5 * t);
            data = LerpData(data, idle, t);
        }
        return data.WithWaistOffset(p);
    }
}
