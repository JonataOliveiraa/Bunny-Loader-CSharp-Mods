using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using umamusumevanities.Common.Effects;

namespace umamusumevanities.Content.Effects.RunningTrails;

public class UmamusumeTrailStreak : ModProjectile
{
    static readonly Color[] colorCache =
    {
        new(79, 136, 204),
        new(255, 255, 255),
        new(245, 245, 255),
        new(255, 60, 60),
        new(156, 146, 230),
        new(255, 218, 88),
        new(255, 218, 88),
        new(255, 255, 255),
        new(190, 214, 255),
    };

    static readonly Color[] rainbowCache =
    {
        new(255, 70, 70), new(255, 174, 45), new(255, 241, 55),
        new(65, 235, 115), new(65, 165, 255), new(184, 99, 255),
    };

    static readonly Color[] riceCache = { new(142, 112, 222), new(124, 140, 224), new(219, 149, 216), new(255, 224, 236) };

    static readonly Color[] cafeCache = { new(255, 218, 88), new(255, 218, 88), new(255, 218, 88), new(125, 204, 255) };

    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 18;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = false;
    }

    public override bool? CanDamage() => false;

    public override void AI()
    {
        Projectile.rotation = Projectile.ai[2] < 0 ? MathF.PI : 0;
        Projectile.velocity = Vector2.Zero;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        int theme = (int)proj.ai[0];
        int variant = (int)proj.ai[1];
        Color tint = theme == 1 ? rainbowCache[variant % rainbowCache.Length]
            : theme == 4 ? riceCache[variant % riceCache.Length]
            : theme == 5 ? cafeCache[variant % cafeCache.Length]
            : theme >= 0 && theme < colorCache.Length ? colorCache[theme] : colorCache[8];
        float fade = Math.Min(1, (18 - proj.timeLeft + 1) / 4f) * Math.Min(1, proj.timeLeft / 9f);
        Color shade = Color.Multiply(tint, fade);
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Vector2 position = proj.Center - Main.screenPosition;
        if (position.X < -70 || position.Y < -70 || position.X > Main.screenWidth + 70 || position.Y > Main.screenHeight + 70)
            return false;
        var source = new Rectangle(0, 0, texture.Width, texture.Height);
        var origin = new Vector2(texture.Width, texture.Height * 0.5f);
        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        if (options.glow && options.level >= 2)
            Main.EntitySpriteDraw(texture, position, source, Color.Multiply(tint, fade * 0.21f), proj.rotation, origin, proj.scale * 1.32f, SpriteEffects.None, 0);
        Main.EntitySpriteDraw(texture, position, source, shade, proj.rotation, origin, proj.scale, SpriteEffects.None, 0);
        if (options.glow && options.level >= 3 && proj.timeLeft > 8)
            Main.EntitySpriteDraw(texture, position, source, Color.Multiply(Color.White, fade * 0.21f), proj.rotation, origin, proj.scale * 0.64f, SpriteEffects.None, 0);
        return false;
    }
}
