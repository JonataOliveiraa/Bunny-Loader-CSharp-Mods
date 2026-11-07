// Marisa Kirisame: rides her broom, hums over her Mini-Hakkero, and has
// something to show Reimu.
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Marisa : BasicTouhouPet
{
    protected override Color InitialColor => new Color(255, 249, 137);

    int blinkFrame;
    int blinkFrameCounter;
    int broomFrame;
    int broomFrameCounter;
    int lightFrame;
    int lightFrameCounter;

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
        DrawFrame(d, broomFrame, color, 2, 1);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color, 2, 0);
        DrawFrame(d, lightFrame, Projectile.GetAlpha(Color.White), 2, 1);
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

    // Happy: little notes float up.
    void Happy()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 4)
            Projectile.frame = 4;
        if (++Projectile.frameCounter > 6)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (ai[0] == 0)
        {
            if (Projectile.frame > 11)
            {
                Vector2 c = Projectile.Center;
                Gore.NewGoreDirect(new Vector2(c.X, c.Y - 27), new Vector2(Main.rand.Next(-5, 5) * 0.1f, Main.rand.Next(-6, -3) * 0.1f),
                    Main.rand.Next(570, 573), Main.rand.NextFloat(0.9f, 1.1f));
                Projectile.frame = 4;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(10, 20))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else if (Projectile.frame > 11)
        {
            Projectile.frame = 0;
            ai[0] = 3600;
            PetState = 0;
        }
    }

    void Idle()
    {
        if (++Projectile.frameCounter > 6)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 3)
            Projectile.frame = 0;
    }

    void UpdateMiscFrame()
    {
        if (broomFrame < 4)
            broomFrame = 4;
        if (++broomFrameCounter > 6)
        {
            broomFrameCounter = 0;
            broomFrame++;
        }
        if (broomFrame > 7)
            broomFrame = 4;

        if (++lightFrameCounter > 10)
        {
            lightFrameCounter = 0;
            lightFrame++;
        }
        if (lightFrame > 3)
            lightFrame = 0;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [new(1, "1"), new(2, "2"), new(3, "3")];
        var forest = ModUtils.ZoneForest(player);
        if (forest && Main.dayTime && Main.cloudAlpha == 0)
            lines.Add(new(4, "4"));
        if (forest && !Main.dayTime && Main.cloudAlpha == 0)
            lines.Add(new(5, "5"));
        // Reimu answers line 6 and line 10.
        if (talkInterval <= 0 && (FindPetState<Reimu>(0, 1) != null || FindPetState<Reimu>(3, 4) != null))
            lines.Add(new(6, "6"));
        if (Main.bloodMoon || Main.eclipse)
            lines.Add(new(10, "10", 10));
        return lines;
    }

    void UpdateTalking()
    {
        BasicTouhouPet? p;
        if ((p = FindChatIndex<Reimu>(4, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToReimu1"), myColor, 7, 600, -1, 9);
        }
        else if ((p = FindChatIndex<Reimu>(5, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToReimu2"), myColor, 8, 600, -1, 9);
        }
        else if ((p = FindChatIndex<Reimu>(6, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToReimu3"), myColor, 9, 360, -1, 9);
        }
        else if ((p = FindChatIndex<Reimu>(7, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToReimu4"), myColor, 0, 360, -1);
            p.ChatIndex = 0;
            talkInterval = 3600;
        }
        else if (mainTimer % 720 == 0 && Main.rand.NextBool(5) && PetState != 2)
        {
            SetChat(myColor);
        }
    }

    protected override void VisualEffectForPreview()
    {
        UpdateMiscFrame();
        if (PetState != 2)
            Idle();
    }

    public override void AI()
    {
        Lighting.AddLight(Projectile.Center, 1.95f, 1.90f, 1.03f);
        ModUtils.SetPetActive<MarisaBuff>(Projectile, player);
        UpdateTalking();

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.012f;
        ChangeDir(player);
        MoveToPoint(50 * player.direction, -30 + player.gfxOffY, 12.5f);

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState != 1 && Projectile.frame == 3)
        {
            if (t % 120 == 0 && Main.rand.NextBool(6) && extraAI[0] <= 0)
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
            Happy();
        }
    }
}
