using System;
using System.IO;
using mitanpc.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Projectiles.Pets;

// Walks after the owner and flies when too far, like the original pet's custom AI.
public class BrokenMitaPetProjectile : ModProjectile
{
    bool fly;
    int playerStill;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 5;
        Main.projPet[Type] = true;
        ProjectileID.Sets.CharacterPreviewAnimations[Type] = ProjectileID.Sets.SimpleLoop(0, 4, 22, false)
            .WithOffset(-6, -5)
            .WithSpriteDirection(-1);
    }

    public override void SetDefaults()
    {
        Projectile.netImportant = true;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 18000;
        Projectile.tileCollide = true;
        Projectile.width = 30;
        Projectile.height = 42;
        Projectile.scale = 0.85f;
    }

    public override bool? CanDamage() => false;

    public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
    {
        fallThrough = Vector2.Distance(Main.player[Projectile.owner].Center, Projectile.Center) > 200;
        return true;
    }

    bool HoleBelow()
    {
        int tileX = (int)MathF.Floor(Projectile.Center.X / 16) - 4;
        if (Projectile.velocity.X > 0)
            tileX += 4;
        int tileY = (int)MathF.Floor((Projectile.position.Y + Projectile.height) / 16);
        for (int y = tileY; y < tileY + 2; y++)
        {
            for (int x = tileX; x < tileX + 4; x++)
            {
                if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY)
                    return false;
                if (Main.tile[x, y].active())
                    return false;
            }
        }
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
        if (!player.dead && player.FindBuffIndex(ModContent.BuffType<AlarmClockBuff>()) >= 0)
            Projectile.timeLeft = 2;
        Vector2 target = player.Center, at = Projectile.Center;
        float distance = Vector2.Distance(target, at);
        if (!fly)
            WalkAI(player, distance);
        else
            FlyAI(player, target, at);
        if (Projectile.velocity.X > 0.25f)
            Projectile.spriteDirection = -1;
        else if (Projectile.velocity.X < -0.25f)
            Projectile.spriteDirection = 1;
    }

    void WalkAI(Player player, float distance)
    {
        Projectile.rotation = 0;
        if (Projectile.velocity.Y == 0 && (HoleBelow() || (distance > 110 && Projectile.position.X == Projectile.oldPosition.X)))
            Projectile.velocity.Y = -5;
        Projectile.velocity.Y = MathF.Min(7, Projectile.velocity.Y + 0.2f);
        if (distance > 600)
        {
            fly = true;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
        }
        if (distance > 100)
        {
            Projectile.velocity.X = Math.Clamp(Projectile.velocity.X + (player.position.X > Projectile.position.X ? 0.1f : -0.1f), -7f, 7f);
        }
        else if (distance < 100)
        {
            if (Projectile.velocity.X > 0.4f)
                Projectile.velocity.X -= 0.15f;
            else if (Projectile.velocity.X < -0.4f)
                Projectile.velocity.X += 0.15f;
            else
                Projectile.velocity.X = 0;
        }
        if (Projectile.position.X == Projectile.oldPosition.X && Projectile.position.Y == Projectile.oldPosition.Y && Projectile.velocity.X == 0)
        {
            Projectile.frame = 0;
        }
        else if (Projectile.velocity.Y > 0.3f && Projectile.position.Y != Projectile.oldPosition.Y)
        {
            Projectile.frame = 1;
            Projectile.frameCounter = 0;
        }
        else if (++Projectile.frameCounter > 8)
        {
            Projectile.frameCounter = 0;
            Projectile.frame = (Projectile.frame + 1) % 4;
        }
    }

    void FlyAI(Player player, Vector2 target, Vector2 at)
    {
        Projectile.tileCollide = false;
        float x = target.X - at.X + Random.Shared.Next(31) - 10 - 60 * player.direction;
        float y = target.Y - at.Y + Random.Shared.Next(31) - 10 - 60;
        float length = MathF.Sqrt(x * x + y * y);
        float acceleration = 0.3f;
        if (length > 1200)
        {
            Projectile.position = new Vector2(target.X - Projectile.width / 2f, target.Y - Projectile.height / 2f);
            Projectile.netUpdate = true;
        }
        if (length < 100)
        {
            acceleration = 0.1f;
            playerStill = player.velocity.Y == 0 ? playerStill + 1 : 0;
            if (playerStill > 60 && !Collision.SolidCollision(Projectile.position, Projectile.width, Projectile.height))
            {
                fly = false;
                Projectile.tileCollide = true;
                playerStill = 0;
                Projectile.netUpdate = true;
            }
        }
        if (length < 50)
        {
            if (MathF.Abs(Projectile.velocity.X) > 2 || MathF.Abs(Projectile.velocity.Y) > 2)
                Projectile.velocity *= 0.9f;
            acceleration = 0.01f;
        }
        else
        {
            if (length > 300)
                acceleration = 1;
            x *= 18 / length;
            y *= 18 / length;
        }
        float ax = Projectile.velocity.X <= x ? acceleration : -acceleration;
        float ay = Projectile.velocity.Y <= y ? acceleration : -acceleration;
        Projectile.velocity.X += ax;
        Projectile.velocity.Y += ay;
        if (acceleration > 0.05f && Projectile.velocity.X * ax < 0)
            Projectile.velocity.X += ax;
        if (acceleration > 0.05f && Projectile.velocity.Y * ay < 0)
            Projectile.velocity.Y += ay * 2;
        Projectile.rotation = Projectile.velocity.X * 0.03f;
        Projectile.frame = 4;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(fly);
        writer.Write(playerStill);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        fly = reader.ReadBoolean();
        playerStill = reader.ReadInt32();
    }
}
