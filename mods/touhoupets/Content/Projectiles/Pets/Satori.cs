// Satori Komeiji: her third eye circles around her and glows when she reads
// your mind; while it is wide open, you see the creatures around you.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Satori : BasicTouhouPet
{
    protected override Color InitialColor => new Color(255, 149, 170);

    int blinkFrame;
    int blinkFrameCounter;
    int clothFrame;
    int clothFrameCounter;
    float eyeSparkScale;
    float eyeX;
    float eyeY;

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 7;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = true;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        var t2 = Main.GlobalTimeWrappedHourly * 2.3f;
        var ex = 1.2f * MathF.Cos(t2) * -26;
        var ey = 0.35f * MathF.Sin(t2) * -26;
        Vector2 c = Projectile.Center;
        // The eye in world coordinates: the light follows it (AI).
        eyeX = c.X + ex;
        eyeY = c.Y + ey + 8;

        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        if (ey <= 0)
            DrawEye(d);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        DrawFrame(d, clothFrame, color, 2, 1);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color, 2, 0);
        if (ey > 0)
            DrawEye(d);
        return false;
    }

    void DrawEye(in PetDrawInfo d)
    {
        var screen = Main.screenPosition;
        var x = eyeX - screen.X;
        var y = eyeY - screen.Y;
        var s = 1 + 0.9f + Random.Shared.NextSingle() * 0.2f;
        var spark = eyeSparkScale;
        Texture2D glow = ModUtils.Texture("Assets/Textures/Extra/SatoriEyeSpark");
        Texture2D aura = ModUtils.Texture("Assets/Textures/Extra/SatoriEyeAura");
        Color pink = Color.Multiply(Projectile.GetAlpha(Color.DeepPink), 0.5f);
        Vector2 at = new Vector2(x, y + 2);

        ModUtils.ToggleAdditive(true);
        Vector2 auraOrigin = new Vector2(aura.Width / 2f, aura.Height / 2f);
        for (int i = 0; i < 2; i++)
            ModUtils.Draw(aura, at, null, pink, Projectile.rotation, auraOrigin, Projectile.scale * 0.38f * spark, 0);
        ModUtils.ToggleAdditive(false);

        // The eye itself, never flipped.
        DrawFrame(d, 4, Projectile.GetAlpha(Color.White), 2, 1, x - d.x, y - d.y, 0);

        ModUtils.ToggleAdditive(true);
        Vector2 glowOrigin = new Vector2(glow.Width / 2f, glow.Height / 2f);
        Vector2 tall = new Vector2(Projectile.scale * 0.14f * s * spark, Projectile.scale * 0.4f * s * spark);
        Vector2 low = new Vector2(Projectile.scale * 0.14f * s * spark, Projectile.scale * 0.26f * s * spark);
        for (int i = 0; i < 8; i++)
        {
            ModUtils.DrawScaled(glow, at, null, pink, Projectile.rotation + MathF.PI / 2f, glowOrigin, tall, 0);
            ModUtils.DrawScaled(glow, at, null, pink, Projectile.rotation, glowOrigin, low, 0);
        }
        ModUtils.ToggleAdditive(false);
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

    void MindReading()
    {
        int[] ai = extraAI;
        if (++Projectile.frameCounter > 8)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 2 && ai[1] > 0)
            Projectile.frame = 2;
        if (Projectile.frame >= 2)
        {
            ai[1]--;
            if (eyeSparkScale < 1)
                eyeSparkScale += 0.01f;
        }
        if (Projectile.frame > 3)
        {
            ai[0] = 1200;
            ai[1] = 0;
            PetState = 0;
            Projectile.frame = 0;
        }
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

    protected override List<ChatLine> ChatLines(Player player)
    {
        return [new(1, "1"), new(2, "2"), new(3, "3")];
    }

    void UpdateTalking()
    {
        if (FindChatIndex<Koishi>(5, 0, 0) != null || FindChatIndex<Utsuho>(5, 0, 0) != null)
            ChatCD = 1;

        BasicTouhouPet? p;
        if ((p = FindChatIndex<Koishi>(5, 0, 1, true)) != null)
        {
            SetChatWithOtherOne(p, Text("ToKoishi"), myColor, 4, 600);
            p.ChatIndex = 0;
            talkInterval = 1200;
        }
        else if ((p = FindChatIndex<Utsuho>(5, 0, 1, true)) != null)
        {
            // The original passed no one as the other pet here (a slip in it):
            // Utsuho keeps her cooldown.
            SetChatWithOtherOne(null, Text("ToUtsuho"), myColor, 5, 600);
            p.ChatIndex = 0;
            talkInterval = 2400;
        }
        else if (mainTimer % 960 == 0 && Main.rand.NextBool(8))
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
        var light = 1 + eyeSparkScale;
        Lighting.AddLight(new Vector2(eyeX, eyeY), 1.72f * light, 0.69f * light, 0.89f * light);
        ModUtils.SetPetActive<SatoriBuff>(Projectile, player);
        ModUtils.SetPetActive<KomeijiBuff>(Projectile, player);
        UpdateTalking();

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.01f;
        ChangeDir(player);
        MoveToPoint(54 * player.direction, -34 + player.gfxOffY, 11);

        if (mainTimer % 270 == 0 && PetState != 2)
            PetState = 1;
        if (PetState == 0 && mainTimer >= 600 && mainTimer % 120 == 0 && extraAI[0] <= 0)
        {
            PetState = 2;
            extraAI[1] = Main.rand.Next(360, 480);
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
            MindReading();
            if (eyeSparkScale >= 1)
                player.detectCreature = true;
        }
        if (PetState != 2 && eyeSparkScale > 0)
            eyeSparkScale -= 0.02f;
        if (eyeSparkScale < 0)
            eyeSparkScale = 0;
    }
}
