using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace mitanpc.Content.Projectiles;

// The invisible hit of a resident's melee swing. See TownNPCBase.AI.
public class TownMeleeStrike : ModProjectile
{
    public override string Texture => "mitanpc/Content/Items/Weapons/Melee/GameCartridge";

    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 32;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }

    public override bool PreDraw(Player player, ref Color lightColor) => false;

    public override bool? CanCutTiles() => false;
}
