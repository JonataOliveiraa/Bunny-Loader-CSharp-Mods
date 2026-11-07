using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Projectiles.Weapons.Magic;

public class MilasBookProjectile : ModProjectile
{
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 15;
    }

    public override void SetDefaults()
    {
        Projectile.width = 8;
        Projectile.height = 13;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
        Projectile.extraUpdates = 1;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 900;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.alpha = 255;
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, 1f, 1f, 1f);
        if (++Projectile.frameCounter >= 20)
        {
            Projectile.frameCounter = 0;
            Projectile.frame = Random.Shared.Next(15);
        }
        Projectile.alpha = Math.Max(0, Projectile.alpha - 15);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Vector2 center = Projectile.Center;
        for (int i = 0; i < 4; i++)
        {
            float angle = i * MathF.PI / 2 * (1 + Random.Shared.NextSingle() * 0.2f);
            var at = new Vector2(center.X + MathF.Cos(angle) * 18, center.Y + MathF.Sin(angle) * 18);
            int index = Dust.NewDust(at, 1, 1, DustID.WhiteTorch, MathF.Cos(angle) * 12, MathF.Sin(angle) * 12, 0, Color.White, 0.5f + Random.Shared.NextSingle() * 0.8f);
            if (index >= 0 && index < Main.dust.Length)
                Main.dust[index].noGravity = true;
        }
        Projectile.Kill();
    }
}
