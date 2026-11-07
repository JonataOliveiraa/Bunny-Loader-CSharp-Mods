// Remilia Scarlet: drinks her tea, rules the night and talks with Flandre and
// Patchouli. Hides under her hat in the sunlight.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Remilia : BasicTouhouPet
{
    protected override Color InitialColor => new Color(255, 10, 10);

    int wingFrame;
    int wingFrameCounter;
    int blinkFrame;
    int blinkFrameCounter;
    int clothFrame;
    int clothFrameCounter;
    int extraAdjX;
    int extraAdjY;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 25;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = false;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        var ax = extraAdjX;
        var ay = extraAdjY;
        DrawFrame(d, wingFrame, color, 1, 0, ax, ay);
        DrawFrame(d, Projectile.frame, color);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color);
        if (!HateSunlight())
            DrawFrame(d, clothFrame, color, 1, 0, ax, ay);
        return false;
    }

    bool HateSunlight()
    {
        return (Main.dayTime && (player.ZoneOverworldHeight || player.ZoneSkyHeight) && !player.behindBackWall) || Main.raining;
    }

    void Blink()
    {
        if (blinkFrame < 22)
            blinkFrame = 22;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 24)
        {
            blinkFrame = 22;
            PetState = 0;
        }
    }

    void DrinkingTea()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 1)
            Projectile.frame = 1;
        if (++Projectile.frameCounter > 9)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            if (Projectile.frame >= 5)
            {
                Projectile.frame = 5;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(120, 360))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        if (ai[0] == 1)
        {
            if (Projectile.frame >= 6)
            {
                Projectile.frame = 6;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(120, 360))
            {
                ai[1] = 0;
                ai[2]++;
                if (ai[2] > Main.rand.Next(3, 9))
                {
                    ai[2] = 0;
                    ai[0] = 2;
                }
                else
                {
                    ai[0] = 0;
                }
            }
        }
        else if (Projectile.frame > 10)
        {
            Projectile.frame = 0;
            ai[0] = 1800;
            PetState = 0;
        }
    }

    void UpdateWingFrame()
    {
        if (wingFrame < 16)
            wingFrame = 16;
        if (++wingFrameCounter > 6)
        {
            wingFrameCounter = 0;
            wingFrame++;
        }
        if (wingFrame > 21)
            wingFrame = 16;
    }

    void UpdateClothFrame()
    {
        if (clothFrame < 12)
            clothFrame = 12;
        if (++clothFrameCounter > 4)
        {
            clothFrameCounter = 0;
            clothFrame++;
        }
        if (clothFrame > 15)
            clothFrame = 12;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [new(3, "3"), new(4, "4")];
        if (PetState == 2)
            lines.AddRange([new(8, "8", 5), new(9, "9", 5)]);
        if (Main.bloodMoon)
            lines.Add(new(10, "10", 5));
        // Flandre answers this one.
        if (talkInterval <= 0 && FindPetState<Flandre>(0) != null)
            lines.Add(new(11, "11"));
        return lines;
    }

    void UpdateTalking()
    {
        if (FindChatIndex<Flandre>(3, 0, 0) != null || FindChatIndex<Patchouli>(16, 0, 0) != null)
            ChatCD = 1;

        BasicTouhouPet? p;
        if ((p = FindChatIndex<Flandre>(5, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToFlandre1"), myColor, 12, 600, -1);
        }
        else if ((p = FindChatIndex<Flandre>(6, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToFlandre2"), myColor, 13, 360, -1);
        }
        else if ((p = FindChatIndex<Flandre>(3, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToFlandre3"), myColor, 14, 360, -1, 10);
            p.ChatIndex = 0;
            talkInterval = 600;
        }
        else if ((p = FindChatIndex<Patchouli>(16, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToPatchouli1"), myColor, 5, 600, -1, 11);
        }
        else if ((p = FindChatIndex<Patchouli>(17, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToPatchouli2"), myColor, 6, 600, -1, 6);
        }
        else if ((p = FindChatIndex<Patchouli>(18, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToPatchouli3"), myColor, 7, 360, -1, 10);
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(9))
        {
            SetChat(myColor);
        }
    }

    protected override void VisualEffectForPreview()
    {
        UpdateWingFrame();
        UpdateClothFrame();
    }

    public override void AI()
    {
        ModUtils.SetPetActive<RemiliaBuff>(Projectile, player);
        ModUtils.SetPetActive<ScarletBuff>(Projectile, player);
        if (!Main.dayTime)
            UpdateTalking();

        var withSister = Owns<Flandre>();
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * (PetState != 2 ? 0.03f : 0.005f);

        ChangeDir(player, !withSister);
        MoveToPoint((withSister ? 50 : -50) * player.direction, -50 + player.gfxOffY, 19);
        if (HateSunlight())
        {
            extraAI[0] = 0;
            extraAI[1] = 0;
            extraAI[2] = 0;
            Projectile.rotation = 0;
            PetState = 0;
            Projectile.frame = 11;
            chatFuncIsOccupied = true;
            return;
        }
        chatFuncIsOccupied = false;

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState != 1)
        {
            if (t % 900 == 0 && Main.rand.NextBool(6) && extraAI[0] <= 0 && player.velocity.Length() < 4)
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
            DrinkingTea();
        }

        extraAdjX = 0;
        extraAdjY = 0;
        int f = Projectile.frame;
        if (f >= 1 && f <= 10)
        {
            extraAdjY = -2;
            if (f != 10)
                extraAdjX = -2 * Projectile.spriteDirection;
        }
    }
}
