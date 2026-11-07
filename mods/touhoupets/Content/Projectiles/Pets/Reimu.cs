// Reimu Hakurei: naps when nothing is happening, flies when you go fast,
// complains about donations and puts up with Marisa and Sanae.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Reimu : BasicTouhouPet
{
    protected override Color InitialColor => new Color(255, 120, 120);

    int blinkFrame;
    int blinkFrameCounter;
    int clothFrame;
    int clothFrameCounter;
    int flyTimeleft;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 18;
        Main.projPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        DrawFrame(d, Projectile.frame, color);
        if (PetState < 3)
            DrawFrame(d, clothFrame, color);
        if (PetState == 1 || PetState == 4)
            DrawFrame(d, blinkFrame, color);
        return false;
    }

    void Blink()
    {
        if (blinkFrame < 13)
            blinkFrame = 13;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 15)
        {
            blinkFrame = 13;
            PetState = PetState == 4 ? 3 : 0;
        }
    }

    void UpdateClothFrame()
    {
        if (clothFrame < 9)
            clothFrame = 9;
        if (++clothFrameCounter > 5)
        {
            clothFrameCounter = 0;
            clothFrame++;
        }
        if (clothFrame > 12)
            clothFrame = 9;
    }

    void Nap()
    {
        int[] ai = extraAI;
        chatFuncIsOccupied = true;
        if (Projectile.frame < 1)
            Projectile.frame = 1;
        if (++Projectile.frameCounter > 6)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            Projectile.velocity = Vector2.Multiply(Projectile.velocity, 0.5f);
            Projectile.frame = 1;
            ai[1]++;
            if (ai[1] > Main.rand.Next(60, 180))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else if (ai[0] == 1)
        {
            Projectile.velocity = Vector2.Multiply(Projectile.velocity, 0.1f);
            if (Projectile.frame >= 3)
                Projectile.frame = 3;
            if (mainTimer % 240 == 0 && Main.rand.NextBool(3) && !player.sleeping.FullyFallenAsleep)
            {
                if (Main.rand.NextBool(2))
                {
                    ai[0] = 2;
                }
                else
                {
                    Projectile.frame = 1;
                    ai[0] = 0;
                }
            }
        }
        else if (ai[0] == 2)
        {
            Projectile.velocity = Vector2.Multiply(Projectile.velocity, 0.5f);
            if (Projectile.frame == 5)
                Projectile.frame = 6;
            if (Projectile.frame > 6)
            {
                Projectile.frame = 4;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(2, 4))
            {
                ai[1] = 0;
                ai[0] = 3;
            }
        }
        else if (ai[0] == 3)
        {
            Projectile.frame = 5;
            Projectile.velocity = Vector2.Multiply(Projectile.velocity, 0.75f);
            if (ai[1] > Main.rand.Next(40, 60))
            {
                ai[1] = 0;
                ai[0] = 4;
            }
            else
            {
                ai[1]++;
            }
        }
        else if (ai[0] == 4)
        {
            Projectile.frameCounter += 2;
            if (Projectile.frame > 8)
            {
                Projectile.frame = 7;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(6, 9))
            {
                ai[1] = 0;
                ai[0] = 5;
            }
        }
        else
        {
            Projectile.frame = 0;
            ai[0] = 600;
            PetState = 0;
        }
    }

    void Flying()
    {
        if (Projectile.frame < 16)
            Projectile.frame = 16;
        if (++Projectile.frameCounter > 3)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 17)
            Projectile.frame = 16;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        if (ModUtils.AnyBosses(player))
            return [new(8, "8")];
        if (Main.bloodMoon || Main.eclipse || Main.slimeRain)
            return [new(3, "3", 10)];
        return [new(1, "1"), new(2, "2")];
    }

    void UpdateTalking()
    {
        if (FindChatIndex<Marisa>(6, 0, 0) != null || FindChatIndex<Marisa>(10, 0, 0) != null || FindChatIndex<Sanae>(4, 0, 0) != null)
        {
            ChatCD = 1;
        }

        BasicTouhouPet? p;
        if ((p = FindChatIndex<Marisa>(10)) != null)
        {
            SetChatWithOtherOne(p, Text("ToMarisa0"), myColor, 0, 600);
            p.ChatIndex = 0;
            talkInterval = 1200;
        }
        else if ((p = FindChatIndex<Marisa>(6, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToMarisa1"), myColor, 4, 600);
            extraAI[0] = 600;
        }
        else if ((p = FindChatIndex<Marisa>(7, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToMarisa2"), myColor, 5, 600, -1, 9);
            extraAI[0] = 600;
        }
        else if ((p = FindChatIndex<Marisa>(8, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToMarisa3"), myColor, 6, 360, -1, 9);
            extraAI[0] = 600;
        }
        else if ((p = FindChatIndex<Marisa>(9, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToMarisa4"), myColor, 7, 360, -1, 9);
        }
        else if ((p = FindChatIndex<Sanae>(4, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToSanae"), myColor, 8, 360);
            p.ChatIndex = 0;
            talkInterval = 600;
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(7))
        {
            SetChat(myColor);
        }
    }

    protected override void VisualEffectForPreview()
    {
        UpdateClothFrame();
    }

    public override void AI()
    {
        if (flyTimeleft > 0)
            flyTimeleft--;
        ModUtils.SetPetActive<ReimuBuff>(Projectile, player);
        if (PetState != 2)
            UpdateTalking();

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.012f;
        if (PetState != 2)
            ChangeDir(player, PetState < 3);
        MoveToPoint(-50 * player.direction, -30 + player.gfxOffY, 22);

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2 && PetState < 3)
            PetState = 1;
        else if (t % 270 == 0 && PetState == 3)
            PetState = 4;

        var speed = player.velocity.Length();
        var bosses = ModUtils.AnyBosses(player);
        if (t >= 1200 && t < 3600 && PetState != 1 && PetState < 3 && !bosses)
        {
            var chance = 6;
            if (Main.bloodMoon || Main.eclipse)
                chance = 11;
            else if (Main.dayTime || Main.raining)
                chance = 3;
            else if (player.sleeping.FullyFallenAsleep)
                chance = 1;
            if (t % 120 == 0 && Main.rand.NextBool(chance) && extraAI[0] <= 0 && speed == 0 && ChatCD <= 0)
            {
                SetChat(myColor, Text("Sleepy"), 3);
                PetState = 2;
            }
        }
        if (speed > 4 && PetState == 2)
            PetState = 0;
        if (speed > 15)
        {
            flyTimeleft = 5;
            if (PetState < 3)
                PetState = 3;
        }
        else if (flyTimeleft <= 0 && PetState >= 3)
        {
            extraAI[0] = 960;
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
            Nap();
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
        if (PetState == 2 && bosses)
            PetState = 0;
        if (PetState != 2 && chatFuncIsOccupied)
            chatFuncIsOccupied = false;
    }
}
