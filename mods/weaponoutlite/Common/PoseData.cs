using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using WeaponOutLiteMod.Common.Configs;
using ArmStretch = Terraria.Player.CompositeArmStretchAmount;

namespace WeaponOutLiteMod.Common;

public struct PoseVector
{
    public double X;
    public double Y;

    public PoseVector(double x, double y)
    {
        X = x;
        Y = y;
    }
}

public static class MH
{
    public const double Pi = Math.PI;
    public const double PiOver2 = Pi / 2;
    public const double PiOver4 = Pi / 4;
    public const double TwoPi = Pi * 2;
}

// The item being posed. The math stays in double on purpose: Round2Dir takes floor/ceil of values
// like width * 0.05, where float rounding would land on a different pixel than the original's numbers.
// A struct, so cloning is just a copy and a pose allocates nothing per frame.
public struct PoseData
{
    public Texture2D texture;
    public PoseVector position;
    public Rectangle sourceRect;
    public Color color;
    public double rotation;
    public PoseVector origin;
    public PoseVector scale;
    public SpriteEffects effect;

    public PoseData(Texture2D texture, PoseVector position, Rectangle sourceRect, Color color, double rotation, PoseVector origin, PoseVector scale, SpriteEffects effect)
    {
        this.texture = texture;
        this.position = position;
        this.sourceRect = sourceRect;
        this.color = color;
        this.rotation = rotation;
        this.origin = origin;
        this.scale = scale;
        this.effect = effect;
    }

    public readonly bool Has(SpriteEffects flag) => (effect & flag) != 0;
}

// The chainable part of PoseData. Each one changes the data in place and hands it back by ref.
public static class PoseDataExtensions
{
    public static ref PoseData Move(this ref PoseData d, double x, double y)
    {
        d.position.X += x;
        d.position.Y += y;
        return ref d;
    }

    public static ref PoseData Hide(this ref PoseData d)
    {
        d.color = new Color(0, 0, 0, 0);
        return ref d;
    }

    public static ref PoseData SetOrigin(this ref PoseData d, double ox, double oy, Player p) => ref d.SetOriginDir(ox, oy, p.direction, p.gravDir);

    public static ref PoseData SetOriginDir(this ref PoseData d, double ox, double oy, double xDir, double yDir)
    {
        d.origin = PoseMath.Round2Dir(PoseMath.NewOrigin(ox, oy, d.sourceRect, xDir, yDir), xDir, yDir);
        if (d.origin.Y % 2 == 0)
        {
            d.origin.Y -= yDir;
            d.position.Y += 1;
        }
        if (d.origin.X % 2 == 0)
        {
            d.origin.X += xDir;
            d.position.X -= 1;
        }
        return ref d;
    }

    public static ref PoseData RotateFaceForward(this ref PoseData d, Player p, double height, double width)
    {
        d.rotation = MH.Pi * (0.25 + d.RotateFaceForwardOffset(p, height, width));
        return ref d;
    }

    public static double RotateFaceForwardOffset(this ref PoseData d, Player p, double height, double width)
    {
        double sensitivity = height < 32 ? 1 : 0.5;
        double offset = PoseMath.Clamp((height - width) / Math.Min(height, width) * sensitivity, -1, 1);
        if (d.Has(SpriteEffects.FlipVertically) == (p.direction > 0))
            offset = -offset - 2;
        return offset / 4;
    }

    public static ref PoseData ApplyFlip(this ref PoseData d, Player p) => ref d.ApplyFlipDir(p.direction, p.gravDir);

    public static ref PoseData ApplyFlipDir(this ref PoseData d, double xDir, double yDir)
    {
        SpriteEffects e = xDir > 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        if (yDir < 0)
            e |= SpriteEffects.FlipHorizontally;
        d.effect = e;
        if (d.sourceRect.Height % 4 == 0)
            d.position.Y += 2;
        return ref d;
    }

    public static ref PoseData Unflip(this ref PoseData d, Player p)
    {
        SpriteEffects e = p.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        if (p.gravDir < 0)
            e |= SpriteEffects.FlipHorizontally;
        d.effect = e;
        if (d.sourceRect.Height % 4 == 0)
            d.position.Y -= 2;
        return ref d;
    }

    public static ref PoseData WithHandOffset(this ref PoseData d, Player p)
    {
        int f = PoseMath.BodyFrameNum(p);
        if (f % 7 <= 2)
            d.position.Y -= 2;
        switch (f)
        {
            case 7: case 8: case 9: case 10: d.position.X -= 2; break;
            case 14: case 17: d.position.X += 2; break;
            case 15: case 16: d.position.X += 4; break;
        }
        return ref d;
    }

    public static ref PoseData WithOffHandOffset(this ref PoseData d, Player p)
    {
        int f = PoseMath.BodyFrameNum(p);
        if (f % 7 <= 2)
            d.position.Y -= 2;
        switch (f)
        {
            case 7: case 8: case 9: case 10: d.position.X += 2; break;
            case 14: case 17: d.position.X -= 2; break;
            case 15: case 16: d.position.X -= 4; break;
        }
        return ref d;
    }

    public static ref PoseData WithWaistOffset(this ref PoseData d, Player p)
    {
        switch (PoseMath.BodyFrameNum(p))
        {
            case 7: case 8: case 9: case 14: case 15: case 16: d.position.Y -= 2; break;
        }
        return ref d;
    }
}

public static class PoseMath
{
    public static double Clamp(double v, double min, double max) => Math.Min(Math.Max(v, min), max);

    public static double Lerp(double a, double b, double t) => a + (b - a) * t;

    // Math.Round rounds halves to even; the original's rounds them up.
    public static double RoundHalfUp(double v)
    {
        double floor = Math.Floor(v);
        return v - floor >= 0.5 ? floor + 1 : floor;
    }

    public static double RoundEven(double v)
    {
        double r = RoundHalfUp(v);
        return Math.Abs(v % 1) == 0.5 && r % 2 != 0 ? r - 1 : r;
    }

    static double Directed(double v, double dir) => dir < 0 ? Math.Floor(v) : Math.Ceiling(v);

    public static double Round2f(double v) => RoundEven(v / 2) * 2;

    public static PoseVector Round2(PoseVector v) => new(Round2f(v.X), Round2f(v.Y));

    public static PoseVector Round2Dir(PoseVector v, double xDir, double yDir) => new(Directed(v.X / 2, xDir) * 2, Directed(v.Y / 2, yDir) * 2);

    public static PoseVector RoundDir(PoseVector v, double xDir, double yDir) => new(Directed(v.X, xDir), Directed(v.Y, yDir));

    public static int BodyFrameNum(Player p)
    {
        int height = p.bodyFrame.Height;
        return height == 0 ? -1 : p.bodyFrame.Y / height;
    }

    public static bool MountActive(Player p) => p.mount.Active;

    public static bool IsMountPoseActive(Player p)
    {
        Rectangle f = p.bodyFrame;
        return MountActive(p) && f.Y >= f.Height && f.Y < 5 * f.Height;
    }

    public static void SetArmFront(Player p, bool enabled, ArmStretch stretch, double rotation)
    {
        p.compositeFrontArm.enabled = enabled;
        p.compositeFrontArm.stretch = stretch;
        p.compositeFrontArm.rotation = (float)rotation;
    }

    public static void SetArmBack(Player p, bool enabled, ArmStretch stretch, double rotation)
    {
        p.compositeBackArm.enabled = enabled;
        p.compositeBackArm.stretch = stretch;
        p.compositeBackArm.rotation = (float)rotation;
    }

    public static bool ArmBackEnabled(Player p) => p.compositeBackArm.enabled;

    public static void HideShield(Player p)
    {
        p.shield = -1;
        p.cShield = -1;
    }

    public static void SetLegFrameIfIdle(Player p, int frame)
    {
        ref Rectangle leg = ref p.legFrame;
        if (leg.Y == 0)
            leg.Y = frame * leg.Height;
    }

    public static PoseVector NewOrigin(double ox, double oy, Rectangle rect, double xDir, double yDir) =>
        new(rect.Width * (0.5 - (0.5 - ox) * xDir), rect.Height * (0.5 - (0.5 - oy) * yDir));

    public static PoseData LerpData(in PoseData start, in PoseData end, double t)
    {
        PoseData mix = start;
        mix.origin.X = Lerp(start.origin.X, end.origin.X, t);
        mix.origin.Y = Lerp(start.origin.Y, end.origin.Y, t);
        mix.position.X = Lerp(start.position.X, end.position.X, t);
        mix.position.Y = Lerp(start.position.Y, end.position.Y, t);
        mix.rotation = Lerp(start.rotation, end.rotation, t);
        return mix;
    }

    public static double GetHandleLength(double rx, double ry, double width, double height)
    {
        double x = width * rx, y = height * ry;
        return Math.Sqrt(x * x + y * y) / 4;
    }

    public static double SnapNearOne(double scale, double near = 0.1, double to = 1) => Math.Abs(scale - to - 0.001) <= near ? to : scale;

    public static double GetGiantTextureScale(double width, double height)
    {
        WeaponOutConfig c = WeaponOutConfig.Instance;
        return c.GiantItemThreshold < Math.Max(width, height) ? c.GiantItemScalePercent / 100.0 : 1;
    }

    public static double GlobalTime() => Main.GlobalTimeWrappedHourly;
}

public static class Anim
{
    const double Pi = Math.PI;

    static bool Enabled => WeaponOutConfig.Instance.EnableSheathingAnim;

    static double Sheathing(double duration, double timer) => PoseMath.Clamp(1 - timer / duration, 0, 1);

    public static double ArmWaggle(double n) => Enabled ? -Math.Sin(2 * n * n * Pi) * 0.5 : 0;

    public static double ArmRaiseLower(double n, double speed = 1) => Enabled ? Math.Sin(Math.Pow(n, speed) * Pi) : 0;

    public static double LinearNormal(double d, double t) => Enabled ? Sheathing(d, t) : 0;

    public static double EaseInEaseOutNormal(double d, double t) => Enabled ? 0.5 - 0.5 * Math.Cos(Sheathing(d, t) * Pi) : 0;

    public static double EaseOutNormal(double d, double t) => Enabled ? 1 + Math.Pow(Sheathing(d, t) - 1, 3) : 0;

    public static double EaseInNormal(double d, double t) => Enabled ? Math.Pow(1 - Math.Min(t, d) / d, 3) : 0;

    public static double OverEaseOutNormal(double d, double t)
    {
        if (!Enabled)
            return 0;
        double s = 1 - Math.Min(t, d) / d;
        return 0.675 - 0.5 * Math.Cos(s * Pi) - 0.175 * Math.Cos(2 * s * Pi);
    }

    public static double OverEaseNormal(double d, double t)
    {
        if (!Enabled)
            return 0;
        double s = 1 - Math.Min(t, d) / d;
        return 0.55 - 0.55 * Math.Cos(s * 1.2 * Pi);
    }
}
