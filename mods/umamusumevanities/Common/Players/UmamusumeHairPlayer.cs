using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using umamusumevanities.Content.Effects.Hair;

namespace umamusumevanities.Common.Players;

public class UmamusumeHairPlayer : ModPlayer
{
    static readonly string[] names =
    {
        "SymboliRudolfHair", "TokaiTeioHair", "OguriCapHair", "GoldShipHair",
        "RiceShowerHair", "ManhattanCafeHair", "AgnesTachornHair", "MatikanetannhauserHair",
    };

    static Dictionary<int, int>? hairTypes;
    static int visualType;

    public float rotation;
    public float angularVelocity;
    public float phase;

    static Dictionary<int, int> GetHairTypes()
    {
        if (hairTypes != null)
            return hairTypes;
        var types = new Dictionary<int, int>();
        for (int i = 0; i < names.Length; i++)
        {
            if (ModContent.TryFind("umamusumevanities", names[i], out ModItem item))
                types[item.Type] = i;
        }
        if (types.Count == names.Length)
            hairTypes = types;
        return types;
    }

    public static int GetVisibleUmaHair(Player player)
    {
        Dictionary<int, int> types = GetHairTypes();
        Item vanity = player.armor[10];
        Item equipped = !vanity.IsNull && vanity.stack > 0 ? vanity : player.armor[0];
        if (equipped.IsNull || equipped.stack <= 0)
            return -1;
        return types.TryGetValue(equipped.type, out int hair) ? hair : -1;
    }

    public override void PostUpdate()
    {
        Player player = Player;
        bool visible = player.active && !player.dead && !(!player.mount.IsNull && player.mount.Active);
        int index = visible ? GetVisibleUmaHair(player) : -1;
        if (index < 0)
        {
            rotation *= 0.8f;
            angularVelocity *= 0.8f;
            return;
        }

        float vx = player.velocity.X;
        float vy = player.velocity.Y;
        float speed = Math.Abs(vx);
        phase += 0.05f + Math.Min(speed, 12) * 0.07f;
        float running = Math.Min(1, speed / 3);
        float flutter = MathF.Sin(phase) * (0.02f + running * 0.08f);
        float target = Math.Max(-0.72f, Math.Min(0.72f, vx * 0.078f - vy * 0.016f * player.direction + flutter));
        angularVelocity = (angularVelocity + (target - rotation) * 0.095f) * 0.86f;
        rotation += angularVelocity;

        if (player.whoAmI != Main.myPlayer)
            return;
        if (visualType == 0)
            visualType = ModContent.ProjectileType<UmamusumeHairVisual>();
        if (visualType <= 0 || player.ownedProjectileCounts[visualType] > 0)
            return;
        Projectile.NewProjectile(player.GetProjectileSource_Item(player.HeldItem), player.Center, Vector2.Zero, visualType, 0, 0, player.whoAmI, index, 0, 0);
    }
}
