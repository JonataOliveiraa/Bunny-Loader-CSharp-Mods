using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using umamusumevanities.Common.Audio;

namespace umamusumevanities.Content.Items.Pet.Mambo;

public class MamboProjectile : ModProjectile
{
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 5;
        Main.projPet[Type] = true;
    }

    public override void SetDefaults()
    {
        Projectile.width = 26;
        Projectile.height = 36;
        Projectile.tileCollide = true;
        Projectile.penetrate = -1;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.timeLeft = 2;
    }

    public override void OnSpawn(IEntitySource source) => Projectile.localAI[0] = Main.rand.Next(3600, 10801);

    public override bool OnTileCollide(Vector2 oldVelocity) => false;

    public override void AI()
    {
        Projectile proj = Projectile;
        Player player = Main.player[proj.owner];
        if (player.IsNull || !player.active)
            return;
        if (!player.dead && player.FindBuffIndex(ModContent.BuffType<MamboBuff>()) >= 0)
            proj.timeLeft = 2;
        proj.gfxOffY = 2;

        if (proj.owner == Main.myPlayer)
        {
            if (proj.localAI[0] <= 0)
                proj.localAI[0] = Main.rand.Next(3600, 10801);
            if (--proj.localAI[0] <= 0)
            {
                UmamusumeAudio.PlayMambo(proj.Center);
                proj.localAI[0] = Main.rand.Next(3600, 10801);
            }
        }

        Vector2 center = player.Center;
        float dx = center.X - player.direction * 40 - proj.Center.X;
        Vector2 v = proj.velocity;
        float vx = Math.Abs(dx) > 8 ? Math.Sign(dx) * Math.Min(2.7f, Math.Max(0.4f, Math.Abs(dx) * 0.15f)) : v.X * 0.6f;
        if (Math.Abs(vx) < 0.05f)
            vx = 0;
        float vy = Math.Min(v.Y + 0.35f, 10);
        bool grounded = v.Y == 0;
        if (grounded && Math.Abs(dx) > 20 && Math.Abs(v.X) < 0.2f)
            vy = -7.5f;
        if (Math.Abs(center.X - proj.Center.X) > 480 || Math.Abs(center.Y - proj.Center.Y) > 360)
        {
            proj.Center = new Vector2(center.X, center.Y - 40);
            vx = 0;
            vy = 0;
        }
        proj.velocity = new Vector2(vx, vy);
        if (Math.Abs(vx) > 0.1f)
            proj.spriteDirection = vx > 0 ? 1 : -1;

        if (!grounded)
        {
            proj.frame = 1;
            proj.frameCounter = 0;
        }
        else if (Math.Abs(vx) > 0.15f)
        {
            proj.frameCounter++;
            if (proj.frameCounter >= 6)
            {
                proj.frameCounter = 0;
                proj.frame = (proj.frame + 1) % 5;
            }
        }
        else
        {
            proj.frame = 0;
            proj.frameCounter = 0;
        }
    }
}
