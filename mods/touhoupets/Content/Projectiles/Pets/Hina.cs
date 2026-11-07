// Hina Kagiyama: spins round and round to keep misfortune away.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Hina : BasicTouhouPet
{
    protected override Color InitialColor => new Color(70, 226, 164);

    int blinkFrame;
    int blinkFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 17;
        Main.projPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        DrawFrame(d, Projectile.frame, color);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color);
        return false;
    }

    void Blink()
    {
        if (blinkFrame < 14)
            blinkFrame = 14;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 16)
        {
            blinkFrame = 14;
            PetState = 0;
        }
    }

    void Idle()
    {
        if (++Projectile.frameCounter > 8)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 5)
            Projectile.frame = 0;
    }

    void Turning()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 6)
            Projectile.frame = 6;
        if (++Projectile.frameCounter > 7)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            if (Projectile.frame > 13)
            {
                Projectile.frame = 6;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(10, 20))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else if (Projectile.frame > 13)
        {
            ai[2] = 0;
            Projectile.frame = 0;
            ai[0] = 300;
            PetState = 0;
        }
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        return [new(1, "1"), new(2, "2")];
    }

    void UpdateTalking()
    {
        int[] ai = extraAI;
        if (PetState == 2 && ai[1] <= 3 && mainTimer % 60 == 0 && Main.rand.NextBool(9) && ai[2] == 0)
        {
            SetChat(myColor, Text("Spin"), 3, 0, 30);
            ai[2] = 1;
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(9))
        {
            SetChat(myColor);
        }
    }

    protected override void VisualEffectForPreview()
    {
        if (PetState != 2)
            Idle();
    }

    public override void AI()
    {
        ModUtils.SetPetActive<HinaBuff>(Projectile, player);
        UpdateTalking();
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.032f;
        if (PetState != 2)
            ChangeDir(player, true);
        MoveToPoint(-40 * player.direction, -30 + player.gfxOffY, 13);

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState != 1)
        {
            if (t % 200 == 0 && Main.rand.NextBool(2) && extraAI[0] <= 0)
                PetState = 2;
        }
        if (PetState == 0)
        {
            if (extraAI[0] >= 1)
                extraAI[0]--;
        }
        else if (PetState == 1)
        {
            Blink();
        }
        else if (PetState == 2)
        {
            Turning();
        }
    }
}
