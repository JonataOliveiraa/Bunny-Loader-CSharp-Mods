// Utsuho Reiuji: a little sun burns over her head, and now and then she fires
// her arm cannon and gets pushed back by the blast.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Utsuho : BasicTouhouPet
{
    protected override Color InitialColor => new Color(228, 184, 75);

    int wingFrame;
    int wingFrameCounter;

    static readonly Color NO_COLOR = new Color(0, 0, 0, 0);
    // Where the sun floats, above her head.
    const int SUN_Y = -40;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 11;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        Color white = Projectile.GetAlpha(Color.White);
        DrawFrame(d, wingFrame, color);

        // The wings' glow, in the same frame of its own sheet.
        Texture2D glow = ModUtils.Texture("Assets/Textures/Glow/UtsuhoGlow");
        ModUtils.Draw(glow, new Vector2(d.x, d.y), new Rectangle(0, (int)(wingFrame * d.height), d.width, (int)d.height), white,
            d.rotation, new Vector2(d.width / 2f, d.height / 2f), d.scale, d.effect);

        Texture2D sun = ModUtils.Texture("Assets/Textures/Extra/UtsuhoSun");
        Vector2 origin = new Vector2(sun.Width / 2f, sun.Height / 2f);
        Color halo = Color.Multiply(white, 0.5f);
        for (int i = 0; i < 5; i++)
        {
            var jx = (Random.Shared.Next(21) - 10) * 0.2f;
            var jy = (Random.Shared.Next(21) - 10) * 0.2f;
            ModUtils.Draw(sun, new Vector2(d.x + jx, d.y + SUN_Y + jy), null, halo, -mainTimer * 0.09f, origin, d.scale * 1.02f, 0);
        }
        ModUtils.Draw(sun, new Vector2(d.x, d.y + SUN_Y), null, white, mainTimer * 0.05f, origin, d.scale, 0);
        DrawFrame(d, Projectile.frame, color);
        return false;
    }

    void Blink()
    {
        if (++Projectile.frameCounter > 3)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 2)
        {
            Projectile.frame = 0;
            PetState = 0;
        }
    }

    void Fire()
    {
        int[] ai = extraAI;
        if (ai[1] > 0)
            ai[1]--;
        if (Projectile.frame == 3 && Projectile.frameCounter >= 12)
        {
            Projectile.frame = 4;
            ai[1] = 60;
        }
        Vector2 c = Projectile.Center;
        var dir = Projectile.spriteDirection;
        if (ai[1] > 0 && Main.rand.NextBool(2))
        {
            Dust.NewDustPerfect(new Vector2(c.X + 26 * dir, c.Y + 6), MyDustId.OrangeFire2,
                new Vector2(Main.rand.Next(-3, 3) * 0.75f, Main.rand.Next(-3, 3) * 0.75f), 100, NO_COLOR, Main.rand.NextFloat(0.5f, 1.25f)).noGravity = true;
        }
        if (Projectile.frame < 3)
            Projectile.frame = 3;
        if (ai[1] <= 0)
            Projectile.frameCounter++;
        if (Projectile.frameCounter > 12)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        // The shot: smoke and fire out of the cannon, and the recoil.
        if (Projectile.frame == 5 && Projectile.frameCounter == 1)
        {
            for (int i = 0; i < 7; i++)
            {
                var k = Main.rand.NextFloat(0.5f, 2);
                Vector2 v = Turn(6 * dir * k, -4 * k, Main.rand.Next(-30, 30));
                Dust.NewDustDirect(c, 4, 4, MyDustId.Smoke, v.X, v.Y, 100, NO_COLOR, Main.rand.NextFloat(0.5f, 1.5f)).noGravity = true;
                k = 2 * Main.rand.NextFloat(0.5f, 2);
                v = Turn(6 * dir * k, -4 * k, Main.rand.Next(-10, 10));
                Dust.NewDustDirect(c, 4, 4, MyDustId.OrangeFire2, v.X, v.Y, 100, NO_COLOR, Main.rand.NextFloat(1.4f, 1.9f)).noGravity = true;
            }
            Projectile.velocity = new Vector2(9 * -dir, 4);
        }
        if (Projectile.frame >= 5)
            Projectile.velocity = Vector2.Multiply(Projectile.velocity, 0.85f);
        if (Projectile.frame > 6)
        {
            Projectile.frame = 0;
            ai[1] = 0;
            ai[0] = 600;
            PetState = 0;
        }
    }

    static Vector2 Turn(float x, float y, float deg)
    {
        var r = deg * MathF.PI / 180f;
        return new Vector2(x * MathF.Cos(r) - y * MathF.Sin(r), x * MathF.Sin(r) + y * MathF.Cos(r));
    }

    void UpdateWingFrame()
    {
        if (wingFrame < 7)
            wingFrame = 7;
        if (++wingFrameCounter > 6)
        {
            wingFrameCounter = 0;
            wingFrame++;
        }
        if (wingFrame > 10)
            wingFrame = 7;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [new(1, "1"), new(2, "2"), new(3, "3")];
        if (player.ZoneUnderworldHeight)
            lines.Add(new(4, "4", 3));
        // Satori answers this one.
        if (Owns<Satori>())
            lines.Add(new(5, "5"));
        return lines;
    }

    protected override void VisualEffectForPreview()
    {
        UpdateWingFrame();
    }

    public override void AI()
    {
        Vector2 c = Projectile.Center;
        Lighting.AddLight(new Vector2(c.X, c.Y + SUN_Y), 1, 0.77f, 0.32f);
        ModUtils.SetPetActive<UtsuhoBuff>(Projectile, player);
        if (mainTimer % 720 == 0 && Main.rand.NextBool(7))
            SetChat(myColor);

        // Higher up with the Komeiji sisters around.
        var height = Owns<Satori>() || Owns<Koishi>() ? -80 : -60;
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.012f;
        // While firing (frame 5 on) the recoil moves her, not the follow.
        if (Projectile.frame < 5)
        {
            ChangeDir(player);
            MoveToPoint(0, height + player.gfxOffY, 13.5f);
        }

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState != 1)
        {
            if (t % 300 == 0 && Main.rand.NextBool(7) && extraAI[0] <= 0)
                PetState = 2;
        }
        if (PetState == 0)
        {
            Projectile.frame = 0;
            if (extraAI[0] >= 1)
                extraAI[0]--;
        }
        else if (PetState == 1)
        {
            Blink();
        }
        else if (PetState == 2)
        {
            Fire();
        }
        // The cannon smokes for a while after the shot.
        if (extraAI[0] >= 480 && Main.rand.NextBool(4))
        {
            Dust.NewDustPerfect(new Vector2(c.X + 24 * Projectile.spriteDirection, c.Y + 20), MyDustId.Smoke,
                new Vector2(0, Main.rand.Next(-4, -2) * 0.5f), 100, NO_COLOR, Main.rand.NextFloat(0.5f, 1.25f)).noGravity = true;
        }
    }
}
