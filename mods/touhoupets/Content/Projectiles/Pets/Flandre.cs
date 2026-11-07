// Flandre Scarlet: glowing crystal wings, a pudding to eat, and a sister she
// wishes would play with her. Hides under her hat in the sunlight.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Flandre : BasicTouhouPet
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
        Main.projFrames[Type] = 23;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = false;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        Color white = Projectile.GetAlpha(Color.White);
        var ax = extraAdjX;
        var ay = extraAdjY;
        DrawFrame(d, wingFrame, color, 1, 0, ax, ay);

        // The crystals: a soft glow turning around them, then the crystals.
        Texture2D glow = ModUtils.Texture("Assets/Textures/Glow/FlandreGlow");
        var rect = new Rectangle(0, (int)(wingFrame * d.height), d.width, (int)d.height);
        Vector2 origin = new Vector2(d.width / 2f, d.height / 2f);
        var spin = MathF.PI * 2 * Main.GlobalTimeWrappedHourly;
        Color soft = Color.Multiply(white, 0.4f);
        ModUtils.ToggleAdditive(true);
        for (int i = 0; i < 8; i++)
        {
            // (0, -2) turned by the angle.
            var a = spin + MathF.PI * 2 / 8f * i * 0.6f;
            ModUtils.Draw(glow, new Vector2(d.x + ax + 2 * MathF.Sin(a), d.y + ay - 2 * MathF.Cos(a)), rect, soft,
                d.rotation, origin, d.scale * 0.95f, d.effect);
        }
        ModUtils.ToggleAdditive(false);
        ModUtils.Draw(glow, new Vector2(d.x + ax, d.y + ay), rect, Color.Multiply(white, 0.6f), d.rotation, origin, d.scale, d.effect);

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
        if (blinkFrame < 20)
            blinkFrame = 20;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 22)
        {
            blinkFrame = 20;
            PetState = 0;
        }
    }

    void Eating()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 1)
            Projectile.frame = 1;
        if (++Projectile.frameCounter > 10)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            if (Projectile.frame >= 3)
            {
                Projectile.frame = 3;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(90, 120))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else if (ai[0] == 1)
        {
            if (Projectile.frame >= 7)
            {
                Projectile.frame = 7;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(120, 360))
            {
                ai[1] = 0;
                ai[0] = 2;
            }
        }
        else
        {
            Projectile.frameCounter += 1;
            if (Projectile.frame > 9)
            {
                Projectile.frame = 0;
                ai[0] = 1200;
                PetState = 0;
            }
        }
    }

    void UpdateWingFrame()
    {
        if (wingFrame < 15)
            wingFrame = 15;
        if (++wingFrameCounter > 6)
        {
            wingFrameCounter = 0;
            wingFrame++;
        }
        if (wingFrame > 19)
            wingFrame = 15;
    }

    void UpdateClothFrame()
    {
        if (clothFrame < 11)
            clothFrame = 11;
        if (++clothFrameCounter > 4)
        {
            clothFrameCounter = 0;
            clothFrame++;
        }
        if (clothFrame > 14)
            clothFrame = 11;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        // Line 3 is answered by Remilia.
        List<ChatLine> lines = [new(1, "1"), new(2, "2"), new(3, "3")];
        if (PetState == 2)
            lines.AddRange([new(4, "4", 5), new(5, "5", 5)]);
        return lines;
    }

    void UpdateTalking()
    {
        if (FindChatIndex<Remilia>(11, 0, 0) != null)
            ChatCD = 1;

        BasicTouhouPet? p;
        if ((p = FindChatIndex<Remilia>(11, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToRemilia1"), myColor, 5, 600, -1);
        }
        else if ((p = FindChatIndex<Remilia>(12, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToRemilia2"), myColor, 6, 600, -1);
        }
        else if ((p = FindChatIndex<Remilia>(13, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToRemilia3"), myColor, 7, 360, -1);
            p.ChatIndex = 0;
            talkInterval = 3600;
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(7) && mainTimer > 0)
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
        Lighting.AddLight(Projectile.Center, Main.DiscoR * 0.013f, Main.DiscoG * 0.016f, Main.DiscoB * 0.019f);
        ModUtils.SetPetActive<FlandreBuff>(Projectile, player);
        ModUtils.SetPetActive<ScarletBuff>(Projectile, player);

        if (!Main.dayTime)
            UpdateTalking();
        var withSister = Owns<Remilia>();
        var px = (withSister ? -50 : 50) * player.direction;
        var py = -40 + player.gfxOffY;
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * (PetState != 2 ? 0.03f : 0.005f);

        ChangeDir(player, withSister);
        if (HateSunlight())
        {
            extraAI[0] = 0;
            extraAI[1] = 0;
            extraAI[2] = 0;
            Projectile.rotation = 0;
            MoveToPoint(px, py, 36);
            PetState = 0;
            Projectile.frame = 10;
            chatFuncIsOccupied = true;
            return;
        }
        MoveToPoint(px, py, 19);
        chatFuncIsOccupied = false;

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState != 1)
        {
            if (t % 600 == 0 && Main.rand.NextBool(4) && extraAI[0] <= 0)
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
            Projectile.frame = 0;
            Blink();
        }
        else if (PetState == 2)
        {
            Eating();
        }

        extraAdjX = 0;
        extraAdjY = 0;
        int f = Projectile.frame;
        if (f >= 1 && f <= 9)
        {
            extraAdjY = -2;
            if (f != 1 && f != 6 && f != 9)
                extraAdjX = -2 * Projectile.spriteDirection;
        }
    }
}
