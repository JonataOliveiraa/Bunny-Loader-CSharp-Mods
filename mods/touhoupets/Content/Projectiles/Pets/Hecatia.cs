// Hecatia Lapislazuli: three bodies and three planets (Otherworld, Earth and
// Moon). Every 80 seconds she switches to the next world.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Hecatia : BasicTouhouPet
{
    protected override Color InitialColor => WORLD_COLORS[0];

    int blinkFrame;
    int blinkFrameCounter;
    int worldState;
    readonly float[] bodyAlpha = new float[3];
    // The planets around her: 0 Otherworld, 1 Earth, 2 Moon.
    readonly Vector2[] planets = new Vector2[3];

    static readonly Color[] WORLD_COLORS = { new Color(255, 120, 120), new Color(79, 215, 239), new Color(255, 249, 137) };

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 7;
        Main.projPet[Type] = true;
    }

    void DrawPlanets(in PetDrawInfo d, float x, float y, Color color)
    {
        Texture2D t = ModUtils.Texture("Assets/Textures/Extra/HecatiaPlanets");
        var w = t.Width;
        var h = (int)MathF.Floor(t.Height / 3f);
        Vector2 origin = new Vector2(w / 2f, h / 2f);
        var cos = MathF.Cos(d.rotation);
        var sin = MathF.Sin(d.rotation);
        var dir = -Projectile.spriteDirection;
        for (int i = 0; i < 3; i++)
        {
            var px = planets[i].X * dir;
            var py = planets[i].Y;
            // The offset turns with the body (RotatedBy).
            ModUtils.Draw(t, new Vector2(x + px * cos - py * sin, y + px * sin + py * cos), new Rectangle(0, i * h, w, h),
                color, d.rotation, origin, d.scale * (i == 0 ? 1.12f : 1), d.effect);
        }
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        Color color = Projectile.GetAlpha(lightColor);
        DrawPlanets(d, d.x, d.y + 4 * Main.essScale, color);

        var a = bodyAlpha;
        DrawFrame(d, Projectile.frame, color, 3, 0);
        for (int i = 0; i < 3; i++)
            DrawFrame(d, Projectile.frame, Color.Multiply(color, a[i]), 3, i);
        if (PetState == 1)
        {
            DrawFrame(d, blinkFrame, color, 3, 0);
            for (int i = 0; i < 3; i++)
                DrawFrame(d, blinkFrame, Color.Multiply(color, a[i]), 3, i);
        }
        return false;
    }

    // The body of the current world fades in, its planet goes up on top, and
    // the other two move to the sides.
    void UpdateWorldState()
    {
        var speed = 1.5f;
        if (worldState > 2)
            worldState = 0;
        var top = worldState;
        var right = (top + 1) % 3;
        var left = (top + 2) % 3;
        var p = planets;

        if (p[top].X > 0)
            p[top].X--;
        if (p[top].X < 0)
            p[top].X++;
        if (p[top].Y > -27)
            p[top].Y--;
        MoveSidePlanet(right, speed);
        MoveSidePlanet(left, -speed);
        for (int i = 0; i < 3; i++)
            bodyAlpha[i] += i == top ? 0.02f : -0.02f;
        myColor = WORLD_COLORS[top];

        for (int i = 0; i < 3; i++)
        {
            bodyAlpha[i] = Math.Min(Math.Max(bodyAlpha[i], 0), 1);
            p[i].X = Math.Min(Math.Max(p[i].X, -20), 20);
            p[i].Y = Math.Min(Math.Max(p[i].Y, -27), -5);
        }
    }

    void MoveSidePlanet(int i, float dx)
    {
        Vector2[] p = planets;
        p[i].X += dx;
        if (p[i].Y > -5)
            p[i].Y--;
        if (p[i].Y < -5)
            p[i].Y++;
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

    void Idle()
    {
        if (++Projectile.frameCounter > 8)
        {
            Projectile.frameCounter = 0;
            Projectile.frame++;
        }
        if (Projectile.frame > 3)
            Projectile.frame = 0;
    }

    protected override List<ChatLine> ChatLines(Player player)
    {
        // Clownpiece answers line 2.
        return [new(1, "1"), new(2, "2")];
    }

    protected override void VisualEffectForPreview()
    {
        UpdateWorldState();
        Idle();
    }

    public override void AI()
    {
        ModUtils.SetPetActive<HecatiaBuff>(Projectile, player);
        if (mainTimer % 777 == 0 && Main.rand.NextBool(7))
            SetChat(myColor);

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.030f;
        if (PetState != 2)
            ChangeDir(player, true);
        MoveToPoint(-55 * player.direction, -40 + player.gfxOffY, 14.5f);

        if (mainTimer % 270 == 0 && PetState != 2)
            PetState = 1;
        if (mainTimer == 4798)
            worldState++;
        if (PetState == 1)
            Blink();
    }
}
