using mitanpc.Content.Buffs;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Projectiles.Pets;

public class FlyPetProjectile : ModProjectile
{
    int customFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 2;
        Main.projPet[Type] = true;
        ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, 2, 26, false)
            .WithOffset(-22, -32)
            .WithSpriteDirection(-1);
    }

    public override void SetDefaults()
    {
        Projectile.CloneDefaults(ProjectileID.ZephyrFish);
        AIType = ProjectileID.ZephyrFish;
        Projectile.scale = 0.6f;
    }

    public override bool PreAI()
    {
        Main.player[Projectile.owner].zephyrfish = false;
        return true;
    }

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active)
        {
            Projectile.Kill();
            return;
        }
        if (!player.dead && player.FindBuffIndex(ModContent.BuffType<GameConsoleBuff>()) >= 0)
            Projectile.timeLeft = 2;
        Lighting.AddLight(Projectile.Center, 0.369f, 0.6f, 0.067f);
        Projectile.frameCounter = 0;
        if (++customFrameCounter >= 12)
        {
            customFrameCounter = 0;
            Projectile.frame = (Projectile.frame + 1) % 2;
        }
    }
}
