// Junko: wrapped in a burning aura of tails that flares when she is angry.
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using TouhouPetsMod.Common;
using TouhouPetsMod.Content.Buffs.PetBuffs;

namespace TouhouPetsMod.Content.Projectiles.Pets;

public class Junko : BasicTouhouPet
{
    protected override Color InitialColor => new Color(254, 159, 75);

    int blinkFrame;
    int blinkFrameCounter;
    float auraValue;

    const float DEG = MathF.PI / 180f;

    // The aura, tail by tail: wide, dx (times -spriteDirection), dy, color, angle, side.
    // `wide` is the flatter tail (DrawAuraSingle2); the side angle grows with
    // the breathing of the light (Main.essScale).
    static readonly (bool Wide, int Dx, int Dy, float ColorFactor, int Angle, int Side)[] TAILS = {
        (false, 0, 0, 1f, 0, 0),
        (true, 0, 6, 0.5f, 10, 1), (false, 0, 6, 1f, 25, 1), (true, 0, 6, 0.5f, 55, 1),
        (false, 0, 16, 1f, 60, 1), (true, -4, 6, 0.5f, 110, 1), (false, -4, 6, 1f, 120, 1),
        (true, 0, 6, 0.5f, -10, -1), (false, 0, 6, 1f, -25, -1), (true, 0, 6, 0.5f, -55, -1),
        (false, 0, 16, 1f, -60, -1), (true, 4, 6, 0.5f, -110, -1), (false, 4, 6, 1f, -120, -1),
    };

    readonly Color[] whiteLayers = new Color[4];
    readonly Color[] halfLayers = new Color[4];
    readonly Vector2[] tallScales = new Vector2[4];
    readonly Vector2[] flatScales = new Vector2[4];

    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 11;
        Main.projPet[Type] = true;
        ProjectileID.Sets.LightPet[Type] = true;
    }

    void DrawAura(in PetDrawInfo d)
    {
        var ess = Main.essScale;
        var aura = auraValue;
        var w = (int)MathF.Floor(d.width / 2f);
        var rect = new Rectangle(w, (int)d.height, w, (int)d.height);
        Vector2 origin = new Vector2(w / 2f, d.height / 2f);
        Color white = Projectile.GetAlpha(Color.White);
        Color half = Color.Multiply(white, 0.5f);
        // The four layers of a tail: color factor and scale.
        float tall = 1 + 1.1f * ess * aura;
        float flat = 0.75f + 1.1f * ess * aura;
        SetLayer(0, 0.1f * aura, 1.75f * aura, white, half, d.scale, tall, flat);
        SetLayer(1, 0.25f * aura, 1.5f * aura, white, half, d.scale, tall, flat);
        SetLayer(2, 0.5f, 1.25f, white, half, d.scale, tall, flat);
        SetLayer(3, 0.75f, 0.75f, white, half, d.scale, tall, flat);
        var dir = -Projectile.spriteDirection;
        var bx = d.x - 4 * Projectile.spriteDirection;
        var by = d.y - 6 + 3 * ess;

        ModUtils.ToggleAdditive(true);
        foreach (var (wide, dx, dy, colorFactor, angle, side) in TAILS)
        {
            Color[] colors = colorFactor == 1 ? whiteLayers : halfLayers;
            var rotation = d.rotation + (angle + 20 * side * ess) * DEG;
            var x = bx + dx * dir;
            var y = by + dy;
            for (int i = 0; i < 4; i++)
            {
                for (int l = 0; l < 4; l++)
                {
                    // A little shake each time, as in the original.
                    var jx = (Random.Shared.Next(21) - 10) * 0.25f;
                    var jy = (Random.Shared.Next(21) - 10) * 0.25f;
                    ModUtils.DrawScaled(d.t, new Vector2(x + jx, y + jy), rect, colors[l], rotation,
                        origin, wide ? flatScales[l] : tallScales[l], d.effect);
                }
            }
        }
        ModUtils.ToggleAdditive(false);
    }

    void SetLayer(int l, float factor, float s, Color white, Color half, float baseScale, float tall, float flat)
    {
        whiteLayers[l] = Color.Multiply(white, factor);
        halfLayers[l] = Color.Multiply(half, factor);
        tallScales[l] = new Vector2(baseScale * s, baseScale * s * tall);
        flatScales[l] = new Vector2(baseScale * s * 2, baseScale * s * flat);
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        PetDrawInfo d = DrawInfo();
        DrawAura(d);
        Color color = Projectile.GetAlpha(lightColor);
        DrawFrame(d, Projectile.frame, color, 2, 0);
        if (PetState == 1)
            DrawFrame(d, blinkFrame, color, 2, 0);
        return false;
    }

    void Blink()
    {
        if (blinkFrame < 9)
            blinkFrame = 9;
        if (++blinkFrameCounter > 3)
        {
            blinkFrameCounter = 0;
            blinkFrame++;
        }
        if (blinkFrame > 11)
        {
            blinkFrame = 9;
            PetState = 0;
        }
    }

    void Wrath()
    {
        int[] ai = extraAI;
        if (Projectile.frame < 4)
            Projectile.frame = 4;
        if (ai[0] == 0)
        {
            if (++Projectile.frameCounter > 3)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame > 6)
            {
                Projectile.frame = 5;
                ai[1]++;
            }
            if (ai[1] > Main.rand.Next(24, 48))
            {
                ai[1] = 0;
                ai[0] = 1;
            }
        }
        else
        {
            if (++Projectile.frameCounter > 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
            }
            if (Projectile.frame > 7)
            {
                Projectile.frame = 0;
                ai[0] = 2400;
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

    protected override List<ChatLine> ChatLines(Player player)
    {
        List<ChatLine> lines = [];
        if (!Main.dayTime && Main.cloudAlpha <= 0)
            lines.Add(new(1, "1"));
        // A silent one: most of the time she says nothing.
        lines.Add(new(2, null));
        return lines;
    }

    protected override void VisualEffectForPreview()
    {
        if (PetState != 2)
            Idle();
    }

    public override void AI()
    {
        var aura = auraValue;
        Lighting.AddLight(Projectile.Center, 2.38f + 5 * aura, 1.41f + 5 * aura, 2.55f + 5 * aura);
        ModUtils.SetPetActive<JunkoBuff>(Projectile, player);
        if (mainTimer % 960 == 0 && mainTimer > 0 && PetState != 2 && Main.rand.NextBool(9))
            SetChat(myColor);

        Projectile.tileCollide = false;
        Projectile.rotation = Projectile.velocity.X * 0.018f;
        ChangeDir(player);
        MoveToPoint(45 * player.direction, -65 + player.gfxOffY, 17);

        int t = mainTimer;
        if (t % 270 == 0 && PetState != 2)
            PetState = 1;
        if (t >= 1200 && t < 3600 && PetState != 1)
        {
            if (t % 450 == 0 && Main.rand.NextBool(6) && extraAI[0] <= 0)
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
            Wrath();
        }
        auraValue = Math.Min(Math.Max(auraValue, 0), 1);
        auraValue += PetState == 2 ? 0.08f : -0.01f;
    }
}
