using System;
using BunnyLoader.Interop;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using WeaponOutLiteMod.Common.Configs;
using WeaponOutLiteMod.Common.Poses;

namespace WeaponOutLiteMod.Common.Players;

public class WeaponOutPlayer : ModPlayer
{
    public bool isShowingHeldItem;
    public bool showHeldItemThisFrame;
    public float? expectedFrontArmThisFrame;
    public Pose? CurrentDrawItemPose;
    public int CombatDelayTimer;
    // The item shown in the menu. The handle keeps it alive between frames.
    GameHandle? gameMenuItem;
    int lastHeldType;

    static WeaponOutPlayer? template;

    public override void Initialize()
    {
        isShowingHeldItem = true;
        showHeldItemThisFrame = true;
        expectedFrontArmThisFrame = null;
        CurrentDrawItemPose = null;
        CombatDelayTimer = 0;
        gameMenuItem = null;
        lastHeldType = -1;
    }

    public static WeaponOutPlayer Of(Player player) => player.GetModPlayer(template ??= ModContent.GetInstance<WeaponOutPlayer>());

    public static bool IsLocal(Player player) => player.whoAmI == Main.myPlayer;

    public bool IsShowingHeldItem()
    {
        WeaponOutConfig config = WeaponOutConfig.Instance;
        if (!config.EnableWeaponOutVisuals)
            return false;
        if (IsLocal(Player) && isShowingHeldItem != config.ShowHeldItem)
            config.SetShowHeldItem(isShowingHeldItem);
        return isShowingHeldItem;
    }

    public void SetShowingHeldItem(bool value)
    {
        isShowingHeldItem = value;
        if (IsLocal(Player))
        {
            if (WeaponOutConfig.Instance.ShowHeldItem != isShowingHeldItem)
                WeaponOutConfig.Instance.SetShowHeldItem(isShowingHeldItem);
            Net.SendUpdateWeaponVisual(Player, this);
        }
    }

    public bool DrawHeldItem()
    {
        if (!IsShowingHeldItem() || !showHeldItemThisFrame)
            return false;
        Player player = Player;
        bool frontEnabled = player.compositeFrontArm.enabled;
        bool untouched = (!frontEnabled && expectedFrontArmThisFrame == null)
            || (frontEnabled && expectedFrontArmThisFrame == player.compositeFrontArm.rotation);
        if (!untouched)
            return false;
        return !Main.gameMenu && player.active && !player.dead && !player.stoned && player.itemAnimation <= 0;
    }

    public Item HeldItem() => Main.gameMenu && gameMenuItem != null ? new Item(gameMenuItem.Target) : Player.HeldItem;

    public override void OnEnterWorld()
    {
        if (Main.netMode != NetmodeID.SinglePlayer)
            Net.SendUpdateWeaponVisual(Player, this);
    }

    public override void ResetEffects()
    {
        expectedFrontArmThisFrame = null;
        showHeldItemThisFrame = true;
    }

    public override void PostUpdate()
    {
        if (!WeaponOutConfig.Instance.EnableWeaponOutVisuals)
            return;
        ManageCombatTimer(false);
        if (DrawHeldItem() && !Main.dedServ)
        {
            if (IsLocal(Player))
                ManageHoldStyle();
            ManageBodyFrame();
        }
    }

    public override void FrameEffects()
    {
        if (Main.gameMenu && WeaponOutConfig.Instance.EnableMenuDisplay)
        {
            if (gameMenuItem == null)
            {
                Item held = Player.HeldItem;
                if (!held.IsNull)
                    gameMenuItem = new GameHandle(held.Pointer);
            }
            CombatDelayTimer = 0;
            CurrentDrawItemPose = Classifier.SelectItemPose(Player, HeldItem());
            ManageBodyFrame();
        }
        else if (gameMenuItem != null)
        {
            gameMenuItem.Dispose();
            gameMenuItem = null;
        }
    }

    public override void OnHurt(Player.HurtInfo info)
    {
        if (WeaponOutConfig.Instance.CombatStanceWhenHurt)
            ManageCombatTimer(true);
    }

    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (fromWho == -1)
            Net.SendUpdateWeaponVisual(Player, this);
    }

    public override void SaveData(TagCompound tag) => tag["isShowingHeldItem"] = isShowingHeldItem;

    public override void LoadData(TagCompound tag)
    {
        if (tag.TryGet("isShowingHeldItem", out bool value))
            isShowingHeldItem = value;
    }

    void ManageCombatTimer(bool combatPoseTriggered)
    {
        Player player = Player;
        bool active = player.itemAnimation > 0;
        if (IsLocal(player))
        {
            WeaponOutConfig config = WeaponOutConfig.Instance;
            int max = (int)((double)config.CombatDelayTimerMax * 60);
            if (config.CombatStanceAlwaysOn)
                max = 2147483647;
            if (combatPoseTriggered || active)
                CombatDelayTimer = max;
            if (CombatDelayTimer == max && !active)
                Net.SendUpdateCombatTimer(player, this);
        }
        if (!active && !player.CCed)
        {
            CombatDelayTimer = Math.Max(0, CombatDelayTimer - 1);
            bool resting = player.sitting.isSitting || player.sleeping.isSleeping;
            if (resting && CombatDelayTimer > 120)
                CombatDelayTimer = 120;
        }
    }

    void ManageHoldStyle()
    {
        Player player = Player;
        CurrentDrawItemPose = Classifier.SelectItemPose(player, HeldItem());
        Item held = player.HeldItem;
        int type = held.IsNull ? 0 : held.type;
        if (type != lastHeldType)
        {
            lastHeldType = type;
            // A client, or the phone's host (netMode 3), which is both.
            if ((Main.netMode & NetmodeID.MultiplayerClient) != 0)
            {
                Net.SendUpdateWeaponVisual(player, this);
                Net.SendUpdateCombatTimer(player, this);
            }
        }
    }

    void ManageBodyFrame()
    {
        Player player = Player;
        Item item = HeldItem();
        if (item.IsNull || item.type == 0 || item.holdStyle != 0)
            return;
        if (CurrentDrawItemPose == null || player.sleeping.isSleeping)
            return;
        int frame = PoseMath.BodyFrameNum(player);
        bool idleFrames = frame == 0 || frame >= 5;
        bool idleArms = !player.compositeFrontArm.enabled && !player.compositeBackArm.enabled;
        if (idleFrames && idleArms)
        {
            int next = CurrentDrawItemPose.UpdateIdleBodyFrame(player, item, frame, CombatDelayTimer);
            if (next != frame)
                player.bodyFrame.Y = next * player.bodyFrame.Height;
        }
        if (player.compositeFrontArm.enabled)
            expectedFrontArmThisFrame = player.compositeFrontArm.rotation;
        else
            expectedFrontArmThisFrame = null;
    }

    public int DrawDepth()
    {
        Pose? pose = CurrentDrawItemPose;
        return pose == null ? 0 : pose.DrawDepth(Player, HeldItem(), CombatDelayTimer);
    }

    public int PoseId => CurrentDrawItemPose != null ? (int)CurrentDrawItemPose.GetID() : (int)DrawItemPoseID.Unassigned;
}
