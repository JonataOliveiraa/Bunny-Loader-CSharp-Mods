using System;
using Terraria;
using WeaponOutLiteMod.Common.Configs;
using static WeaponOutLiteMod.Common.PoseMath;
using ArmStretch = Terraria.Player.CompositeArmStretchAmount;
using ID = WeaponOutLiteMod.Common.DrawItemPoseID;

namespace WeaponOutLiteMod.Common.Poses;

public class Pose
{
    protected const double Pi = Math.PI;

    public virtual ID GetID() => ID.Unassigned;

    public virtual int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Hand;

    public virtual int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer) => bodyFrame;

    public virtual PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer) => data;
}

public class Unassigned : Pose
{
    public override ID GetID() => ID.Unassigned;
    public override int DrawDepth(Player p, Item i, double timer) => 0;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer) => data.Hide();
}

public class None : Pose
{
    public override ID GetID() => ID.None;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer) => data.Hide();
}

public class OnBack : Pose
{
    public override ID GetID() => ID.Back;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.5, 0.5, p).RotateFaceForward(p, height, width);
        PoseVector o = Round2(new PoseVector(2 + width * -0.1, height * 0.05));
        data.Move(o.X, o.Y);
        data.rotation -= Pi * 1.25;
        return data.WithWaistOffset(p);
    }
}

public class OnBackBow : Pose
{
    public override ID GetID() => ID.BackBow;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.5, 0.5, p).ApplyFlip(p);
        double smallItemOffset = Clamp(height, 20, 80);
        double smallItemOffsetR = 80 / smallItemOffset * 0.25;
        data.Move(-4, 0);
        data.rotation -= Pi * 1.1 + smallItemOffsetR;
        return data.WithWaistOffset(p);
    }
}

public class OnBackDownward : Pose
{
    public override ID GetID() => ID.BackDownward;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.5, 0.5, p);
        double longRotation = 32 / Math.Max(32, width);
        data.rotation += Pi * -(1.15 + 0.2 * longRotation);
        data.Move(-4 + width * 0.05, 0);
        return data.WithWaistOffset(p);
    }
}

public class OnBackFlail : Pose
{
    public override ID GetID() => ID.BackFlail;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.6, 1, p).Move(0, 16);
        return data.WithWaistOffset(p);
    }
}

public class OnBackFlailShoulder : Pose
{
    public override ID GetID() => ID.BackFlailShoulder;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.OffHand;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        SetArmBack(p, true, ArmStretch.ThreeQuarters, Pi * -0.25 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.6, 1, p).RotateFaceForward(p, height, width);
        data.Move(16, 6);
        data.rotation += Pi * -0.6;
        return data.WithWaistOffset(p);
    }
}

public class OnBackGunShoulder : Pose
{
    public override ID GetID() => ID.BackGunShoulder;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.OffHand;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        SetArmBack(p, true, ArmStretch.Full, Pi * -0.365 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.RotateFaceForward(p, height - 4, width).SetOrigin(0.25, 0.75, p);
        PoseVector o = Round2Dir(new PoseVector(-12 + width / 4, 2 - height / 8), p.direction, 1);
        data.Move(o.X, o.Y);
        return data.WithWaistOffset(p);
    }
}

public class OnBackUpright : Pose
{
    public override ID GetID() => ID.BackUpright;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.5, 0.5, p).RotateFaceForward(p, height, width * 1.5);
        data.rotation -= Pi * 0.5;
        data.Move(-6, 20 - width / 2);
        return data.WithWaistOffset(p);
    }
}

public class OnWaistHolster : Pose
{
    public override ID GetID() => ID.WaistHolster;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.RotateFaceForward(p, height, width);
        PoseVector o = Round2Dir(new PoseVector(-2 + width / 8, 28 - (height + width) / 4.5), p.direction, 1);
        data.Move(o.X, o.Y);
        if (WeaponOutConfig.Instance.EnableWeaponPhysics)
        {
            double vX = p.velocity.X, vY = p.velocity.Y;
            double motion = vY == 0 ? vX * p.direction : -vY * p.gravDir;
            data.rotation -= -Clamp(motion * 0.02, -0.39, 0.39);
        }
        return data.WithWaistOffset(p);
    }
}

public class OnWaistSheathe : Pose
{
    public override ID GetID() => ID.WaistSheathe;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.5 - 0.3 * (width / height), 0.8, p).ApplyFlip(p).RotateFaceForward(p, height, width);
        const double minSize = 64;
        double smallItemOffset = Math.Max(Math.Sqrt(width * width + height * height), minSize) / minSize;
        double smallItemOffsetR = 0.1 / smallItemOffset;
        data.rotation += Pi * (1 - smallItemOffsetR);
        data.Move(4 + 8 / smallItemOffset, 4 + 4 / smallItemOffset);
        if (WeaponOutConfig.Instance.EnableWeaponPhysics)
        {
            double vX = p.velocity.X, vY = p.velocity.Y;
            double motion = vY == 0 ? vX * p.direction : vY * p.gravDir;
            data.rotation -= -Clamp(motion * 0.02, -0.39, 0.39);
        }
        return data.WithWaistOffset(p);
    }
}

public class HoldInHand : Pose
{
    public override ID GetID() => ID.HoldInHand;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Hand;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.05, 0.9, p);
        if (bodyFrame == 0)
        {
            data.rotation += Pi * 0.5;
            data.Move(-6, 8);
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
        return data;
    }
}

public class HoldInHandFront : Pose
{
    public override ID GetID() => ID.HoldInHandFront;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Hand;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (bodyFrame == 0)
            bodyFrame = 3;
        else if (bodyFrame == 5)
            SetArmFront(p, true, ArmStretch.Full, MH.PiOver2 * -p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.25, 0.5, p);
        if (bodyFrame == 3)
            data.Move(12, 6);
        else if (bodyFrame == 5)
            data.Move(12, 2);
        else if (bodyFrame > 5)
            data.Move(0, 6).WithHandOffset(p);
        else
            data.Hide();
        return data;
    }
}

public class HoldReversedInHand : Pose
{
    public override ID GetID() => ID.HoldReversedInHand;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Hand;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.5 - 0.4 * (width / height), 0.9, p);
        double handle = GetHandleLength(0.1, 0.1, width, height);
        if (bodyFrame == 0)
        {
            data.rotation = Pi * -0.5 * p.gravDir;
            data.Move(-6, 14);
        }
        else if (bodyFrame == 5)
        {
            data.rotation = Pi * 0.3;
            data.Move(-10, -12 + Math.Sin(data.rotation) * handle);
        }
        else if (bodyFrame > 5)
        {
            data.rotation = Pi * -0.8;
            data.Move(2 + Math.Cos(data.rotation) * handle, 8 + Math.Sin(data.rotation) * handle).WithHandOffset(p);
        }
        else
        {
            data.Hide();
        }
        return data;
    }
}

public class HoldSpellTome : Pose
{
    public override ID GetID() => ID.HoldSpellTome;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Hand;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (bodyFrame == 0)
            bodyFrame = 4;
        else if (bodyFrame == 5)
            SetArmFront(p, true, ArmStretch.Full, MH.PiOver2 * -p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.25, 0.75, p).ApplyFlip(p);
        data.rotation = -MH.PiOver2;
        if (bodyFrame == 4)
        {
            data.Move(9, 7);
        }
        else if (bodyFrame == 0)
        {
            data.Move(4, 12);
            data.rotation += MH.PiOver4 * 5;
        }
        else if (bodyFrame == 5)
        {
            data.Move(12, 2);
            data.rotation -= MH.PiOver4 / 2;
        }
        else if (bodyFrame > 5)
        {
            data.rotation += MH.PiOver4 * 5;
            data.Move(4, 10).WithHandOffset(p);
        }
        else
        {
            data.Hide();
        }
        return data;
    }
}

public class HoldInOffHand : Pose
{
    public override ID GetID() => ID.HoldInOffHand;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.OffHand;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (bodyFrame == 0)
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.25 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.05, 0.85, p);
        if (bodyFrame == 0 || IsMountPoseActive(p))
        {
            data.Move(16, 10);
        }
        else if (bodyFrame == 5)
        {
            data.rotation += Pi * -0.5;
            data.Move(12, -6);
        }
        else if (bodyFrame > 5)
        {
            data.Move(8, 6).WithOffHandOffset(p);
        }
        else
        {
            data.Hide();
        }
        return data;
    }
}

public class HoldRifleOffHandUpright : Pose
{
    public override ID GetID() => ID.HoldRifleOffHandUpright;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        HideShield(p);
        if (bodyFrame <= 5)
            SetArmBack(p, true, ArmStretch.ThreeQuarters, Pi * -0.375 * p.direction);
        else
            SetArmBack(p, true, ArmStretch.Quarter, Pi * -0.5 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.25, 0.5, p);
        data.rotation -= Pi * 0.5;
        data.Move(14, Round2f(Math.Min(23 - width / 4, width / 4)));
        if (bodyFrame > 5)
            data.Move(-2, 0);
        return data.WithWaistOffset(p);
    }
}

public class PoleOffHand : Pose
{
    public override ID GetID() => ID.PoleOffHand;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (bodyFrame == 0 || bodyFrame == 5)
            SetArmBack(p, true, ArmStretch.ThreeQuarters, Pi * -0.175 * p.direction);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.5 - 0.4 * (width / height), 0.9, p).RotateFaceForward(p, height, width);
        if (bodyFrame == 0)
        {
            data.rotation += Pi * -0.5;
            data.Move(15, 18);
        }
        else if (bodyFrame > 5)
        {
            data.rotation += Pi * -0.5;
            data.Move(13, 16).WithOffHandOffset(p);
        }
        else
        {
            data.rotation += Pi * -0.45;
            data.Move(10, 19);
        }
        return data;
    }
}

public class CarryInBothHands : Pose
{
    public override ID GetID() => ID.CarryInBothHands;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Hand;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        HideShield(p);
        if (bodyFrame == 0)
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.15 * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * -0.2 * p.direction);
        }
        else if (bodyFrame == 5)
        {
            SetArmBack(p, true, ArmStretch.Full, Pi * -0.3 * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * -0.35 * p.direction);
        }
        else if (bodyFrame > 5)
        {
            SetArmBack(p, true, ArmStretch.Full, 0);
            SetArmFront(p, true, ArmStretch.Full, Pi * -0.1 * p.direction);
        }
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.05, 0.95, p);
        int frame = BodyFrameNum(p);
        if (frame == 0)
        {
            data.Move(8, 12);
        }
        else if (frame == 5)
        {
            data.Move(10, 8);
        }
        else if (frame > 5)
        {
            data.Move(4, 13);
            if (MountActive(p))
                data.Move(2, -6);
            data.rotation = Pi * 0.075;
        }
        else
        {
            data.Hide();
        }
        return data.WithWaistOffset(p);
    }
}

public class JoustingLance : Pose
{
    public override ID GetID() => ID.JoustingLance;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Hand;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (bodyFrame == 0)
            return 6;
        if (bodyFrame == 5)
            return 10;
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.SetOrigin(0.1, 0.9, p);
        if (bodyFrame == 0)
        {
            data.rotation += Pi * 0.5;
            data.Move(-6, 8);
        }
        else if (bodyFrame == 5)
        {
            data.rotation += Pi * -2.25;
            data.Move(-9, -7);
        }
        else if (bodyFrame > 5)
        {
            data.rotation += Pi * 0.3;
            data.Move(-6, 9).WithHandOffset(p);
        }
        else if (IsMountPoseActive(p))
        {
            double speedRotation = 0.25;
            if (WeaponOutConfig.Instance.EnableWeaponPhysics)
            {
                const double maxSpeed = 3;
                speedRotation = Clamp((double)p.velocity.X * p.direction, 0, maxSpeed) / maxSpeed * 0.25;
            }
            data.rotation -= Pi * (0.25 - speedRotation);
            data.Move(8, 8);
        }
        else
        {
            data.Hide();
        }
        return data;
    }
}

public abstract class FloatingPose : Pose
{
    protected static PoseVector Spin(double xScale, double speed = 2) => new(Math.Sin(GlobalTime() * speed) * xScale, -Math.Cos(GlobalTime() * speed));

    protected static void Momentum(ref PoseData data, Player p, double k) => data.Move(-p.velocity.X * k * p.direction, -p.velocity.Y * k * p.gravDir);

    protected static void OffHandFloatArms(Player p, int bodyFrame, double standing, double moving)
    {
        HideShield(p);
        SetArmBack(p, true, ArmStretch.Full, Pi * (bodyFrame == 0 ? standing : moving) * p.direction);
    }
}

public class FloatingBack : FloatingPose
{
    public override ID GetID() => ID.FloatingBack;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.RotateFaceForward(p, height, width);
        data.rotation -= MH.PiOver4;
        PoseVector spin = Spin(p.direction);
        data.Move(-24 + spin.X * 2, 8 - Math.Max(height, width) / 4 + spin.Y * 2);
        Momentum(ref data, p, 1);
        data.rotation += Pi * 0.8 + spin.X * -0.02;
        return data;
    }
}

public class FloatingBackUpright : FloatingPose
{
    public override ID GetID() => ID.FloatingBackUpright;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.RotateFaceForward(p, height, width);
        data.rotation -= MH.PiOver4;
        PoseVector spin = Spin(p.direction);
        data.Move(-24 + spin.X * 2, 8 - Math.Max(height, width) / 4 + spin.Y * 2);
        Momentum(ref data, p, 1);
        data.rotation += spin.X * -0.02;
        return data;
    }
}

public class FloatingBackAimed : FloatingPose
{
    public override ID GetID() => ID.FloatingBackAimed;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.RotateFaceForward(p, height, width);
        PoseVector spin = Spin(0.5);
        data.Move(-Math.Max(height, width) / 2 + spin.X * 2, -(height + width) / 4 + spin.Y * 2);
        Momentum(ref data, p, 1);
        data.rotation += spin.Y * 0.02;
        return data;
    }
}

public class FloatingInFront : FloatingPose
{
    public override ID GetID() => ID.FloatingInFront;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer) => 3;
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseVector spin = Spin(1);
        data.Move(24 + width / 4 + spin.X * 2, 4 + spin.Y * 2);
        Momentum(ref data, p, 0.25);
        data.rotation += spin.X * -0.02;
        return data;
    }
}

public class FloatingOffHand : FloatingPose
{
    public override ID GetID() => ID.FloatingOffHand;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        OffHandFloatArms(p, bodyFrame, -0.25, -0.175);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        PoseVector spin = Spin(0.5);
        data.Move(6 + Math.Max(height, width) / 3 + spin.X * 2, -(height + width) / 8 + spin.Y * 2);
        Momentum(ref data, p, 0.5);
        data.rotation += spin.Y * 0.02;
        return data;
    }
}

public class FloatingOffHandAimed : FloatingPose
{
    public override ID GetID() => ID.FloatingOffHandAimed;
    public override int DrawDepth(Player p, Item i, double timer) => DrawDepthID.Back;
    public override int UpdateIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        OffHandFloatArms(p, bodyFrame, -0.375, -0.25);
        return bodyFrame;
    }
    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        data.RotateFaceForward(p, height, width);
        PoseVector spin = Spin(0.5);
        data.Move(6 + Math.Max(height, width) / 3 + spin.X * 2, -(height + width) / 8 + spin.Y * 2);
        Momentum(ref data, p, 0.5);
        data.rotation += spin.Y * 0.02;
        return data;
    }
}

public class FloatingOrbit : FloatingPose
{
    readonly ID id;
    readonly double speed;
    readonly double xSign;
    readonly double ySquash;
    readonly double rotSign;

    FloatingOrbit(ID id, double speed, double xSign, double ySquash, double rotSign)
    {
        this.id = id;
        this.speed = speed;
        this.xSign = xSign;
        this.ySquash = ySquash;
        this.rotSign = rotSign;
    }

    public override ID GetID() => id;

    public override int DrawDepth(Player p, Item i, double timer) => -Math.Cos(GlobalTime() * speed) < 0 ? DrawDepthID.Back : DrawDepthID.Front;

    public override PoseData CalculateDrawData(PoseData data, Player p, double height, double width, int bodyFrame, double timer)
    {
        double t = GlobalTime() * speed;
        double orbitX = xSign * Math.Sin(t) * p.direction;
        double orbitY = -Math.Cos(t) / ySquash;
        data.Move(orbitX * 24, 12 - Math.Max(height, width) / 2.5 + orbitY * 24);
        Momentum(ref data, p, 0.5);
        data.rotation += orbitY * rotSign * p.direction * (64 / (8 + height + width)) * -0.5;
        return data;
    }

    public static FloatingOrbit AntiClockwiseFast() => new(ID.FloatingOrbitAntiClockwiseFast, 2, -1, 3, 1);
    public static FloatingOrbit AntiClockwiseSlow() => new(ID.FloatingOrbitAntiClockwiseSlow, 1, -1, 5, 1);
    public static FloatingOrbit ClockwiseFast() => new(ID.FloatingOrbitClockwiseFast, 2, 1, 3, -1);
    public static FloatingOrbit ClockwiseSlow() => new(ID.FloatingOrbitClockwiseSlow, 1, 1, 5, 1);
}
