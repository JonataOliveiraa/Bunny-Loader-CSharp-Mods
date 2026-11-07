using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using WeaponOutLiteMod.Common.Configs;
using static WeaponOutLiteMod.Common.PoseMath;
using static WeaponOutLiteMod.Common.Poses.StanceHelpers;
using ArmStretch = Terraria.Player.CompositeArmStretchAmount;
using ID = WeaponOutLiteMod.Common.DrawItemPoseID;

namespace WeaponOutLiteMod.Common.Poses;

internal static class RangedHelpers
{
    const double Pi = Math.PI;

    public static bool RestingOrMounted(Player p, double timer) => Resting(p, timer) || MountActive(p);

    public static double WalkTilt(int bodyFrame)
    {
        switch (bodyFrame)
        {
            case 6: return -0.75; case 7: return -1; case 8: return 0; case 9: return 0.25; case 10: return 0.5;
            case 11: return 0.25; case 12: return 0; case 13: return -0.75; case 14: return -1; case 15: return 0;
            case 16: return 0.25; case 17: return 0.5; case 18: return 0.25; case 19: return 0;
        }
        return 0;
    }

    public static void RifleSheatheArms(Player p, double rotation, double backScale)
    {
        ArmStretch back = rotation > 0.5 ? ArmStretch.None : ArmStretch.ThreeQuarters;
        SetArmBack(p, true, back, Pi * (-rotation * backScale) * p.direction);
        ArmStretch front = ArmStretch.Quarter;
        if (rotation > 0.75)
            front = ArmStretch.Full;
        else if (rotation > 0.5)
            front = ArmStretch.ThreeQuarters;
        SetArmFront(p, true, front, Pi * -rotation * p.direction);
    }

    public static void RiflePointDownArms(Player p, int bodyFrame)
    {
        double rotation = -0.125;
        if (bodyFrame != 0 && bodyFrame % 7 <= 2)
            rotation = bodyFrame < 14 ? -0.05 : -0.15;
        SetArmBack(p, true, ArmStretch.Full, Pi * rotation * p.direction);
    }

    public static void RifleWalkArms(Player p, int bodyFrame)
    {
        ArmStretch back = ArmStretch.Full, front = ArmStretch.Full;
        double frontRot = -0.4;
        if (bodyFrame > 5)
        {
            if (bodyFrame % 7 < 4)
            {
                if (bodyFrame < 14)
                {
                    back = ArmStretch.Quarter;
                    front = ArmStretch.Quarter;
                    frontRot += 0.04;
                }
            }
            else
            {
                back = ArmStretch.ThreeQuarters;
                front = ArmStretch.ThreeQuarters;
                frontRot += 0.02;
            }
        }
        SetArmBack(p, true, back, Pi * -0.3 * p.direction);
        SetArmFront(p, true, front, Pi * frontRot * p.direction);
    }

    public static PoseData RiflePointDownData(PoseData data, in PoseData idle, Player p, int bodyFrame, double timer)
    {
        data.SetOrigin(0.25, 0.25, p);
        if (bodyFrame == 0 || bodyFrame > 5)
        {
            PoseVector o = Round2Dir(new PoseVector(2, -2), p.direction, p.gravDir);
            data.Move(o.X, o.Y).WithHandOffset(p);
            data.rotation += 0.5;
        }
        else if (bodyFrame == 5)
        {
            PoseVector o = Round2Dir(new PoseVector(-2, 2), p.direction, p.gravDir);
            data.Move(o.X, o.Y);
            data.rotation += 0.15;
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        data.Move(8 * Math.Sin(t * Pi), 0);
        return LerpData(data, idle, t);
    }
}

public class StancePoleReady : OnBack
{
    public override ID GetID() => ID.StancePoleReady;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.25)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(30, timer);
        if (t > 0 && bodyFrame != 5)
        {
            double rotation = Anim.ArmRaiseLower(t, 1) * 0.33;
            ArmStretch front = t > 0.2 && t < 0.8 ? ArmStretch.None : ArmStretch.ThreeQuarters;
            SetArmFront(p, true, front, Pi * rotation * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.375, 0.625, p).RotateFaceForward(p, height, width);
        if (bodyFrame == 0)
        {
            data.Move(-4, 14);
            data.rotation -= Pi * 1.75;
        }
        else if (bodyFrame > 5)
        {
            data.Move(1, 8).WithHandOffset(p);
            data.rotation -= Pi * 2;
        }
        else if (bodyFrame == 5)
        {
            data.Move(-8, -8);
            data.rotation -= Pi * 2.25;
        }
        else if (IsMountPoseActive(p))
        {
            data.Move(13, 6);
            data.rotation -= Pi * 2.125;
        }
        double t = Anim.EaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(-8 * Math.Sin(t * Pi), 16 * Math.Sin(t * 0.75 * Pi));
            data = LerpData(data, idle, t);
        }
        return data;
    }
}

public class StancePoleShoulder : OnBack
{
    public override ID GetID() => ID.StancePoleShoulder;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(30, timer);
        if (t > 0)
        {
            SetArmFront(p, true, ArmStretch.Full, Pi * Anim.ArmRaiseLower(t) * -1 * p.direction);
            return bodyFrame;
        }
        if (bodyFrame == 0)
        {
            SetArmBack(p, true, ArmStretch.Full, 0);
            return 10;
        }
        if (bodyFrame == 5)
            SetArmFront(p, true, ArmStretch.None, Pi * -0.5 * p.direction);
        else if (bodyFrame > 5)
            SetArmFront(p, true, ArmStretch.Quarter, Pi * -0.25 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.5 - 0.4375, 0.5 + 0.4375, p).ApplyFlip(p).RotateFaceForward(p, height, width);
        if (bodyFrame == 0 || (bodyFrame == 10 && ArmBackEnabled(p)))
        {
            data.Move(6, 20);
            data.rotation += Pi * -0.675;
        }
        else if (bodyFrame > 5)
        {
            data.Move(16, 18).WithWaistOffset(p);
            data.rotation += Pi * -0.75;
        }
        else if (bodyFrame == 5)
        {
            data.Move(18, 8);
            data.rotation += Pi * -0.825;
        }
        else if (IsMountPoseActive(p))
        {
            data.Move(14, 22);
            data.rotation += Pi * -0.675;
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(-width * 0.25 * Math.Sin(t * 0.25 * Pi), -48 * Math.Sin(t * Pi));
            if (t > 0.5)
            {
                data.Unflip(p);
                data.rotation += MH.PiOver2;
            }
            else
            {
                idle.rotation -= MH.PiOver2;
            }
            data = LerpData(data, idle, t);
        }
        return data;
    }
}

public class StancePoleTwoHand : OnBack
{
    public override ID GetID() => ID.StancePoleTwoHand;
    static bool Rest(Player p, double timer) => Resting(p, timer) || p.shieldRaised;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Rest(p, timer) || Anim.LinearNormal(30, timer) > 0.2)
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

        data.SetOrigin(0.4, 0.6, p);
        if (bodyFrame == 0)
        {
            data.Move(12, 6);
        }
        else if (bodyFrame >= 5)
        {
            data.Move(9, 12);
            data.rotation += Pi * 0.15;
        }
        else if (IsMountPoseActive(p))
        {
            double speedRotation = 0.125;
            if (WeaponOutConfig.Instance.EnableWeaponPhysics)
                speedRotation = Clamp((double)p.velocity.X * p.direction, 0, 3) / 3 * 0.125;
            data.rotation += Pi * speedRotation;
            data.Move(14, 4);
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

public class StancePoleUpright : OnBack
{
    public override ID GetID() => ID.StancePoleUpright;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.75)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(30, timer);
        if (t > 0)
        {
            double rotation = bodyFrame == 5 ? -1.25 + Anim.ArmRaiseLower(t) * 1.25 : Anim.ArmWaggle(t) * 0.3;
            ArmStretch front = t > 0.2 && t < 0.8 ? ArmStretch.Full : ArmStretch.Quarter;
            SetArmFront(p, true, front, Pi * rotation * p.direction);
            return bodyFrame;
        }
        if (bodyFrame == 0)
        {
            SetArmBack(p, true, ArmStretch.Full, 0);
            bodyFrame = 6;
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.5 - 0.4 * (width / height), 0.9, p).RotateFaceForward(p, height, width);
        if (bodyFrame == 0 || bodyFrame > 5)
        {
            data.Move(-2, 17).WithHandOffset(p);
            data.rotation -= Pi * 2.5;
        }
        else if (bodyFrame == 5)
        {
            data.Move(0, -2).WithHandOffset(p);
            data.rotation -= Pi * 2.75;
        }
        else if (IsMountPoseActive(p))
        {
            data.SetOrigin(0.25, 0.75, p);
            double speedRotation = 0.25;
            if (WeaponOutConfig.Instance.EnableWeaponPhysics)
                speedRotation = Clamp((double)p.velocity.X * p.direction, 0, 3) / 3 * 0.25;
            data.rotation -= Pi * (2.5 - speedRotation);
            data.Move(10, 6);
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(8 * Math.Sin(t * Pi * 2), -8 * Math.Sin(t * 0.5 * Pi));
            data = LerpData(data, idle, t);
        }
        return data;
    }
}

public class StanceFlailReady : OnBackFlail
{
    public override ID GetID() => ID.StanceFlailReady;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.25)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(30, timer);
        if (t > 0 && bodyFrame != 5)
        {
            double rotation = Anim.ArmRaiseLower(t, 1) * -1;
            ArmStretch front = t > 0.4 && t < 0.6 ? ArmStretch.ThreeQuarters : ArmStretch.Full;
            SetArmFront(p, true, front, MH.PiOver2 * rotation * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.6, 1, p);
        if (bodyFrame == 0)
        {
            data.Move(-6, 8);
            data.rotation += Pi;
        }
        else if (bodyFrame > 5)
        {
            data.Move(-6, 6).WithHandOffset(p);
            data.rotation += Pi * 0.825;
        }
        else if (bodyFrame == 5)
        {
            data.Move(-6, -10);
            data.rotation += Pi * -0.5;
        }
        double t = Anim.EaseInEaseOutNormal(20, timer - 10);
        data.Move(8 * Math.Sin(t * Pi), 0);
        return LerpData(data, idle, t);
    }
}

public class StanceBowInHand : OnBackBow
{
    public override ID GetID() => ID.StanceBowInHand;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (RangedHelpers.RestingOrMounted(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (RangedHelpers.RestingOrMounted(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            double rotation = Anim.ArmRaiseLower(t);
            rotation = bodyFrame == 5 ? -1.25 - rotation * 0.5 : rotation * 0.375;
            SetArmFront(p, true, ArmStretch.Full, Pi * rotation * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (RangedHelpers.RestingOrMounted(p, timer))
            return idle;

        data.SetOrigin(0.5, 0.5, p);
        if (bodyFrame == 0)
        {
            data.rotation += Pi * -1.375;
            data.Move(-4, 10);
        }
        else if (bodyFrame > 5)
        {
            data.rotation += Pi * -1.5;
            data.Move(0, 10).WithHandOffset(p);
        }
        else if (bodyFrame == 5)
        {
            data.rotation += Pi * -0.375;
            data.Move(-11, -8);
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(-64 * t, 0);
            data.rotation += Pi * 2 * t;
            if (t > 0.5)
                data.ApplyFlip(p);
            data = LerpData(data, idle, t);
        }
        return data;
    }
}

public class StanceBowHunt : OnBackBow
{
    public override ID GetID() => ID.StanceBowHunt;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (RangedHelpers.RestingOrMounted(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (RangedHelpers.RestingOrMounted(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            SetArmFront(p, true, ArmStretch.Full, Pi * Anim.ArmRaiseLower(t) * 0.375 * p.direction);
            return bodyFrame;
        }
        if (bodyFrame == 0)
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.125 * p.direction);
            SetArmFront(p, true, ArmStretch.Quarter, Pi * -0.25 * p.direction);
        }
        if (bodyFrame >= 5)
        {
            ArmStretch front = ArmStretch.None;
            double armMotion = 0;
            if (WeaponOutConfig.Instance.EnableWeaponPhysics)
            {
                double vY = p.velocity.Y;
                armMotion += -Clamp(-vY * p.gravDir * 0.02, -0.39, 0.39);
                if (armMotion > 0)
                {
                    front = ArmStretch.Quarter;
                    armMotion /= 2;
                }
            }
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.125 * p.direction);
            SetArmFront(p, true, front, Pi * (-0.25 + armMotion) * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (RangedHelpers.RestingOrMounted(p, timer))
            return idle;

        data.SetOrigin(0.5, 0.5, p);
        if (bodyFrame == 0)
        {
            data.rotation += Pi * -1.675;
            data.Move(12, 14);
        }
        else if (bodyFrame >= 5)
        {
            data.rotation += Pi * -1.75;
            data.Move(12, 12);
        }
        if (WeaponOutConfig.Instance.EnableWeaponPhysics)
        {
            double vY = p.velocity.Y;
            data.rotation -= -Clamp(-vY * p.gravDir * 0.02, -0.39, 0.39);
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(-64 * t, 0);
            data.rotation += Pi * 2 * t;
            if (t > 0.5)
                data.ApplyFlip(p);
            data = LerpData(data, idle, t);
        }
        return data.WithWaistOffset(p);
    }
}

public class StancePistolCowboy : OnWaistHolster
{
    public override ID GetID() => ID.StancePistolCowboy;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        double t = Anim.LinearNormal(30, timer);
        double rotation = Anim.ArmRaiseLower(t);
        if (t > 0)
        {
            HideShield(p);
            ArmStretch back = rotation > 0.5 ? ArmStretch.ThreeQuarters : ArmStretch.Full;
            SetArmBack(p, true, back, Pi * (-rotation * 0.75) * p.direction);
        }
        else if (bodyFrame == 0)
        {
            SetArmBack(p, true, ArmStretch.ThreeQuarters, 0);
            bodyFrame = 17;
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.origin = Round2Dir(NewOrigin(0.25, 0.5, data.sourceRect, p.direction, p.gravDir), p.direction, p.gravDir);
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.rotation = Pi * 4.125;
            data.Move(6 + t * 48, 10 - t * 86);
            return LerpData(data, idle, t);
        }
        if (bodyFrame == 0 || (bodyFrame == 17 && ArmBackEnabled(p)))
        {
            data.rotation = Pi * 0.125;
            data.Move(0, 6);
        }
        else if (bodyFrame == 5)
        {
            data.rotation = Pi * -0.25;
            data.Move(-10, -8);
        }
        else if (bodyFrame > 5)
        {
            data.rotation = Pi * 0.25;
            data.Move(-4, 6).WithHandOffset(p);
        }
        else
        {
            data.Hide();
        }
        return data;
    }
}

public class StancePistolHoldUp : OnWaistHolster
{
    public override ID GetID() => ID.StancePistolHoldUp;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Back;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        if (t > 0)
        {
            double rotation = Anim.EaseInNormal(30, timer);
            ArmStretch back = rotation > 0.75 ? ArmStretch.Full : ArmStretch.ThreeQuarters;
            SetArmBack(p, true, back, Pi * -(0.5 - rotation * 0.5) * p.direction);
        }
        else if (bodyFrame == 0)
        {
            SetArmBack(p, true, ArmStretch.ThreeQuarters, MH.PiOver2 * -0.75 * p.direction);
        }
        else if (bodyFrame == 5)
        {
            SetArmBack(p, true, ArmStretch.ThreeQuarters, MH.PiOver2 * -1 * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.25, 0.5, p);
        if (bodyFrame == 0)
        {
            data.rotation = Pi * -2.375;
            data.Move(12, -2);
        }
        else if (bodyFrame == 5)
        {
            data.rotation = Pi * -2.5;
            data.Move(12, -4);
        }
        else if (bodyFrame > 5)
        {
            data.rotation = Pi * -2.375;
            data.Move(8, 1).WithOffHandOffset(p);
        }
        else
        {
            data.Hide();
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(30 * t, 0);
            return LerpData(data, idle, t);
        }
        return data;
    }
}

public class StanceRifleHipReady : OnBackDownward
{
    public override ID GetID() => ID.StanceRifleHipReady;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.4)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        if (s > 0)
        {
            RangedHelpers.RifleSheatheArms(p, s, 0.5);
        }
        else if (bodyFrame == 0)
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.25 * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * -0.25 * p.direction);
        }
        else if (bodyFrame == 5)
        {
            SetArmBack(p, true, ArmStretch.ThreeQuarters, Pi * -0.1 * p.direction);
            SetArmFront(p, true, ArmStretch.ThreeQuarters, 0);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.25, 0.25, p);
        if (bodyFrame == 0)
        {
            data.Move(6, 2);
        }
        else if (bodyFrame > 5)
        {
            data.WithHandOffset(p);
            data.rotation += 0.05 * RangedHelpers.WalkTilt(bodyFrame);
        }
        else if (bodyFrame == 5)
        {
            data.Move(-2, 2);
            data.rotation -= 0.15;
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        data.Move(8 * Math.Sin(t * Pi), 0);
        return LerpData(data, idle, t);
    }
}

public class StanceRifleHoldUp : OnBackDownward
{
    public override ID GetID() => ID.StanceRifleHoldUp;
    static bool Rest(Player p, double timer) => timer == 0 || (p.bodyFrame.Y != 0 && p.bodyFrame.Y < p.bodyFrame.Height * 4);
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Rest(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Back;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Rest(p, timer))
            return bodyFrame;
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        if (t > 0)
        {
            double s = Anim.ArmRaiseLower(t);
            double rotation = t > 0.5 ? Pi * (-s * 0.9) : Pi * (-0.4 - s * 0.5);
            SetArmBack(p, true, t > 0.5 ? ArmStretch.Full : ArmStretch.ThreeQuarters, rotation * p.direction);
        }
        else if (bodyFrame == 0)
        {
            SetArmBack(p, true, ArmStretch.ThreeQuarters, MH.PiOver2 * -0.75 * p.direction);
        }
        else if (bodyFrame == 5)
        {
            SetArmBack(p, true, ArmStretch.ThreeQuarters, MH.PiOver2 * -1 * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Rest(p, timer))
            return idle;

        data.SetOrigin(0.25, 0.5, p);
        if (bodyFrame == 0)
        {
            data.rotation = Pi * -0.375;
            data.Move(13, -2);
        }
        else if (bodyFrame == 5)
        {
            data.rotation = Pi * -0.5;
            data.Move(12, -4);
        }
        else if (bodyFrame > 5)
        {
            data.rotation = Pi * -0.375;
            data.Move(9, 0).WithOffHandOffset(p);
        }
        else
        {
            data.Hide();
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(-40 * t, -20 * t);
            return LerpData(data, idle, t);
        }
        return data;
    }
}

public class StanceRiflePointDown : OnBackDownward
{
    public override ID GetID() => ID.StanceRiflePointDown;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.4)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        if (s > 0)
        {
            RangedHelpers.RifleSheatheArms(p, s, 0.5);
        }
        else if (bodyFrame == 0 || bodyFrame > 5)
        {
            RangedHelpers.RiflePointDownArms(p, bodyFrame);
            if (bodyFrame == 0)
                bodyFrame = 6;
        }
        else if (bodyFrame == 5)
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.1 * p.direction);
            SetArmFront(p, true, ArmStretch.ThreeQuarters, 0);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;
        return RangedHelpers.RiflePointDownData(data, idle, p, bodyFrame, timer);
    }
}

public class StanceRiflePointDown2 : HoldRifleOffHandUpright
{
    public override ID GetID() => ID.StanceRiflePointDown2;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        double t = Anim.LinearNormal(30, timer);
        if (t > 0)
        {
            SetArmBack(p, true, ArmStretch.ThreeQuarters, Pi * Anim.EaseOutNormal(30, timer) * -0.4 * p.direction);
            if (t < 0.4)
                bodyFrame = 17;
        }
        else if (bodyFrame == 0 || bodyFrame > 5)
        {
            RangedHelpers.RiflePointDownArms(p, bodyFrame);
            if (bodyFrame == 0)
                bodyFrame = 6;
        }
        else if (bodyFrame == 5)
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.1 * p.direction);
            SetArmFront(p, true, ArmStretch.ThreeQuarters, 0);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;
        return RangedHelpers.RiflePointDownData(data, idle, p, bodyFrame, timer);
    }
}

public class StanceRifleBoltAction : OnBackDownward
{
    public override ID GetID() => ID.StanceRifleBoltAction;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.4)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }

    static double AnimationTime(double timer) => Math.Max(0, -(timer - (double)WeaponOutConfig.Instance.CombatDelayTimerMax * 60) - 10);

    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        if (s > 0)
            RangedHelpers.RifleSheatheArms(p, s, 0.5);
        else if (bodyFrame == 0)
            bodyFrame = 3;
        else if (bodyFrame >= 5)
            RangedHelpers.RifleWalkArms(p, bodyFrame);

        double animationTime = AnimationTime(timer);
        if (t == 0 && animationTime <= 40)
        {
            double at = Anim.LinearNormal(40, animationTime);
            if (at > 0)
            {
                double backRot = Math.Min(Anim.ArmRaiseLower(at, 1), 0.75);
                SetArmBack(p, true, ArmStretch.Full, Pi * (-0.4 + backRot * -0.5) * p.direction);
                double armRot = Anim.ArmRaiseLower(at, 0.75);
                ArmStretch bolt = ArmStretch.Full;
                if (armRot > 0.9)
                    bolt = ArmStretch.ThreeQuarters;
                if (armRot > 0.93)
                    bolt = ArmStretch.Quarter;
                if (armRot > 0.96)
                    bolt = ArmStretch.None;
                SetArmFront(p, true, bolt, Pi * (-0.4 - armRot * 0.4) * p.direction);
            }
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        double t = Anim.EaseInEaseOutNormal(30, timer);
        double animationTime = AnimationTime(timer);
        bool inBolt = t == 0 && animationTime <= 40;
        data.SetOrigin(0.25, 1.0 / 3, p);
        if (bodyFrame == 3)
        {
            data.Move(8, 0);
        }
        else if (bodyFrame > 5)
        {
            data.Move(6, 0);
            if (p.velocity.Y != 0)
            {
                data.Move(-1, 2);
                data.rotation += 0.175;
            }
            if (inBolt)
                data.WithWaistOffset(p);
            else
                data.WithHandOffset(p);
            data.rotation += 0.05 * RangedHelpers.WalkTilt(bodyFrame);
        }
        else if (bodyFrame == 5)
        {
            data.Move(6, 0);
        }
        data.Move(8 * Math.Sin(t * Pi), 0);
        data = LerpData(data, idle, t);

        if (inBolt)
        {
            double at = Anim.LinearNormal(40, animationTime);
            if (at > 0 && animationTime == 10 && !p.CCed)
                SoundEngine.PlaySound(SoundID.Item149, p.MountedCenter, 0.5f, 0.375f);
            double gunRot = Anim.ArmRaiseLower(at, 0.75);
            if (at > 0.75)
            {
                data.rotation -= Pi * 0.375;
                data.rotation = Lerp(data.rotation, (double)p.itemRotation * p.direction * p.gravDir, (at - 0.75) * 4);
                data.position.X = Lerp(data.position.X - 8, data.position.X - Math.Min(data.rotation, 0) * -height * 0.25, (at - 0.75) * 4);
            }
            else
            {
                if (at >= 0.5)
                    gunRot = 1;
                data.Move(-8 * gunRot, 0);
                data.rotation -= Pi * 0.375 * gunRot;
            }
        }
        return data;
    }
}

public class StanceRiflePumpAction : OnBackDownward
{
    const double sheathe = 30, pumpMax = 45;

    public override ID GetID() => ID.StanceRiflePumpAction;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.4)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return bodyFrame;
        double t = Anim.LinearNormal(sheathe, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        if (s > 0)
            RangedHelpers.RifleSheatheArms(p, s, 0.75);
        else if (bodyFrame == 0)
            bodyFrame = 3;
        else if (bodyFrame >= 5)
            RangedHelpers.RifleWalkArms(p, bodyFrame);

        double animationTime = Clamp(timer - sheathe, -1, pumpMax + sheathe + 1);
        if (t == 0 && animationTime <= pumpMax)
        {
            double at = Anim.LinearNormal(pumpMax, animationTime);
            if (at > 0)
            {
                double backRot = Math.Min(Anim.ArmRaiseLower(at, 1), 0.5);
                double armRot = Anim.ArmRaiseLower(at, 0.7);
                ArmStretch pump = ArmStretch.Full;
                if (armRot > 0.75)
                    pump = ArmStretch.ThreeQuarters;
                if (armRot > 0.85)
                    pump = ArmStretch.Quarter;
                if (armRot > 0.9)
                    pump = ArmStretch.None;
                SetArmBack(p, true, pump, Pi * (-0.4 + backRot * -0.65) * p.direction);
                SetArmFront(p, true, ArmStretch.Full, Pi * (-0.375 + armRot * 0.125) * p.direction);
            }
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        double t = Anim.EaseInEaseOutNormal(30, timer);
        double animationTime = Clamp(timer - sheathe, 0, pumpMax + sheathe);
        bool inPump = animationTime <= pumpMax;
        data.SetOrigin(0.25, 1.0 / 3, p).Move(8, 0);
        if (bodyFrame > 5)
        {
            if (p.velocity.Y != 0)
            {
                data.Move(-1, 2);
                data.rotation += 0.175;
            }
            if (inPump)
                data.WithWaistOffset(p);
            else
                data.WithHandOffset(p);
            data.rotation += 0.05 * RangedHelpers.WalkTilt(bodyFrame);
        }
        else if (bodyFrame == 5 && !inPump)
        {
            data.Move(6, 0);
        }
        if (inPump)
        {
            double at = Anim.LinearNormal(pumpMax, animationTime);
            if (at > 0 && animationTime == 40)
                SoundEngine.PlaySound(SoundID.Item149, p.MountedCenter, 0.75f, 0.3f);
            double gunRot = Anim.ArmRaiseLower(Math.Min(at, 0.75), 0.75);
            if (at >= 0.5)
                gunRot = 1 + (0.5 - at) * 0.5;
            data.Move(-4 * gunRot, 4 * gunRot);
            data.rotation -= Pi * 0.375 * gunRot;
        }
        data.Move(-height / 2 * Math.Sin(t * Pi), -width / 2 * Math.Sin(t * Pi));
        data.rotation -= 4 * t;
        return LerpData(data, idle, t);
    }
}

public class StanceLauncherShoulder : OnBackUpright
{
    public override ID GetID() => ID.StanceLauncherShoulder;
    static bool Rest(Player p, double timer) => timer == 0 || p.shieldRaised;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Rest(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.OffHand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Rest(p, timer))
            return bodyFrame;
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmRaiseLower(t) : 0;
        SetArmBack(p, true, ArmStretch.Full, Pi * (-0.365 - s) * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Rest(p, timer))
            return idle;

        data.SetOrigin(0.25, 0.75, p).RotateFaceForward(p, height - 6, width);
        PoseVector o = Round2Dir(new PoseVector(-12 + Math.Min(height, width) / 2, 0), p.direction, 1);
        data.Move(o.X, o.Y);
        data = LerpData(data, idle, Anim.OverEaseNormal(30, timer));
        return data.WithWaistOffset(p);
    }
}

public class StanceMachineTool : OnBackUpright
{
    public override ID GetID() => ID.StanceMachineTool;
    public override int DrawDepth(Player p, Item i, double timer)
    {
        if (Resting(p, timer) || Anim.LinearNormal(30, timer) > 0.5)
            return base.DrawDepth(p, i, timer);
        return DrawDepthID.Hand;
    }
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (Resting(p, timer))
            return base.UpdateIdleBodyFrame(p, i, bodyFrame, timer);
        HideShield(p);
        double t = Anim.LinearNormal(30, timer);
        double s = t > 0 ? Anim.ArmWaggle(t) * 0.5 : 0;
        if (bodyFrame == 0 && s == 0)
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.25 * p.direction);
            return bodyFrame;
        }
        if (bodyFrame == 0 || bodyFrame >= 5)
        {
            ArmStretch back = s > 0 ? ArmStretch.Quarter : ArmStretch.ThreeQuarters;
            ArmStretch front = s > 0 ? ArmStretch.None : ArmStretch.ThreeQuarters;
            double backBase = bodyFrame != 5 ? -0.25 : -0.3;
            SetArmBack(p, true, back, Pi * (backBase + s * 2) * p.direction);
            SetArmFront(p, true, front, Pi * (0 - s) * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseData idle = base.CalculateDrawData(data, p, height, width, bodyFrame, timer);
        if (Resting(p, timer))
            return idle;

        data.SetOrigin(0.1, 0.5, p);
        if (bodyFrame == 0 || (bodyFrame == 13 && !ArmBackEnabled(p)))
        {
            data.Move(-8, 8);
        }
        else if (bodyFrame > 5)
        {
            data.Move(-7, 7);
            data.rotation -= Pi * 0.05;
            data.WithWaistOffset(p);
        }
        else if (bodyFrame == 5)
        {
            data.Move(-7, 8);
            data.rotation -= Pi * 0.1;
        }
        double t = Anim.EaseInEaseOutNormal(30, timer);
        if (t > 0)
        {
            data.Move(-16 * t, 0);
            data.rotation -= 3 * t;
            data = LerpData(data, idle, t);
        }
        return data;
    }
}
