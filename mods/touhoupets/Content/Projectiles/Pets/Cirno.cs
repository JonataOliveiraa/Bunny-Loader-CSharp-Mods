// Cirno, the example pet of the original: blinks, laughs, melts in the heat
// and talks with Daiyousei.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Cirno : BasicTouhouPet
{
    protected override Color InitialColor => new Color(76, 207, 239);

    int wingFrame;
    int wingFrameCounter;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 11;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        DrawFrame(d, wingFrame, Projectile.GetAlpha(Color.White));
        DrawFrame(d, Projectile.frame, Projectile.GetAlpha(lightColor));
        return false;
    }

    void Blink()
    {
        if (++Projectile.frameCounter > 3)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 2)
        {
            Projectile.frame = 0;
            PetState = 0;
        }
    }

    void Laugh()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 3)
            Projectile.frame = 3;
        if (++Projectile.frameCounter > (Projectile.frame >= 4 && Projectile.frame <= 5 ? 12 : 7))
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            if (Projectile.frame > 5)
            {
                Projectile.frame = 4;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(10, 20))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else if (Projectile.frame > 6)
        {
            Projectile.frame = 0;
            ai[0] = 600;
            PetState = 0;
        }
    }

    void UpdateWingFrame()
    {
        var hell = player.ZoneUnderworldHeight;
        if (!hell)
        {
            if (wingFrame < 7 || wingFrame >= 10)
                wingFrame = 7;
        }
        else if (wingFrame < 7)
        {
            wingFrame = 7;
        }
        if (++wingFrameCounter > (hell ? 8 : 4))
        {
            wingFrameCounter = 0;
            wingFrame++;
        }
        if (hell && wingFrame > 10)
            wingFrame = 7;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [];
        if (!player.ZoneUnderworldHeight)
        {
            lines.AddRange([new(1, "1"), new(2, "2"), new(3, "3")]);
            // Daiyousei answers this one.
            if (ModUtils.HasBuff<DaiyouseiBuff>(player))
                lines.Add(new(4, "4", 5));
            lines.Add(new(6, "6"));
        }
        if ((player.ZoneDesert && Main.dayTime) || player.ZoneUnderworldHeight || player.ZoneJungle)
        {
            if (player.ZoneJungle && player.ZoneOverworldHeight)
                lines.Add(new(8, "8", 3));
            // Daiyousei answers this one too.
            else
                lines.Add(new(7, "7", 3));
        }
        return lines;
    }

    void UpdateTalking()
    {
        // From the first frame an answer is possible, hold the cooldown, so no
        // other line gets in the way.
        if (FindChatIndex<Daiyousei>(3, 0, 0) != null || FindChatIndex<Daiyousei>(5, 0, 0) != null)
            ChatCD = 1;

        BasicTouhouPet? p;
        if ((p = FindChatIndex<Daiyousei>(3)) != null)
        {
            // The cooldown goes to both, so neither drifts off.
            SetChatWithOtherOne(p, Text("ToDaiyousei1"), myColor, 0, 360);
            p.ChatIndex = 0;
        }
        else if ((p = FindChatIndex<Daiyousei>(5)) != null)
        {
            SetChatWithOtherOne(p, Text("ToDaiyousei2"), myColor, 9, 600);
        }
        else if ((p = FindChatIndex<Daiyousei>(6, 0, 1, true)) != null)
        {
            // Ignores the other's cooldown: the third line on of a conversation.
            SetChatWithOtherOne(p, Text("ToDaiyousei3"), myColor, 0, 360);
            p.ChatIndex = 0;
        }
        else if (mainTimer % 480 == 0 && Main.rand.NextBool(6) && mainTimer > 0 && PetState != 2)
        {
            SetChat(myColor);
        }
    }

    protected override void VisualEffectForPreview()
    {
        UpdateWingFrame();
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, 0.66f, 2.02f, 2.55f);
        ModUtils.SetPetActive<CirnoBuff>(Projectile, player);

        UpdateTalking();
        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.032f;

        ChangeDir(player);
        MoveToPoint(40 * player.direction, -30 + player.gfxOffY, 9);

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState != 1)
        {
            if (t % 300 == 0 && Main.rand.NextBool(2) && extraAI[0] <= 0)
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
            Blink();
        }
        else if (PetState == 2)
        {
            Laugh();
        }

        // In the desert by day, in hell or in the jungle Cirno does not laugh;
        // in hell her eyes are half closed and she sweats.
        if ((player.ZoneDesert && Main.dayTime) || player.ZoneUnderworldHeight || player.ZoneJungle)
        {
            if (PetState == 2)
                PetState = 0;
            if (player.ZoneUnderworldHeight)
            {
                if (Projectile.frame < 1)
                    Projectile.frame = 1;
                if (Main.rand.NextBool(12))
                {
                    Vector2 pos = Projectile.position;
                    for (int i = 0; i < Main.rand.Next(1, 4); i++)
                    {
                        var dust = Dust.NewDustPerfect(new Vector2(pos.X + Main.rand.Next(10, Projectile.width - 10), pos.Y + Main.rand.Next(10, Projectile.height - 10)),
                            MyDustId.Water, null, 100, Color.White, 1);
                        dust.scale = Main.rand.NextFloat(0.5f, 1.2f);
                    }
                }
            }
        }
    }
}
