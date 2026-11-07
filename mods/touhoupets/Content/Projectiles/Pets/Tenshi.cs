// Tenshi Hinanawi: rides her keystone, eats peaches, and is bored of heaven.
// Iku keeps an eye on her.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Tenshi : BasicTouhouPet
{
    protected override Color InitialColor => new Color(69, 170, 234);

    int stoneFrame;
    int stoneFrameCounter;
    int blinkFrame;
    int blinkFrameCounter;
    int clothFrame;
    int clothFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 14;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = false;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        DrawFrame(d, stoneFrame, color, 2, 1);
        DrawFrame(d, clothFrame + 4, color, 2, 1);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color, 2, 0);
        DrawFrame(d, clothFrame, color, 2, 1);
        return false;
    }

    void Blink()
    {
        if (blinkFrame < 11)
            blinkFrame = 11;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 13)
        {
            blinkFrame = 11;
            PetState = 0;
        }
    }

    void EatingPeach()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 1)
            Projectile.frame = 1;
        if (++Projectile.frameCounter > 7)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            if (Projectile.frame > 8)
            {
                Projectile.frame = 7;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(3, 6))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else if (ai[0] == 1)
        {
            if (Projectile.frame > 9)
            {
                Projectile.frame = 5;
                ai[0] = 2;
            }
        }
        else if (ai[0] == 2)
        {
            Projectile.frame = 5;
            ai[1]++;
            if (ai[1] > Main.rand.Next(180, 360))
            {
                ai[1] = 0;
                ai[2]++;
                if (ai[2] > Main.rand.Next(3, 6))
                {
                    ai[2] = 0;
                    ai[0] = 3;
                }
                else
                {
                    ai[0] = 0;
                }
            }
        }
        else
        {
            if (Projectile.frame == 9)
                Projectile.frame = 10;
            if (Projectile.frame > 10)
            {
                Projectile.frame = 0;
                ai[1] = 0;
                ai[2] = 0;
                ai[0] = 1800;
                PetState = 0;
            }
        }
    }

    void UpdateStoneFrame()
    {
        if (stoneFrame < 8)
            stoneFrame = 8;
        if (++stoneFrameCounter > 6)
        {
            stoneFrameCounter = 0;
            stoneFrame++;
        }
        if (stoneFrame > 10)
            stoneFrame = 8;
    }

    void UpdateClothAndHairFrame()
    {
        if (++clothFrameCounter > 4)
        {
            clothFrameCounter = 0;
            clothFrame++;
        }
        if (clothFrame > 3)
            clothFrame = 0;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        // Iku answers line 2.
        return [new(1, "1"), new(2, "2"), new(3, "3")];
    }

    protected override void VisualEffectForPreview()
    {
        UpdateStoneFrame();
        UpdateClothAndHairFrame();
    }

    public override void AI()
    {
        ModUtils.SetPetActive<TenshiBuff>(Projectile, player);
        if (mainTimer % 720 == 0 && Main.rand.NextBool(7))
            SetChat(myColor);

        var withIku = Owns<Iku>();
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.005f;
        ChangeDir(player, !withIku);
        MoveToPoint((withIku ? 50 : -50) * player.direction, -30 + player.gfxOffY, 15);

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState != 1)
        {
            if (t % 860 == 0 && Main.rand.NextBool(6) && extraAI[0] <= 0 && player.velocity.Length() < 4)
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
        else if (PetState == 2)
        {
            EatingPeach();
        }
    }
}
