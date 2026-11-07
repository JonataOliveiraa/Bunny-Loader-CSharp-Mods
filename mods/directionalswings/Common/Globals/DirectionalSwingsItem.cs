using System;
using DirectionalSwingsMod.Common.Configs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace DirectionalSwingsMod.Common.Globals;

public class DirectionalSwingsItem : GlobalItem
{
    const float Reach = 1.6f;      // hitbox size: 160%
    const float Arm = 11f;         // arm reach, in pixels
    const float QuarterPi = MathF.PI / 4f;

    static int lastAnim;
    static float lockedAim;

    // Only swung melee weapons; tools only when the option is on.
    static bool Applies(Item item, Player player, DirectionalSwingsConfig config)
    {
        if (!config.Enabled || player.whoAmI != Main.myPlayer)
            return false;
        if (item.useStyle != 1 || item.noMelee || item.damage <= 0 || player.gravDir != 1f)
            return false;
        return config.Tools || (item.pick <= 0 && item.axe <= 0 && item.hammer <= 0);
    }

    // The angle of the blade tip at itemRotation = 0: the sprite points to the upper front corner.
    static float BaseAngle(int direction) => direction == 1 ? -QuarterPi : -3f * QuarterPi;

    static float Wrap(float angle)
    {
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        return angle;
    }

    static bool TryAimAngle(Player player, out float angle)
    {
        // Main.MouseWorld is the world point the touch or aim points at.
        Vector2 offset = Main.MouseWorld - player.Center;
        angle = 0f;
        if (!float.IsFinite(offset.X) || !float.IsFinite(offset.Y) || (MathF.Abs(offset.X) < 1f && MathF.Abs(offset.Y) < 1f))
            return false;
        angle = MathF.Atan2(offset.Y, offset.X);
        return true;
    }

    // Runs after the game computes the swing rotation: turns the whole arc toward the aim.
    public override void UseStyle(Item item, Player player, Rectangle heldItemFrame)
    {
        DirectionalSwingsConfig config = DirectionalSwingsConfig.Instance;
        if (!Applies(item, player, config))
            return;

        int anim = player.itemAnimation;
        if (anim <= 0)
        {
            lastAnim = 0;
            return;
        }
        if ((anim > lastAnim || !config.LockAim) && TryAimAngle(player, out float aim))
            lockedAim = aim;
        lastAnim = anim;

        int direction = player.direction;
        // The normal arc is centered on rotation -direction * 0.3: move that center to the aim.
        float arc = config.Arc / 100f;
        player.itemRotation = Wrap(lockedAim - BaseAngle(direction) + arc * (player.itemRotation + direction * 0.3f));

        // The hand stays at the shoulder, in the direction the blade points.
        float alpha = BaseAngle(direction) + player.itemRotation;
        Vector2 center = player.Center;
        player.itemLocation = new Vector2(center.X + MathF.Cos(alpha) * Arm, center.Y - 4f + MathF.Sin(alpha) * Arm);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, alpha - MathF.PI / 2f);
    }

    // After the game's hitbox: replaces it with a box over the blade.
    public override void UseItemHitbox(Item item, Player player, ref Rectangle hitbox, ref bool noHitbox)
    {
        if (noHitbox || !Applies(item, player, DirectionalSwingsConfig.Instance))
            return;

        float alpha = BaseAngle(player.direction) + player.itemRotation;
        float scale = player.GetAdjustedItemScale(item);
        float length = MathF.Sqrt(item.width * item.width + item.height * item.height) * scale * Reach;
        float side = MathF.Max(20f, length * 0.6f);
        float x = player.itemLocation.X + MathF.Cos(alpha) * length * 0.6f;
        float y = player.itemLocation.Y + MathF.Sin(alpha) * length * 0.6f;
        hitbox = new Rectangle((int)MathF.Round(x - side / 2f), (int)MathF.Round(y - side / 2f), (int)MathF.Round(side), (int)MathF.Round(side));
    }
}
