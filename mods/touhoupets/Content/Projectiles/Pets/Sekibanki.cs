// Sekibanki: a rokurokubi whose head floats above her body. A loner... who
// sometimes strikes a hero pose and transforms.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Sekibanki : BasicTouhouPet
{
    protected override Color InitialColor => new Color(255, 105, 105);

    int blinkFrame;
    int blinkFrameCounter;
    int headFrame;
    int headFrameCounter;
    int headBaseY;
    int headAdjX;
    int headAdjY;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 17;
        Main.projPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        var hx = headAdjX;
        var hy = headAdjY;
        DrawFrame(d, headFrame, color, 2, 1, hx, -2 * headBaseY * Main.essScale + hy);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color, 2, 1, hx, -2 * headBaseY + hy);
        DrawFrame(d, Projectile.frame, color, 2, 0);
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
        if (blinkFrame > 12)
        {
            blinkFrame = 11;
            PetState = 0;
        }
    }

    void Idle()
    {
        if (++Projectile.frameCounter > 7)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 4)
            Projectile.frame = 0;
    }

    void Pose()
    {
        int[] ai = extraAI;
        if (++Projectile.frameCounter > 6)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            if (Projectile.frame == 6)
                Projectile.frame = 9;
            if (Projectile.frame > 13)
            {
                Projectile.frame = 11;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(2, 5))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else if (Projectile.frame > 16)
        {
            Projectile.frame = 0;
            PetState = 0;
            ai[0] = 3600;
        }
    }

    void Henshin()
    {
        if (++Projectile.frameCounter > 4)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (extraAI[0] == 0)
        {
            if (Projectile.frame > 8)
                Projectile.frame = 7;
        }
        else
        {
            if (Projectile.frame == 10)
                Projectile.frame = 15;
            if (Projectile.frame > 16)
            {
                Projectile.frame = 0;
                PetState = 0;
            }
        }
    }

    void UpdateHeadFrame()
    {
        if (++headFrameCounter > 6)
        {
            headFrameCounter = 0;
            headFrame++;
        }
        if (PetState == 2 && Projectile.frame >= 6)
        {
            if (headFrame > 10)
                headFrame = 5;
        }
        else if (headFrame > 5)
        {
            if (headFrame > 10)
                headFrame = 0;
        }
        else if (headFrame > 4)
        {
            headFrame = 0;
        }
        if (headFrame >= 5 && PetState == 2 && headBaseY < 15)
            headBaseY++;
    }

    void UpdateHeadPosition()
    {
        if (PetState != 2 && headBaseY > 0)
            headBaseY -= 2;
        if (headBaseY < 0)
            headBaseY = 0;
        headAdjX = 0;
        headAdjY = 0;
        int f = Projectile.frame;
        if (f >= 5 && f <= 16)
        {
            headAdjY = -2;
            if (f >= 10 && f <= 15)
                headAdjX = -2 * Projectile.spriteDirection;
        }
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [new(1, "1"), new(2, "2")];
        // The start of a story she tells in three lines.
        if (talkInterval <= 0)
            lines.Add(new(3, "3"));
        return lines;
    }

    // Each line here follows one of her own (FindChainedChat): "I go my own
    // way..." can turn into a transformation, and "I've always worn a cloak,"
    // goes on for two more lines.
    void UpdateTalking()
    {
        var c = myColor;
        if (FindChainedChat(1) && PetState <= 1)
        {
            if (Main.rand.NextBool(7))
            {
                extraAI[0] = 0;
                SetChatWithOtherOne(null, Text("Henshin1"), c, 6, 600);
            }
            else
            {
                ChatIndex = 0;
            }
        }
        else if (FindChainedChat(6))
        {
            PetState = 3;
            SetChatWithOtherOne(null, Text("Henshin2"), c, 7, 600, -1, 30, true);
        }
        else if (FindChainedChat(7))
        {
            SetChatWithOtherOne(null, Text("Henshin3"), c, 8, 600, -1, 40);
        }
        else if (FindChainedChat(8))
        {
            extraAI[0] = 10800;
            SetChatWithOtherOne(null, Text("Henshin4"), c, 0, 600);
        }
        if (FindChainedChat(3))
        {
            SetChatWithOtherOne(null, Text("Story2"), c, 4, 600, -1, 9);
        }
        else if (FindChainedChat(4))
        {
            SetChatWithOtherOne(null, Text("Story3"), c, 5, 600, -1, 9);
        }
        else if (FindChainedChat(5))
        {
            SetChatWithOtherOne(null, Text("Story4"), c, 0, 360);
            talkInterval = 3600;
        }
        else if (mainTimer % 960 == 0 && Main.rand.NextBool(7))
        {
            SetChat(c);
        }
    }

    protected override void VisualEffectForPreview()
    {
        UpdateHeadFrame();
        if (PetState <= 1)
            Idle();
    }

    public override void AI()
    {
        ModUtils.SetPetActive<SekibankiBuff>(Projectile, player);
        UpdateTalking();

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.032f;
        ChangeDir(player, true);
        MoveToPoint(-40 * player.direction, -30 + player.gfxOffY, 7);

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState == 0)
        {
            if (t % 400 == 0 && Main.rand.NextBool(7) && extraAI[0] <= 0)
                PetState = 2;
        }
        if (PetState <= 1)
        {
            if (PetState == 1)
                Blink();
            if (extraAI[0] >= 1)
                extraAI[0]--;
        }
        else if (PetState == 2)
        {
            Pose();
        }
        else if (PetState == 3)
        {
            Henshin();
        }
        UpdateHeadPosition();
    }
}
