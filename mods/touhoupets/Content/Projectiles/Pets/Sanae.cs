// Sanae Kochiya: prays in a green wind aura, flies when you go fast, and
// preaches the Moriya faith until Reimu tells her to stop.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Sanae : BasicTouhouPet
{
    protected override Color InitialColor => new Color(83, 241, 146);

    int blinkFrame;
    int blinkFrameCounter;
    int clothFrame;
    int clothFrameCounter;
    int itemFrame;
    int itemFrameCounter;
    int hairFrame;
    int hairFrameCounter;
    float auraScale;
    int extraAdjY;
    int flyTimeleft;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 15;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = false;
    }

    // Hair, body and clothes drawn again, shaking, as the aura.
    void DrawAura(in PetDrawInfo d, Color color, float x, float y, int frame)
    {
        var ay = extraAdjY;
        DrawFrame(d, hairFrame, color, 2, 1, x, y + ay);
        DrawFrame(d, frame, color, 2, 0, x, y);
        DrawFrame(d, clothFrame, color, 2, 0, x, y + ay);
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        var ay = extraAdjY;

        if (PetState == 2)
        {
            var wave = 2 * auraScale * MathF.Sin(Main.GlobalTimeWrappedHourly * 6);
            Color aura = Color.Multiply(Projectile.GetAlpha(Color.SeaGreen), 0.4f);
            ModUtils.ToggleAdditive(true);
            for (int o = 0; o < 8; o++)
            {
                for (int i = -1; i < 2; i++)
                {
                    DrawAura(d, aura, 0, wave * i, Projectile.frame);
                    DrawAura(d, aura, wave * i, 0, Projectile.frame);
                }
            }
            ModUtils.ToggleAdditive(false);
        }

        if (PetState < 3)
            DrawFrame(d, hairFrame, color, 2, 1, 0, ay);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        if (PetState < 3)
        {
            DrawFrame(d, itemFrame, color, 2, 1);
            DrawFrame(d, clothFrame, color, 2, 0, 0, ay);
        }
        if (PetState == 1 || PetState == 4)
            DrawFrame(d, blinkFrame, color, 2, 0);
        return false;
    }

    void Blink()
    {
        if (blinkFrame < 7)
            blinkFrame = 7;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 9)
        {
            blinkFrame = 7;
            PetState = PetState == 4 ? 3 : 0;
        }
    }

    void UpdateClothFrame()
    {
        if (clothFrame < 10)
            clothFrame = 10;
        if (++clothFrameCounter > (PetState == 2 ? 3 : 5))
        {
            clothFrameCounter = 0;
            clothFrame++;
        }
        if (clothFrame > 13)
            clothFrame = 10;
    }

    void UpdateHairFrame()
    {
        if (hairFrame < 8)
            hairFrame = 8;
        if (++hairFrameCounter > (PetState == 2 ? 3 : 6))
        {
            hairFrameCounter = 0;
            hairFrame++;
        }
        if (hairFrame > 11)
            hairFrame = 8;
    }

    void UpdateItemFrame()
    {
        if (++itemFrameCounter > (PetState == 2 ? 3 : 7))
        {
            itemFrameCounter = 0;
            itemFrame++;
        }
        if (PetState == 2)
        {
            if (Projectile.frame > 0)
                itemFrame = Projectile.frame + 3;
            return;
        }
        if (itemFrame > 3)
            itemFrame = 0;
    }

    void Pray()
    {
        int[] ai = extraAI;
        chatFuncIsOccupied = true;
        if (Projectile.frame < 1)
            Projectile.frame = 1;
        if (auraScale < 2)
            auraScale += 0.02f;
        if (++Projectile.frameCounter > (Projectile.frame >= 2 && Projectile.frame <= 3 ? 3 : 6))
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 3 && ai[1] < ai[2])
        {
            Projectile.frame = 2;
            ai[1]++;
        }
        if (Projectile.frame > 4)
        {
            Projectile.frame = 0;
            ai[0] = 4800;
            ai[1] = 0;
            ai[2] = 0;
            PetState = 0;
        }
    }

    void Flying()
    {
        if (Projectile.frame < 5)
            Projectile.frame = 5;
        if (++Projectile.frameCounter > 3)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 6)
            Projectile.frame = 5;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        // Reimu answers line 4.
        List<ChatLine> lines = [new(1, "1"), new(2, "2"), new(3, "3"), new(4, "4")];
        if (Main.IsItAHappyWindyDay)
            lines.Add(new(5, "5"));
        return lines;
    }

    protected override void VisualEffectForPreview()
    {
        UpdateClothFrame();
        UpdateHairFrame();
        UpdateItemFrame();
    }

    public override void AI()
    {
        var light = auraScale * 0.75f * Main.essScale;
        Lighting.AddLight(Projectile.Center, 0.83f * light, 2.41f * light, 1.46f * light);
        if (flyTimeleft > 0)
            flyTimeleft--;
        ModUtils.SetPetActive<SanaeBuff>(Projectile, player);
        if (PetState != 2 && mainTimer % 960 == 0 && Main.rand.NextBool(7))
            SetChat(myColor);

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.012f;
        if (PetState != 2)
        {
            ChangeDir(player, PetState < 3);
            Lighting.AddLight(Projectile.Center, 0.62f, 1.02f, 0.95f);
        }
        MoveToPoint(50 * player.direction, -30 + player.gfxOffY, 22);

        int t = mainTimer;
        if (t % 270 == 0 && PetState == 0)
            PetState = 1;
        else if (t % 270 == 0 && PetState == 3)
            PetState = 4;
        if (t >= 1200 && t < 3600 && PetState != 1 && PetState < 3)
        {
            if (t % 480 == 0 && Main.rand.NextBool(5) && extraAI[0] <= 0)
            {
                PetState = 2;
                extraAI[2] = Main.rand.Next(120, 240);
            }
        }
        if (player.velocity.Length() > 15)
        {
            flyTimeleft = 5;
            if (PetState < 3)
                PetState = 3;
        }
        else if (flyTimeleft <= 0 && PetState >= 3)
        {
            extraAI[0] = 960;
            extraAI[1] = 0;
            extraAI[2] = 0;
            PetState = 0;
        }

        int state = PetState;
        if (state == 0)
        {
            Projectile.frame = 0;
            if (extraAI[0] >= 1)
                extraAI[0]--;
        }
        else if (state == 1)
        {
            Projectile.frame = 0;
            Blink();
        }
        else if (state == 2)
        {
            Pray();
        }
        else if (state == 3)
        {
            Flying();
        }
        else if (state == 4)
        {
            Flying();
            Blink();
        }

        if (PetState != 2)
        {
            if (chatFuncIsOccupied)
                chatFuncIsOccupied = false;
            if (auraScale > 0)
                auraScale -= 0.01f;
        }
        else if (Main.rand.NextBool(3))
        {
            var dustType = Main.rand.Next(4) == 1 ? MyDustId.TrailingGreen1 : MyDustId.GreenTrans;
            var dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dustType, 0, Main.rand.Next(-3, -1), 100,
                new Color(0, 0, 0, 0), auraScale * 0.45f);
            dust.noGravity = true;
        }
        extraAdjY = Projectile.frame >= 2 && Projectile.frame <= 3 ? -2 : 0;
    }
}
