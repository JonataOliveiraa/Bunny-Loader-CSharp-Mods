// Kogasa Tatara: spins her umbrella and tries to scare you. When a boss shows
// up, she hides behind it.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Kogasa : BasicTouhouPet
{
    protected override Color InitialColor => new Color(172, 69, 191);

    int blinkFrame;
    int blinkFrameCounter;
    int handFrame;
    int handFrameCounter;
    int clothFrame;
    int clothFrameCounter;
    int umbrellaFrame;
    int umbrellaFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 20;
        Main.projPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        var eye = Projectile.spriteDirection == -1 ? 0 : 3;
        int state = PetState;
        DrawFrame(d, umbrellaFrame, color, 2, 1);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        DrawFrame(d, clothFrame, color, 2, 0);
        DrawFrame(d, handFrame, color, 2, 0);
        if (state == 1 || state == 4)
            DrawFrame(d, blinkFrame, color, 2, 0);
        if (state == 0 || state == 3)
            DrawFrame(d, 14 + eye, color, 2, 0);
        if (Projectile.frame == 1 || Projectile.frame == 4)
            DrawFrame(d, 15 + eye, color, 2, 0);
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
            PetState = PetState == 4 ? 3 : 0;
        }
    }

    void UpdateUmbrellaFrame()
    {
        int state = PetState;
        if (++umbrellaFrameCounter > 5)
        {
            umbrellaFrameCounter = 0;
            umbrellaFrame++;
            if (state == 3 || state == 4)
                extraAI[1]++;
        }
        if (state <= 2)
        {
            if (umbrellaFrame > 6)
                umbrellaFrame = 0;
        }
        else
        {
            if (state >= 5)
            {
                umbrellaFrame = 14;
                return;
            }
            if (umbrellaFrame < 7)
                umbrellaFrame = 7;
            if (umbrellaFrame > 13)
                umbrellaFrame = 7;
        }
    }

    void UpdateHandFrame()
    {
        int state = PetState;
        if (state >= 5)
        {
            handFrame = 13;
            return;
        }
        if (state <= 1)
        {
            handFrame = 10;
        }
        else if (state == 3 || state == 4)
        {
            if (handFrame < 11)
                handFrame = 11;
            if (++handFrameCounter > 5)
            {
                handFrameCounter = 0;
                handFrame++;
            }
            if (handFrame > 12)
                handFrame = 11;
        }
    }

    void UpdateClothFrame()
    {
        if (clothFrame < 6)
            clothFrame = 6;
        if (++clothFrameCounter > 5)
        {
            clothFrameCounter = 0;
            clothFrame++;
        }
        if (clothFrame > 9)
            clothFrame = 6;
    }

    void Scare()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 2)
            Projectile.frame = 2;
        if (++Projectile.frameCounter > 7)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
            ai[1]++;
        }
        ai[0] = ai[1] > ai[2] ? 1 : 0;
        if (ai[0] == 1)
        {
            if (Projectile.frame > 4)
            {
                ai[1] = 0;
                ai[2] = 0;
                ai[0] = 1200;
                Projectile.frame = 0;
                PetState = 0;
            }
        }
        else if (Projectile.frame > 3)
        {
            Projectile.frame = 2;
        }
    }

    void SpinUmbrella()
    {
        int[] ai = extraAI;
        if (ai[1] > ai[2] && umbrellaFrame == 7)
        {
            ai[1] = 0;
            ai[2] = 0;
            ai[0] = 1200;
            PetState = 0;
        }
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        return [new(1, "1"), new(2, "2")];
    }

    protected override void VisualEffectForPreview()
    {
        UpdateClothFrame();
        UpdateUmbrellaFrame();
        UpdateHandFrame();
    }

    public override void AI()
    {
        ModUtils.SetPetActive<KogasaBuff>(Projectile, player);
        if (mainTimer % 720 == 0 && Main.rand.NextBool(9) && PetState != 5)
            SetChat(myColor);

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.012f;
        ChangeDir(player, true);
        MoveToPoint(-50 * player.direction, -30 + player.gfxOffY, 12.5f);

        int t = mainTimer;
        if (t % 270 == 0)
        {
            if (PetState == 0)
                PetState = 1;
            else if (PetState == 3)
                PetState = 4;
        }
        if (t >= 1200 && t < 3600 && PetState < 1)
        {
            if (t % 120 == 0 && Main.rand.NextBool(2) && extraAI[0] <= 0)
            {
                if (Main.rand.NextBool(2))
                {
                    PetState = 3;
                    extraAI[2] = Main.rand.Next(30, 40);
                }
                else
                {
                    PetState = 2;
                    extraAI[2] = Main.rand.Next(14, 20);
                }
            }
        }
        int state = PetState;
        if (state == 0)
        {
            if (extraAI[0] >= 1)
                extraAI[0]--;
            Projectile.frame = 0;
        }
        else if (state == 1 || state == 4)
        {
            Projectile.frame = 0;
            Blink();
        }
        else if (state == 2)
        {
            Scare();
        }
        else if (state == 3)
        {
            Projectile.frame = 0;
            SpinUmbrella();
        }
        else if (state == 5)
        {
            chatFuncIsOccupied = true;
            Projectile.frame = 5;
            if (!ModUtils.AnyBosses(player))
                PetState = 0;
        }
        if (ModUtils.AnyBosses(player))
            PetState = 5;
        // The original never turned this back off: after a boss fight she
        // would not talk again.
        else if (PetState != 5)
            chatFuncIsOccupied = false;
    }
}
