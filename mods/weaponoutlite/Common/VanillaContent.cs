using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using static WeaponOutLiteMod.Common.PoseMath;
using ArmStretch = Terraria.Player.CompositeArmStretchAmount;

namespace WeaponOutLiteMod.Common;

public static class VanillaContent
{
    const double Pi = Math.PI;

    public static void SetupVanillaContent(Mod mod)
    {
        void Register(string method, params object[] args)
        {
            object?[] all = new object?[args.Length + 1];
            all[0] = method;
            args.CopyTo(all, 1);
            if (mod.Call(all) is not true)
                throw new InvalidOperationException(method + " ModCall Failed");
        }

        Register("RegisterItem", new int[] { ItemID.BladedGlove, ItemID.ChainKnife, ItemID.Ruler });
        Register("RegisterSpear", new int[] { ItemID.JoustingLance, ItemID.ShadowJoustingLance, ItemID.HallowJoustingLance, ItemID.MonkStaffT1, ItemID.MonkStaffT3 });
        Register("RegisterFlail", new int[] { ItemID.Flairon, ItemID.ChainGuillotines });
        Register("RegisterSmallMelee", new int[] { ItemID.Terragrim, ItemID.Arkhalis });
        Register("RegisterLargeMelee", new int[] { ItemID.BrokenHeroSword, ItemID.Zenith });
        Register("RegisterPistol", new int[] { ItemID.ConfettiGun, ItemID.Revolver, ItemID.VenusMagnum });
        Register("RegisterGun", new int[] { ItemID.CoinGun });
        Register("RegisterWhips", new int[] { ItemID.SolarEruption });
        Register("RegisterItemHoldPose", new int[] { ItemID.NebulaBlaze }, "None");
        Register("RegisterItemHoldPose", new int[] { ItemID.BouncingShield }, "None");
        Register("RegisterItemHoldPose", new int[] { ItemID.Anchor }, "StancePoleShoulder");
        Register("RegisterCustomItemStyle", new int[] { ItemID.TheAxe, ItemID.PortalGun, ItemID.ChargedBlasterCannon, ItemID.AleThrowingGlove });

        Register("RegisterCustomUpdateIdleBodyFrame", (Func<Player, Item, int, double, int>)CustomIdleBodyFrame);
        Register("RegisterCustomDrawDepth", (Func<Player, Item, int, double, int>)CustomDrawDepth);
        Register("RegisterCustomPreDrawData", (Func<Player, Item, PoseData, double, double, int, double, PoseData>)CustomDrawData);
    }

    static PoseData CustomDrawData(Player p, Item i, PoseData data, double h, double w, int bodyFrame, double timer)
    {
        bool walkDisplacement = false;
        if (i.type == ItemID.TheAxe)
        {
            data.rotation += Pi;
            data.Move(4, 10);
            if (bodyFrame >= 7 && bodyFrame % 7 <= 2)
                data.position.Y -= 2;
        }
        if (i.type == ItemID.PortalGun)
        {
            var texture = TextureAssets.Projectile[ProjectileID.PortalGun].Value;
            if (!texture.IsNull)
            {
                data.texture = texture;
                data.sourceRect = new Microsoft.Xna.Framework.Rectangle(0, 0, texture.Width, texture.Height);
                data.origin = new PoseVector(texture.Width * 0.5, texture.Height * 0.875);
            }
            else
            {
                data.texture = default;
            }
            data.rotation += Pi / 2;
            if (bodyFrame == 0)
            {
                data.Move(-6, 9);
            }
            else if (bodyFrame > 5)
            {
                data.Move(-2, 7);
                walkDisplacement = true;
            }
            else if (bodyFrame == 5 || bodyFrame == 1)
            {
                data.rotation += Pi * -0.5;
                data.Move(-11, -4);
            }
            else if (bodyFrame == 2)
            {
                data.rotation += Pi * -0.25;
                data.Move(0, 2);
            }
            else if (bodyFrame == 3)
            {
                data.Move(2, 3);
            }
            else if (bodyFrame == 4)
            {
                data.rotation += Pi * 0.25;
                data.Move(0, 3);
            }
        }
        if (i.type == ItemID.ChargedBlasterCannon || i.type == ItemID.AleThrowingGlove)
        {
            if (i.type == ItemID.AleThrowingGlove)
                data.rotation += Pi * 0.25;
            if (bodyFrame == 0)
            {
                data.rotation += Pi * 0.5;
                data.Move(-7, 6 + h / 2);
            }
            else if (bodyFrame > 5)
            {
                data.Move(-10 + w / 2, 7);
                walkDisplacement = true;
            }
            else if (bodyFrame == 5 || bodyFrame == 1)
            {
                data.rotation += Pi * -0.5;
                data.Move(-9, -2 - h / 2);
            }
            else if (bodyFrame == 2)
            {
                data.rotation += Pi * -0.25;
                data.Move(5, -7);
            }
            else if (bodyFrame == 3)
            {
                data.Move(9, 5);
            }
            else if (bodyFrame == 4)
            {
                data.rotation += Pi * 0.25;
                data.Move(5, 11);
            }
        }
        if (walkDisplacement)
            data.WithHandOffset(p);
        return data;
    }

    static int CustomIdleBodyFrame(Player p, Item i, int bodyFrame, double timer)
    {
        if (i.type == ItemID.TheAxe)
        {
            HideShield(p);
            SetArmBack(p, true, ArmStretch.ThreeQuarters, Pi * -0.3 * p.direction);
            SetArmFront(p, true, ArmStretch.Full, Pi * -0.1 * p.direction);
        }
        return bodyFrame;
    }

    static int CustomDrawDepth(Player p, Item i, int drawDepth, double timer)
    {
        if (i.type == ItemID.TheAxe)
            return 0;
        if (i.type == ItemID.PortalGun)
            return 1;
        if (i.type == ItemID.ChargedBlasterCannon || i.type == ItemID.AleThrowingGlove)
            return 1;
        return drawDepth;
    }
}
