// Clownpiece: Hecatia's fairy, comes with her (HecatiaBuff) and carries a
// torch of hell fire.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Piece : BasicTouhouPet
{
    protected override Color InitialColor => new Color(255, 119, 187);

    int blinkFrame;
    int blinkFrameCounter;
    int wingFrame;
    int wingFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 13;
        Main.projPet[Type] = true;
    }

    // The torch, and its flame flickering in additive light.
    void DrawTorch(in PetDrawInfo d, Color color)
    {
        DrawFrame(d, 11, color);
        Color flame = Color.Multiply(Projectile.GetAlpha(Color.White), 0.5f);
        ModUtils.ToggleAdditive(true);
        for (int i = 0; i < 7; i++)
        {
            var jx = (Random.Shared.Next(21) - 10) * 0.25f;
            var jy = (Random.Shared.Next(21) - 10) * 0.25f;
            DrawFrame(d, 12, flame, 1, 0, jx, jy);
        }
        ModUtils.ToggleAdditive(false);
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        DrawFrame(d, wingFrame, color);
        DrawFrame(d, Projectile.frame, color);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color);
        DrawTorch(d, color);
        return false;
    }

    void Blink()
    {
        if (blinkFrame < 8)
            blinkFrame = 8;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 10)
        {
            blinkFrame = 8;
            PetState = 0;
        }
    }

    void Idle()
    {
        if (++Projectile.frameCounter > 6)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 3)
            Projectile.frame = 0;
    }

    void UpdateWingFrame()
    {
        if (wingFrame < 4)
            wingFrame = 4;
        if (++wingFrameCounter > 3)
        {
            wingFrameCounter = 0;
            wingFrame++;
        }
        if (wingFrame > 7)
            wingFrame = 4;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        return [new(1, "1"), new(2, "2")];
    }

    void UpdateTalking()
    {
        if (FindChatIndex<Hecatia>(2, 0, 0) != null)
            ChatCD = 1;

        BasicTouhouPet? p;
        if ((p = FindChatIndex<Hecatia>(2)) != null)
        {
            SetChatWithOtherOne(p, Text("ToHecatia"), myColor, 0, 360);
            p.ChatIndex = 0;
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(6) && mainTimer > 0)
        {
            SetChat(myColor);
        }
    }

    protected override void VisualEffectForPreview()
    {
        UpdateWingFrame();
        Idle();
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, 1.38f, 0.41f, 1.55f);
        if (Main.rand.NextBool(30))
        {
            // A spark from the torch: (26 * direction, 0) turned with her.
            Vector2 c = Projectile.Center;
            var r = Projectile.rotation;
            var ox = 26 * Projectile.spriteDirection;
            var dust = Dust.NewDustDirect(new Vector2(c.X + ox * MathF.Cos(r), c.Y + ox * MathF.Sin(r)), 4, 4, MyDustId.PurpleTorch, 0, 0, 100, new Color(0, 0, 0, 0), 1);
            if (!Main.rand.NextBool(3))
                dust.noGravity = true;
            var v = dust.velocity;
            dust.velocity = new Vector2(v.X * 0.3f, v.Y * 0.3f - 1.5f);
        }
        ModUtils.SetPetActive<HecatiaBuff>(Projectile, player);
        UpdateTalking();

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.028f;
        ChangeDir(player, true, 150);
        MoveToPoint(-115 * player.direction, -50 + player.gfxOffY, 13);

        if (mainTimer % 270 == 0 && PetState != 2)
            PetState = 1;
        if (PetState == 1)
            Blink();
    }
}
