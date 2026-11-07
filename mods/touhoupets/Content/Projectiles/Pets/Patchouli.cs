// Patchouli Knowledge: reads her book in a soft magic aura, hates to move,
// and has a question for Remilia.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Patchouli : BasicTouhouPet
{
    protected override Color InitialColor => new Color(252, 197, 238);

    int blinkFrame;
    int blinkFrameCounter;
    int clothFrame;
    int clothFrameCounter;
    int auraFrame;
    int auraFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 16;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        Color white = Projectile.GetAlpha(Color.White);

        // The aura: a copy turning around her (0, -1), then the aura itself.
        var spin = MathF.PI * 2 * Main.GlobalTimeWrappedHourly;
        Color soft = Color.Multiply(white, 0.4f);
        ModUtils.ToggleAdditive(true);
        for (int i = 0; i < 8; i++)
        {
            var a = spin + MathF.PI * 2 / 8f * i * 0.6f;
            DrawFrame(d, auraFrame, soft, 2, 1, MathF.Sin(a), -MathF.Cos(a));
        }
        ModUtils.ToggleAdditive(false);
        DrawFrame(d, auraFrame, Color.Multiply(white, 0.8f), 2, 1);

        DrawFrame(d, clothFrame, color, 2, 0);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        if (PetState == 1 || PetState == 3)
            DrawFrame(d, blinkFrame, color, 2, 0);
        return false;
    }

    void Reading()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 1)
            Projectile.frame = 1;
        if (++Projectile.frameCounter > 10)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] <= 0)
            ai[0] = Main.rand.Next(360, 540);
        if (ai[0] < 999)
        {
            if (Projectile.frame >= 3 && ai[1] < ai[0])
            {
                ai[1]++;
                Projectile.frame = 3;
            }
            if (Projectile.frame > 6)
            {
                ai[0] = Main.rand.Next(360, 540);
                ai[1] = 0;
                Projectile.frame = 3;
            }
        }
        // Moving too fast: she closes the book.
        if (Projectile.velocity.Length() > 4.5f)
            ai[0] = 999;
        if (ai[0] >= 999)
        {
            if (Projectile.frame < 7)
                Projectile.frame = 7;
            if (Projectile.frame > 8)
            {
                Projectile.frame = 0;
                ai[0] = 180;
                ai[1] = 0;
                PetState = 0;
            }
        }
    }

    // alt: the slower blink while reading, which goes back to reading.
    void Blink(bool alt = false)
    {
        var first = alt ? 10 : 9;
        if (blinkFrame < first)
            blinkFrame = first;
        if (++blinkFrameCounter > (alt ? 6 : 3))
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 11)
        {
            blinkFrame = first;
            PetState = alt ? 2 : 0;
        }
    }

    void UpdateClothFrame()
    {
        if (clothFrame < 12)
            clothFrame = 12;
        if (++clothFrameCounter > 10)
        {
            clothFrameCounter = 0;
            clothFrame++;
        }
        if (clothFrame > 15)
            clothFrame = 12;
    }

    void UpdateAuraFrame()
    {
        if (++auraFrameCounter > 3)
        {
            auraFrameCounter = 0;
            auraFrame++;
        }
        if (auraFrame > 4)
            auraFrame = 0;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [];
        if (Projectile.velocity.Length() >= 4)
            lines.Add(new(1, "1"));
        if (PetState > 1)
        {
            lines.AddRange([new(2, "2"), new(3, "3"), new(4, "4"), new(5, "5")]);
            // Remilia answers this one.
            if (talkInterval <= 0 && FindPetState<Remilia>(0) != null && !Main.dayTime)
                lines.Add(new(16, "16", 10));
        }
        else
        {
            lines.Add(new(7, "7"));
        }
        lines.Add(new(6, "6"));
        return lines;
    }

    void UpdateTalking()
    {
        BasicTouhouPet? p;
        if ((p = FindChatIndex<Remilia>(5, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToRemilia1"), myColor, 17, 600, -1, 7);
        }
        else if ((p = FindChatIndex<Remilia>(6, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToRemilia2"), myColor, 18, 600, -1, 7);
        }
        else if ((p = FindChatIndex<Remilia>(7, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToRemilia3"), myColor, 19, 360, -1);
            p.ChatIndex = 0;
            talkInterval = 3600;
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(12))
        {
            SetChat(myColor);
        }
    }

    protected override void VisualEffectForPreview()
    {
        UpdateClothFrame();
        UpdateAuraFrame();
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, 2.52f, 1.97f, 2.38f);
        ModUtils.SetPetActive<PatchouliBuff>(Projectile, player);
        UpdateTalking();

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.032f;
        ChangeDir(player);
        // With Flandre around she floats above the player instead.
        if (Owns<Flandre>())
            MoveToPoint(0, -60 + player.gfxOffY, 4.5f);
        else
            MoveToPoint(40 * player.direction, -20 + player.gfxOffY, 4.5f);

        if (mainTimer % 270 == 0)
            PetState = PetState <= 1 ? 1 : 3;
        if (PetState == 0)
        {
            if (mainTimer % 180 == 0 && Main.rand.NextBool(4) && extraAI[0] <= 0 && Projectile.velocity.Length() < 2)
            {
                PetState = 2;
            }
        }
        if (PetState == 0)
        {
            Projectile.frame = 0;
            if (extraAI[0] >= 1)
                extraAI[0]--;
        }
        else if (PetState == 1)
        {
            Projectile.frame = 0;
            Blink();
        }
        else
        {
            Reading();
            if (PetState == 1)
                PetState = 3;
            if (PetState == 3)
                Blink(true);
        }
    }
}
