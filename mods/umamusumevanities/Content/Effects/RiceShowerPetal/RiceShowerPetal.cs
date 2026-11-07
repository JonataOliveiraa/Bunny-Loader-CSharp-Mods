using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using umamusumevanities.Common.Effects;

namespace umamusumevanities.Content.Effects.RiceShowerPetal;

public class RiceShowerPetal : ModProjectile
{
    static readonly Color[] petalTints =
    {
        new(136, 143, 224), new(149, 133, 218), new(174, 143, 226), new(123, 152, 213),
        new(187, 157, 225), new(155, 140, 220), new(201, 161, 216), new(139, 148, 220),
    };

    static readonly Color palePetal = new(253, 246, 250);
    static readonly Color glowPetal = new(217, 184, 255);
    static readonly Vector2 petalOrigin = new(5, 5);

    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 10;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 360;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = false;
    }

    public override bool? CanDamage() => false;

    public override void AI()
    {
        Projectile proj = Projectile;
        if (proj.ai[2] > 0)
        {
            proj.velocity = Vector2.Zero;
            return;
        }
        int age = 360 - proj.timeLeft;
        if (age < 6)
            return;
        proj.tileCollide = true;
        float seed = proj.ai[1];
        float phase = seed * 0.017f;
        float amplitude = 0.8f + seed % 17 / 17 * 0.9f;
        float sway = MathF.Sin(age * 0.09f + phase) * amplitude;
        float ramp = Math.Min(1, age / 12f);
        proj.velocity = new Vector2(proj.velocity.X + (sway - proj.velocity.X) * 0.3f * ramp, Math.Min(1.8f, proj.velocity.Y + 0.045f));
        proj.rotation = MathF.Sin(age * 0.126f + phase * 1.3f) * 0.7f * ramp;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile proj = Projectile;
        proj.ai[2] = 1;
        proj.tileCollide = false;
        proj.velocity = Vector2.Zero;
        proj.timeLeft = Math.Min(proj.timeLeft, 18);
        return false;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        int age = 360 - proj.timeLeft;
        float opacity = Math.Min(1, Math.Min(age / 12f, proj.timeLeft / 18f));
        if (opacity <= 0)
            return false;
        Vector2 center = proj.Center;
        Vector2 camera = Main.screenPosition;
        float x = center.X - camera.X;
        float y = center.Y - camera.Y;
        if (x < -20 || y < -20 || x > Main.screenWidth + 20 || y > Main.screenHeight + 20)
            return false;
        int seed = (int)proj.ai[1];
        Color color = (int)proj.ai[0] == 1 ? palePetal : petalTints[Math.Abs(seed) % petalTints.Length];
        int frame = (age + Math.Abs(seed) % 33) / 3 % 11;
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        var position = new Vector2(x, y);
        var source = new Rectangle(0, frame * 10, 10, 10);
        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        if (options.glow && options.level >= 2 && (seed & 3) == 0)
            Main.EntitySpriteDraw(texture, position, source, Color.Multiply(glowPetal, opacity * 0.19f), proj.rotation, petalOrigin, proj.scale * 1.35f, SpriteEffects.None, 0);
        Main.EntitySpriteDraw(texture, position, source, Color.Multiply(color, opacity), proj.rotation, petalOrigin, proj.scale, SpriteEffects.None, 0);
        return false;
    }
}
