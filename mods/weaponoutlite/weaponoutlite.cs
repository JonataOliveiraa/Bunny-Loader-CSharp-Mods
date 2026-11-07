using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Terraria;
using Terraria.ModLoader;
using WeaponOutLiteMod.Common;
using WeaponOutLiteMod.Common.Players;
using WeaponOutLiteMod.Common.Poses;

public class WeaponOutLite : Mod
{
    static readonly Dictionary<string, PoseGroup> GROUP_CALLS = new()
    {
        ["RegisterBow"] = PoseGroup.Bow, ["RegisterFlail"] = PoseGroup.Flail, ["RegisterGun"] = PoseGroup.Gun,
        ["RegisterGunManual"] = PoseGroup.GunManual, ["RegisterShotgun"] = PoseGroup.Shotgun, ["RegisterItem"] = PoseGroup.Item,
        ["RegisterLargeItem"] = PoseGroup.LargeItem, ["RegisterVanityItem"] = PoseGroup.VanityItem, ["RegisterLargeMelee"] = PoseGroup.LargeMelee,
        ["RegisterLargeTool"] = PoseGroup.LargeMelee, ["RegisterLauncher"] = PoseGroup.Launcher, ["RegisterMagicBook"] = PoseGroup.MagicBook,
        ["RegisterMagicItem"] = PoseGroup.MagicItem, ["RegisterPistol"] = PoseGroup.Pistol, ["RegisterPotion"] = PoseGroup.Potion,
        ["RegisterPowerTool"] = PoseGroup.PowerTool, ["RegisterRapier"] = PoseGroup.Rapier, ["RegisterRepeater"] = PoseGroup.Repeater,
        ["RegisterSmallMelee"] = PoseGroup.SmallMelee, ["RegisterSmallTool"] = PoseGroup.SmallTool, ["RegisterSpear"] = PoseGroup.Spear,
        ["RegisterStaff"] = PoseGroup.Staff, ["RegisterThrown"] = PoseGroup.Thrown, ["RegisterThrownThin"] = PoseGroup.ThrownThin,
        ["RegisterWhips"] = PoseGroup.Whips, ["RegisterYoyo"] = PoseGroup.Yoyo,
    };

    // The item types an argument of Call stands for, or null: a number, a list of them, an item,
    // a ModItem (its instance, class or "Mod/Name").
    static List<int>? TypesOf(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case int or short or ushort or byte or sbyte or long:
                return new List<int> { Convert.ToInt32(value, CultureInfo.InvariantCulture) };
            case Item item:
                return new List<int> { item.type };
            case ModItem modItem:
                return new List<int> { modItem.Type };
            case Type modItemClass:
                if (!typeof(ModItem).IsAssignableFrom(modItemClass))
                    return null;
                int classType = (int)typeof(ModContent).GetMethod(nameof(ModContent.ItemType), Type.EmptyTypes)!.MakeGenericMethod(modItemClass).Invoke(null, null)!;
                return classType > 0 ? new List<int> { classType } : null;
            case string text:
                if (int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
                    return new List<int> { number };
                return ModContent.TryFind(text, out ModItem found) ? new List<int> { found.Type } : null;
            case IEnumerable list:
                var types = new List<int>();
                foreach (object? element in list)
                {
                    List<int>? inner = TypesOf(element);
                    if (inner != null)
                        types.AddRange(inner);
                }
                return types;
        }
        return null;
    }

    static bool TryNumber(object? value, out double number)
    {
        number = 0;
        if (value is string text)
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);
        if (value is not IConvertible || value is bool)
            return false;
        number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        return double.IsFinite(number);
    }

    public override void Load()
    {
        Net.mod = this;
        ItemPoses.All();
    }

    public override void PostSetupContent()
    {
        // Here and not in Load, so an override saved for another mod's item finds its type.
        Overrides.LoadOverrides(this);
        try
        {
            VanillaContent.SetupVanillaContent(this);
        }
        catch (Exception e)
        {
            State.Log("failed to register the game's items: " + e);
        }
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI) => Net.HandlePacket(reader);

    public override object? Call(params object?[] args)
    {
        if (args.Length == 0 || args[0] is not string method)
            throw new ArgumentException("First argument must be a string!");
        object? arg = args.Length > 1 ? args[1] : null;
        object? second = args.Length > 2 ? args[2] : null;

        switch (method)
        {
            case "HidePlayerHeldItem":
                if (arg is int who && who >= 0 && who < Main.player.Length)
                    WeaponOutPlayer.Of(Main.player[who]).showHeldItemThisFrame = false;
                return false;

            case "RegisterCustomItemStyle":
            {
                List<int>? types = TypesOf(arg);
                if (types == null)
                    return false;
                foreach (int type in types)
                    State.customItemHoldStyles.Add(type);
                Classifier.InvalidateClassifier();
                return true;
            }

            case "RegisterCustomPreDrawData":
                if (arg is not Func<Player, Item, PoseData, double, double, int, double, PoseData> preDraw)
                    return false;
                if (!State.customPreDrawDataFuncs.Contains(preDraw))
                    State.customPreDrawDataFuncs.Add(preDraw);
                return true;

            case "RegisterCustomDrawDepth":
                if (arg is not Func<Player, Item, int, double, int> drawDepth)
                    return false;
                if (!State.customDrawDepthFuncs.Contains(drawDepth))
                    State.customDrawDepthFuncs.Add(drawDepth);
                return true;

            case "RegisterCustomUpdateIdleBodyFrame":
                if (arg is not Func<Player, Item, int, double, int> idleFrame)
                    return false;
                if (!State.customUpdateIdleBodyFrameFuncs.Contains(idleFrame))
                    State.customUpdateIdleBodyFrameFuncs.Add(idleFrame);
                return true;

            case "RegisterItemHoldPose":
            {
                List<int>? types = TypesOf(arg);
                if (types == null || !PoseIDs.TryFindPose(second?.ToString() ?? "", out DrawItemPoseID pose))
                    return false;
                foreach (int type in types)
                    State.priorityItemHoldPose[type] = pose;
                return true;
            }

            case "RegisterItemSpriteTurn":
            {
                List<int>? types = TypesOf(arg);
                if (types == null || !TryNumber(second, out double degrees))
                    return false;
                foreach (int type in types)
                    Overrides.SetModSpriteTurn(type, (int)PoseMath.RoundHalfUp(degrees / 90));
                return true;
            }
        }

        if (GROUP_CALLS.TryGetValue(method, out PoseGroup group))
        {
            List<int>? types = TypesOf(arg);
            if (types == null)
                return false;
            foreach (int type in types)
                State.priorityItemHoldGroups.TryAdd(type, group);
            Classifier.InvalidateClassifier();
            return true;
        }

        throw new ArgumentException("No Mod Call method '" + method + "' exists!");
    }
}
