using System;
using ImprovedMovementMod.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ImprovedMovementMod.Common.Players;

public class MovementPlayer : ModPlayer
{
    bool leaning;
    bool looking;
    uint spinTick = uint.MaxValue;

    bool Busy() => Player.sleeping.isSleeping || Player.sitting.isSitting || Player.petting.isPetting || Player.isDisplayDollOrInanimate;

    bool MountBlocks(MovementConfig config)
    {
        if (!Player.mount.Active)
            return false;
        return !config.EnableMountLeaning || Lists.MountBlacklist.Contains(Player.mount.Type) || Player.mount.Cart;
    }

    Vector2 MouseOffset() => Main.MouseWorld - Player.Center;

    public override void PostUpdate()
    {
        if (!Player.dead && Player.whoAmI == Main.myPlayer)
            Face(MovementConfig.Instance);
    }

    public void BeforeDraw()
    {
        if (Player.dead)
            return;
        MovementConfig config = MovementConfig.Instance;
        Look(config, Player.whoAmI == Main.myPlayer);
        Lean(config);
    }

    void Face(MovementConfig config)
    {
        if (config.FaceCursor == FaceCursorMode.Off || Busy())
            return;
        if (config.DisableRopeTurning && Player.pulley)
            return;
        if (config.FaceCursor == FaceCursorMode.UsingItem && Player.itemAnimation <= 0)
            return;
        int direction = MathF.Sign(MouseOffset().X);
        if (direction != 0 && direction != Player.direction)
            Player.ChangeDir(direction);
    }

    void Look(MovementConfig config, bool local)
    {
        if (config.LookingType == LookingMode.Off || !local || Busy())
        {
            if (looking)
            {
                Player.headRotation = 0f;
                looking = false;
            }
            return;
        }
        Vector2 offset = MouseOffset();
        if (MathF.Sign(offset.X) != Player.direction)
        {
            if (looking)
                Player.headRotation *= 0.8f;
            return;
        }
        float angle = MathF.Atan2(offset.Y * Player.direction, offset.X * Player.direction) * Player.gravDir;
        Player.headRotation = config.LookingType == LookingMode.Clamped ? Math.Clamp(angle, -0.5f, 0.4f) : angle * 0.4f;
        looking = true;
    }

    void Lean(MovementConfig config)
    {
        if (config.LeaningType == LeaningMode.Off || Busy() || MountBlocks(config))
        {
            if (leaning && !Busy())
                Player.fullRotation = 0f;
            leaning = false;
            return;
        }
        float strength = config.LeanStrength;
        float limit = 0.157f * strength;
        float rotation;
        switch (config.LeaningType)
        {
            case LeaningMode.Alternate:
                rotation = Math.Clamp((Player.velocity.X * Player.runAcceleration + Player.velocity.Y * 0.1f * -Player.direction) * strength, -limit, limit);
                break;
            case LeaningMode.Spin:
                uint tick = Main.GameUpdateCount;
                float step = spinTick == tick ? 0f : (Player.velocity.X - Player.velocity.Y * -Player.direction) * 0.05f * strength;
                spinTick = tick;
                rotation = (Player.fullRotation + step) % (MathF.PI * 2f);
                break;
            default:
                rotation = Math.Clamp(Player.velocity.X * Player.runAcceleration * strength, -limit, limit);
                break;
        }
        Player.fullRotation = rotation;
        Player.fullRotationOrigin = new Vector2(Player.width / 2f, Player.height / 2f);
        leaning = true;
    }

    public override void HideDrawLayers(PlayerDrawSet drawInfo)
    {
        if (MovementConfig.Instance.HideNeckWhenLooking && MathF.Abs(Player.headRotation) > 0.05f)
            PlayerDrawLayers.NeckAcc.Hide();
    }

    public override void PostUpdateEquips()
    {
        MovementConfig config = MovementConfig.Instance;
        if (!config.EnableFlickeringFix || config.FaceCursor == FaceCursorMode.Off)
            return;
        Item held = Player.HeldItem;
        if (!held.IsNull && held.useStyle == 1 && !held.noUseGraphic && held.useTurn)
            held.useTurn = false;
    }
}
