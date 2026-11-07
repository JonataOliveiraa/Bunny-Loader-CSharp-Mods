using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using umamusumevanities.Content.Effects.Tails;

namespace umamusumevanities.Common.Players;

public class UmamusumeTailPlayer : ModPlayer
{
    static readonly string[] names =
    {
        "BlackTail", "DeepBrownTail", "OrangeBrownTail",
        "PinkBrownTail", "RoseBrownTail", "WhiteTail",
    };

    static Dictionary<int, int>? tailTypes;
    static Dictionary<int, int>? waistSlots;
    static int visualType;

    public float tailRotation;
    public float tailVelocity;
    public float bouncePhase;

    static Dictionary<int, int> GetTailTypes()
    {
        if (tailTypes != null)
            return tailTypes;
        var types = new Dictionary<int, int>();
        for (int i = 0; i < names.Length; i++)
        {
            if (ModContent.TryFind("umamusumevanities", names[i], out ModItem item))
                types[item.Type] = i;
        }
        if (types.Count == names.Length)
            tailTypes = types;
        return types;
    }

    public static int GetVisibleUmaTail(Player player)
    {
        Dictionary<int, int> types = GetTailTypes();
        GameArray<bool> hidden = player.hideVisibleAccessory;
        int limit = Math.Min(player.armor.Length, 20);
        for (int i = 0; i < limit; i++)
        {
            if (i < hidden.Length && hidden[i])
                continue;
            Item item = player.armor[i];
            if (!item.IsNull && item.stack > 0 && types.TryGetValue(item.type, out int tail))
                return tail;
        }
        return -1;
    }

    Dictionary<int, int> GetWaistSlots()
    {
        if (waistSlots != null)
            return waistSlots;
        var slots = new Dictionary<int, int>();
        int hidden = EquipLoader.GetEquipSlot(Mod, "UmaTailBlank", EquipType.Waist);
        if (hidden < 0)
            return slots;
        foreach (string name in names)
        {
            int slot = EquipLoader.GetEquipSlot(Mod, name, EquipType.Waist);
            if (slot >= 0)
                slots[slot] = hidden;
        }
        if (slots.Count == names.Length)
            waistSlots = slots;
        return slots;
    }

    public override void PostUpdate()
    {
        Player player = Player;
        bool visible = player.active && !player.dead && !(!player.mount.IsNull && player.mount.Active);
        int tail = visible ? GetVisibleUmaTail(player) : -1;
        if (tail < 0)
        {
            tailVelocity *= 0.8f;
            tailRotation *= 0.8f;
            return;
        }

        float vx = player.velocity.X;
        float vy = player.velocity.Y;
        bouncePhase += (Math.Abs(vx) + Math.Abs(vy)) * 0.06f;
        float gate = Math.Min(1, Math.Abs(vx) / 2);
        float target = Math.Max(-0.8f, Math.Min(0.8f, vx * 0.05f + vy * 0.015f * player.direction + MathF.Sin(bouncePhase) * 0.09f * gate));
        tailVelocity += (target - tailRotation) * 0.06f;
        tailVelocity *= 0.86f;
        tailRotation += tailVelocity;

        if (player.whoAmI != Main.myPlayer)
            return;
        if (visualType == 0)
            visualType = ModContent.ProjectileType<UmamusumeTailVisual>();
        if (visualType <= 0 || player.ownedProjectileCounts[visualType] > 0)
            return;
        Projectile.NewProjectile(player.GetProjectileSource_Item(player.HeldItem), player.Center, Vector2.Zero, visualType, 0, 0, player.whoAmI, tail, 0, 0);
    }

    public override void FrameEffects()
    {
        Player player = Player;
        if (!player.active || player.dead || (!player.mount.IsNull && player.mount.Active))
            return;
        if (GetVisibleUmaTail(player) < 0)
            return;
        if (visualType == 0)
            visualType = ModContent.ProjectileType<UmamusumeTailVisual>();
        if (visualType <= 0 || player.ownedProjectileCounts[visualType] <= 0)
            return;
        if (GetWaistSlots().TryGetValue(player.waist, out int blank))
            player.waist = (sbyte)blank;
    }
}
