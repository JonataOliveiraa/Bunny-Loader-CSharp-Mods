using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using WeaponOutLiteMod.Common.Configs;
using WeaponOutLiteMod.Common.Players;
using WeaponOutLiteMod.Common.Poses;

namespace WeaponOutLiteMod.Common;

public static class Actions
{
    public const string MOD_ID = "weaponoutlite";

    public static string T(string key, params object[] args)
    {
        string text = Language.GetTextValue("Mods." + MOD_ID + "." + key) ?? key;
        for (int i = 0; i < args.Length; i++)
            text = text.Replace("{" + i + "}", args[i].ToString());
        return text;
    }

    public static void Say(string text, byte r, byte g, byte b) => Main.NewText(text, r, g, b, false);

    static void Gold(string text) => Say(text, 255, 215, 120);

    static void Grey(string text) => Say(text, 190, 190, 190);

    static void Sound(int type) => SoundEngine.PlaySound(type, Main.LocalPlayer.Center, 1, 0f);

    static string GroupLabel(PoseGroup group) => T("PoseGroups." + PoseIDs.PoseGroupName(group));

    static string PoseName(DrawItemPoseID id)
    {
        int labelled = Array.IndexOf(PoseIDs.Labelled, id);
        if (labelled >= 0)
            return T("Poses." + (LabelledItemPose)labelled);
        return Enum.IsDefined(id) ? id.ToString() : ((int)id).ToString();
    }

    static string ItemName(Item item)
    {
        string? name = item.Name;
        if (string.IsNullOrEmpty(name))
            name = item.AffixName();
        return string.IsNullOrEmpty(name) ? "#" + item.type : name;
    }

    static bool HeldItemOrSay(out Item item)
    {
        item = Main.LocalPlayer.HeldItem;
        if (item.IsNull || item.type == 0)
        {
            Grey(T("Common.NoItem"));
            return false;
        }
        return true;
    }

    public static void ToggleShowItem()
    {
        WeaponOutPlayer modPlayer = WeaponOutPlayer.Of(Main.LocalPlayer);
        modPlayer.SetShowingHeldItem(!modPlayer.isShowingHeldItem);
        Sound(SoundID.MenuTick);
        Gold(T(modPlayer.isShowingHeldItem ? "Common.Shown" : "Common.Hidden"));
    }

    public static bool NextPoseGroup()
    {
        if (!WeaponOutConfig.Instance.EnableWeaponQuickWeaponCategorisation)
        {
            Grey(T("Common.QuickDisabled"));
            Sound(SoundID.Unlock);
            return false;
        }
        if (!HeldItemOrSay(out Item item))
            return false;
        PoseGroup original = Classifier.GetItemPoseGroupData(item, false).poseGroup;
        Overrides.Entry? entry = Overrides.OverrideEntry(item.type);
        PoseGroup current = original;
        DrawItemPoseID pose = DrawItemPoseID.Unassigned;
        if (entry != null)
        {
            current = PoseIDs.GroupNumber(entry.ForcePoseGroup);
            pose = PoseIDs.PoseNumber(entry.ForceDrawItemPose);
        }
        PoseGroup[] order = PoseIDs.PoseGroupOrder;
        int next = Array.IndexOf(order, current) + 1;
        PoseGroup group = next <= 0 || next >= order.Length ? order[1] : order[next];
        bool isOverride = true;
        if (group == original)
        {
            group = PoseGroup.Unassigned;
            isOverride = false;
        }
        Overrides.UpsertOverride(item.type, group, pose);
        Sound(isOverride ? SoundID.MenuTick : SoundID.Unlock);
        Info();
        return isOverride;
    }

    public static void ClearItemOverride()
    {
        if (!HeldItemOrSay(out Item item))
            return;
        Overrides.ClearOverride(item.type);
        Sound(SoundID.Unlock);
        Info();
    }

    public static void TurnItemSprite()
    {
        if (!HeldItemOrSay(out Item item))
            return;
        int next = (Renderer.DefaultSpriteTurn(item) + 1) % 4;
        Overrides.SetSpriteTurn(item.type, next);
        Sound(SoundID.MenuTick);
        Gold(T("Common.Turned", next * 90));
    }

    public static void ResetAllOverrides()
    {
        Overrides.ResetOverrides();
        Gold(T("Common.ResetDone"));
    }

    public static bool ForcePose(string name)
    {
        if (!HeldItemOrSay(out Item item))
            return false;
        if (!PoseIDs.TryFindLabelled(name, out LabelledItemPose labelled))
            return false;
        Overrides.Entry? entry = Overrides.OverrideEntry(item.type);
        PoseGroup group = entry != null ? PoseIDs.GroupNumber(entry.ForcePoseGroup) : PoseGroup.Unassigned;
        Overrides.UpsertOverride(item.type, group, WeaponOutConfig.PoseId(labelled));
        Sound(SoundID.MenuTick);
        Info();
        return true;
    }

    public static void Info()
    {
        if (!HeldItemOrSay(out Item item))
            return;
        Player player = Main.LocalPlayer;
        WeaponOutPlayer modPlayer = WeaponOutPlayer.Of(player);
        PoseGroupData data = Classifier.GetItemPoseGroupData(item);
        Pose pose = Classifier.SelectItemPose(player, item);
        string custom = Overrides.FindStyleOverride(item.type, out _) ? " " + T("Common.Override") : "";
        Gold(T("Common.Info", ItemName(item), GroupLabel(data.poseGroup), PoseName(pose.GetID()), custom,
            T(modPlayer.isShowingHeldItem ? "Common.Shown" : "Common.Hidden")));
    }

    public static void ListPoses() => Grey(string.Join(", ", Enum.GetNames<LabelledItemPose>()));
}
