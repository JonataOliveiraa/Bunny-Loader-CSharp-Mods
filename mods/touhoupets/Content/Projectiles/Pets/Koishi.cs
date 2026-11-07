// Koishi Komeiji: her closed third eye circles around her. She fades away now
// and then, pulls faces at you, and when her master is about to die, without
// Satori around... she makes a phone call.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Koishi : BasicTouhouPet
{
    protected override Color InitialColor => new Color(145, 255, 183);

    int blinkFrame;
    int blinkFrameCounter;
    int clothFrame;
    int clothFrameCounter;
    int annoyingFrame;
    int annoyingFrameCounter;

    const int STATE_KILL = 4;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 18;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = false;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        var t2 = Main.GlobalTimeWrappedHourly * 2;
        var ex = 1.2f * MathF.Cos(t2) * 26;
        var ey = 0.35f * MathF.Sin(t2) * 26;
        // The eye goes behind her on the far half of the circle.
        void Eye(in PetDrawInfo info) => DrawFrame(info, 7, color, 2, 1, ex, ey + 8 - 7 * Main.essScale, 0);

        if (ey <= 0)
            Eye(d);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color, 2, 1);
        DrawFrame(d, clothFrame, color, 2, 1);
        if (PetState == 3)
            DrawFrame(d, annoyingFrame, color, 2, 1);
        if (ey > 0)
            Eye(d);
        return false;
    }

    void Blink()
    {
        if (blinkFrame < 4)
            blinkFrame = 4;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 6)
        {
            blinkFrame = 4;
            PetState = 0;
        }
    }

    void Fade()
    {
        int[] ai = extraAI;
        ai[0]++;
        if (ai[0] > ai[1] - 255 / 2f)
            Projectile.Opacity += 0.01f;
        else
            Projectile.Opacity -= 0.005f;
        if (ai[0] > ai[1])
        {
            ai[0] = 3600;
            ai[1] = 0;
            PetState = 0;
        }
    }

    void Annoying()
    {
        int[] ai = extraAI;
        if (++annoyingFrameCounter > 4)
        {
            annoyingFrameCounter = 0;
            annoyingFrame++;
        }
        if (annoyingFrame < 8)
            annoyingFrame = 8;
        if (annoyingFrame > 9)
            annoyingFrame = 8;
        if (++Projectile.frameCounter > 5)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame < 14)
            Projectile.frame = 14;
        if (Projectile.frame > 16 && ai[1] <= ai[2])
        {
            ai[1]++;
            Projectile.frame = 15;
        }
        if (Projectile.frame > 17)
        {
            ai[0] = 1800;
            ai[1] = 0;
            ai[2] = 0;
            PetState = 0;
            Projectile.frame = 0;
            return;
        }
        chatFuncIsOccupied = true;
    }

    void UpdateClothFrame()
    {
        if (++clothFrameCounter > 4)
        {
            clothFrameCounter = 0;
            clothFrame++;
        }
        if (clothFrame > 3)
            clothFrame = 0;
    }

    void KillingPhoneCall()
    {
        int[] ai = extraAI;
        if (++Projectile.frameCounter > 12)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            if (Projectile.frame < 5)
            {
                chatFuncIsOccupied = true;
            }
            else
            {
                chatFuncIsOccupied = false;
                if (ai[1] == 0 && ChatTimeLeft <= 0)
                {
                    SetChatWithOtherOne(null, Text("Call1"), myColor, -1, 60, -1, 60);
                    ai[1] = 1;
                }
                if (ai[1] == 1 && ChatTimeLeft <= 0)
                {
                    SetChatWithOtherOne(null, Text("Call2"), myColor, -2, 60, -1);
                    ai[1] = 2;
                }
            }
            if (Projectile.frame > 6)
                Projectile.frame = 5;
            if (ai[1] >= 2 && ChatCD <= 0)
            {
                Projectile.frame = 6;
                Projectile.Opacity -= 0.01f;
                if (Projectile.Opacity <= 0)
                {
                    ai[0] = 1;
                    ai[1] = 0;
                }
            }
        }
        else if (ai[0] == 1)
        {
            Projectile.Opacity += 0.01f;
            if (Projectile.Opacity < 1)
                Projectile.frame = 7;
            if (ai[1] == 0 && Projectile.frame == 8)
            {
                textShaking = true;
                SetChatWithOtherOne(null, Text("Call3"), Color.Red, -3, 4, -1, 45);
                ai[1]++;
            }
            if (Projectile.frame > 10)
                Projectile.frame = 9;
            if (ai[1] > 0 && ChatCD <= 0)
            {
                ai[0]++;
                ai[1] = 0;
            }
        }
        else if (ai[0] == 2)
        {
            Projectile.frameCounter += 2;
            if (Projectile.frame >= 13)
            {
                Projectile.frame = 13;
                // Only on the owner's own game: anyone else would see a death
                // that did not happen.
                if (player.whoAmI == Main.myPlayer)
                {
                    var reason = (Language.GetTextValue("Mods.touhoupets.DeathReason.KilledByKoishi") ?? "").Replace("{0}", player.name);
                    player.KillMe(PlayerDeathReason.ByCustomReason(reason), 0, 0, false);
                }
                if (!player.dead)
                {
                    ai[0] = 0;
                    PetState = 0;
                }
            }
        }
        if (player.active && ai[0] > 0 && !player.dead)
        {
            var c = player.Center;
            Projectile.Center = new Vector2(c.X - 30 * player.direction, c.Y + player.gfxOffY);
        }
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        // Satori answers line 5.
        return [new(1, "1"), new(2, "2"), new(3, "3"), new(4, "4"), new(5, "5")];
    }

    void UpdateTalking()
    {
        if (player.statLife < player.statLifeMax2 / 10f && !Owns<Satori>())
        {
            if (mainTimer % 120 == 0 && Main.rand.NextBool(7) && PetState == 0 && ChatCD <= 0)
            {
                PetState = STATE_KILL;
                extraAI[0] = 0;
            }
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(7) && PetState <= 1)
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
        var kept = ModUtils.HasBuff<KoishiBuff>(player) || ModUtils.HasBuff<KomeijiBuff>(player);
        if ((!kept && PetState != STATE_KILL) || player.dead)
        {
            Projectile.velocity = new Vector2(0, 0);
            Projectile.frame = Projectile.frame == 13 ? 13 : 0;
            Projectile.Opacity -= 0.009f;
            if (Projectile.Opacity <= 0)
            {
                Projectile.active = false;
                return;
            }
        }
        else if (PetState <= 1)
        {
            Projectile.Opacity += 0.009f;
        }
        Projectile.timeLeft = 2;

        UpdateTalking();
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.01f;
        ChangeDir(player, true);
        if (!player.dead)
            MoveToPoint(-54 * player.direction, -34 + player.gfxOffY, 13);

        int t = mainTimer;
        if (t % 270 == 0 && PetState < 2)
            PetState = 1;
        if (t >= 600 && t < 4200 && PetState == 0)
        {
            if (t % 360 == 0 && Main.rand.NextBool(4) && extraAI[0] <= 0)
            {
                if (Main.rand.NextBool(4) && ChatTimeLeft <= 0)
                {
                    PetState = 3;
                    extraAI[2] = Main.rand.Next(5, 14);
                }
                else if (!Owns<Satori>())
                {
                    PetState = 2;
                    extraAI[1] = Main.rand.Next(1800, 3600);
                }
            }
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
            Projectile.frame = 0;
            Fade();
        }
        else if (state == 3)
        {
            Annoying();
        }
        else if (state == STATE_KILL)
        {
            KillingPhoneCall();
        }
        if (PetState != STATE_KILL)
        {
            if (chatFuncIsOccupied && PetState != 3)
                chatFuncIsOccupied = false;
            textShaking = false;
        }
    }
}
