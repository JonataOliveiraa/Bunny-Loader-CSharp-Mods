// Iku Nagae: reads the air. Her lines are about the weather and the events
// of the world; sometimes she spins and sparks fly.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Iku : BasicTouhouPet
{
    protected override Color InitialColor => new Color(79, 215, 239);

    int blinkFrame;
    int blinkFrameCounter;
    int clothFrame;
    int clothFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 15;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        DrawFrame(d, clothFrame, color, 2, 1, 0, 3 * Main.essScale);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color, 2, 0);
        return false;
    }

    void Blink()
    {
        if (blinkFrame < 12)
            blinkFrame = 12;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 14)
        {
            blinkFrame = 12;
            PetState = 0;
        }
    }

    void Spinning()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 4)
            Projectile.frame = 4;
        if (ai[0] == 0)
        {
            if (++Projectile.frameCounter > 2)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame > 7)
            {
                Projectile.frame = 6;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(7, 14))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else if (ai[0] == 1)
        {
            if (++Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame > 10)
            {
                Projectile.frame = 9;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(30, 50))
            {
                ai[1] = 0;
                ai[0] = 2;
            }
            if (Projectile.frame >= 9)
            {
                Vector2 c = Projectile.Center;
                var dust = Dust.NewDustDirect(new Vector2(c.X - 16 * Projectile.spriteDirection, c.Y - 10), 1, 1,
                    Main.rand.NextBool(2) ? MyDustId.TrailingCyan : MyDustId.ElectricCyan,
                    Main.rand.Next(-4, 4), Main.rand.Next(-6, -1), 100, Color.White, Main.rand.NextFloat(0.3f, 1.2f));
                dust.noGravity = false;
            }
        }
        else
        {
            if (++Projectile.frameCounter > 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame > 11)
            {
                Projectile.frame = 0;
                ai[0] = 1200;
                PetState = 0;
            }
        }
    }

    void Idle()
    {
        if (++Projectile.frameCounter > 4)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 3)
            Projectile.frame = 0;
    }

    // Her cloth has a single frame (the original counts it anyway).
    void UpdateMiscFrame()
    {
        if (++clothFrameCounter > 6)
            clothFrameCounter = 0;
        clothFrame = 0;
    }

    // One line about the world, all with index 0 (no one answers them).
    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [];
        var wind = Math.Abs(Main.windSpeedTarget);
        var cloud = Main.cloudAlpha;
        if (Sandstorm.Happening && player.ZoneDesert && player.ZoneOverworldHeight)
        {
            lines.Add(new(0, "Sandstorm"));
        }
        else if (Main.slimeRain)
        {
            lines.Add(new(0, "SlimeRain"));
        }
        else if (Main.bloodMoon)
        {
            lines.Add(new(0, "BloodMoon"));
        }
        else if (Main.eclipse)
        {
            lines.Add(new(0, "Eclipse"));
        }
        else
        {
            if (cloud == 0 && wind <= 0.1f)
                lines.Add(new(0, "Calm"));
            else if (cloud == 0 && wind > 0.1f && wind < 0.25f)
                lines.Add(new(0, "Breeze"));
            else if (cloud == 0 && wind > 0.25f && wind < 0.4f)
                lines.Add(new(0, "Fresh"));
            else if (cloud == 0 && wind >= 0.4f)
                lines.Add(new(0, "Gale"));

            if (cloud >= 0.4f && wind >= 0.3f && cloud < 0.5f && wind < 0.4f)
            {
                lines.Add(new(0, "StormComing"));
            }
            else if (cloud >= 0.5f && wind >= 0.4f && cloud < 0.8f && wind < 0.65f)
            {
                lines.Add(new(0, "Storm"));
            }
            else if (cloud >= 0.8f && wind >= 0.65f)
            {
                lines.Add(new(0, "DragonGod"));
            }
            else if (!ModUtils.AnyBosses(player))
            {
                lines.Add(new(0, "Peaceful"));
                if (ModUtils.PartyIsUp)
                    lines.Add(new(0, "Party"));
                if (LanternNight.LanternsUp)
                    lines.Add(new(0, "Lanterns"));
            }
        }
        return lines;
    }

    void UpdateTalking()
    {
        if (FindChatIndex<Tenshi>(2, 0, 0) != null)
            ChatCD = 1;

        BasicTouhouPet? p;
        if ((p = FindChatIndex<Tenshi>(2)) != null)
        {
            SetChatWithOtherOne(p, Text("ToTenshi"), myColor, 0, 360);
            p.ChatIndex = 0;
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(9) && PetState != 2)
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
        Lighting.AddLight(Projectile.Center, 0.79f, 2.15f, 2.39f);
        ModUtils.SetPetActive<IkuBuff>(Projectile, player);
        UpdateMiscFrame();
        UpdateTalking();

        var withTenshi = Owns<Tenshi>();
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.012f;
        ChangeDir(player, withTenshi);
        MoveToPoint((withTenshi ? -45 : 45) * player.direction, -45 + player.gfxOffY, 15);

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
            if (extraAI[0] >= 1)
                extraAI[0]--;
        }
        else if (PetState == 1)
        {
            Blink();
        }
        else if (PetState == 2)
        {
            Spinning();
        }
    }
}
