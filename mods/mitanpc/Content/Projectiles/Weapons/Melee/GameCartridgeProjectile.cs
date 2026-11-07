using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Projectiles.Weapons.Melee;

// A boomerang: flies for 30 ticks or until it hits something, then returns to the owner.
public class GameCartridgeProjectile : ModProjectile
{
    public override string Texture => "mitanpc/Content/Items/Weapons/Melee/GameCartridge";

    public override void SetDefaults()
    {
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.width = 38;
        Projectile.height = 32;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 1080;
        Projectile.tileCollide = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }

    static float Steer(float current, float target)
    {
        if (current < target)
            return current + 3.2f * (current < 0 && target > 0 ? 2 : 1);
        if (current > target)
            return current - 3.2f * (current > 0 && target < 0 ? 2 : 1);
        return current;
    }

    public override void AI()
    {
        Projectile.rotation += 0.2f;
        if (Projectile.soundDelay == 0)
        {
            Projectile.soundDelay = 15;
            SoundEngine.PlaySound(SoundID.Item7, Projectile.position);
        }
        if (Projectile.ai[0] == 0)
        {
            Projectile.ai[1]++;
            if (Projectile.ai[1] >= 30)
            {
                Projectile.ai[0] = 1;
                Projectile.ai[1] = 0;
                Projectile.netUpdate = true;
            }
            return;
        }
        Projectile.tileCollide = false;
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead)
        {
            Projectile.Kill();
            return;
        }
        Vector2 center = owner.Center;
        Vector2 at = Projectile.Center;
        float dx = center.X - at.X, dy = center.Y - at.Y;
        float distance = Vector2.Distance(center, at);
        if (distance > 3000 || distance < 1 ||
            (Main.myPlayer == Projectile.owner && Projectile.Hitbox.Intersects(owner.Hitbox)))
        {
            Projectile.Kill();
            return;
        }
        Projectile.velocity.X = Steer(Projectile.velocity.X, dx * 18 / distance);
        Projectile.velocity.Y = Steer(Projectile.velocity.Y, dy * 18 / distance);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => Projectile.ai[0] = 1;

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
        SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        if (Projectile.velocity.X != oldVelocity.X)
            Projectile.velocity.X = -oldVelocity.X;
        if (Projectile.velocity.Y != oldVelocity.Y)
            Projectile.velocity.Y = -oldVelocity.Y;
        Projectile.ai[0] = 1;
        return false;
    }
}
