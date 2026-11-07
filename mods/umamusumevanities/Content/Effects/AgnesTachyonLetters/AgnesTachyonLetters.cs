using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using umamusumevanities.Common.Effects;

namespace umamusumevanities.Content.Effects.AgnesTachyonLetters;

public class AgnesTachyonLetters : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 90;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = false;
    }

    public override bool? CanDamage() => false;

    public override void AI() => Projectile.velocity = Vector2.Zero;

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        int direction = proj.ai[0] < 0 ? -1 : 1;
        int count = Math.Max(3, Math.Min(14, (int)proj.ai[1]));
        int seed = (int)proj.ai[2];
        int elapsed = 90 - proj.timeLeft;
        float fade = Math.Min(1, proj.timeLeft / 16f);
        if (fade <= 0)
            return false;
        Vector2 center = proj.Center;
        Vector2 camera = Main.screenPosition;
        float x = center.X - camera.X;
        float y = center.Y - camera.Y;
        if (x < -150 || x > Main.screenWidth + 150 || y < -25 || y > Main.screenHeight + 25)
            return false;

        Color light = Lighting.GetColor((int)Math.Floor(center.X / 16), (int)Math.Floor(center.Y / 16));
        var tint = new Color(Math.Max(186, (int)light.R), Math.Max(161, (int)Math.Floor(light.G * 218 / 255f + 0.5f)), Math.Max(68, (int)Math.Floor(light.B * 88 / 255f + 0.5f)));
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        var origin = new Vector2(3, 3);
        VFXOptions options = UmamusumeVFXSettings.GetVFX();

        for (int i = 0; i < count; i++)
        {
            int age = elapsed - i * 2;
            if (age <= 0)
                continue;
            float ink = Math.Min(1, age / 5f);
            float entrance = MathF.Sin(MathF.PI * Math.Min(1, age / 10f));
            float bob = MathF.Sin(elapsed * 0.13f + i * 0.9f + seed * 0.01f) * 1.8f;
            float rise = Math.Min(4, age * 0.045f);
            float offsetY = bob - rise - (1 - ink) * 4;
            float rotation = MathF.Sin(elapsed * 0.12f + i * 0.75f) * 0.10f * (0.45f + entrance * 0.55f);
            float size = 1.12f * (0.5f + ink * 0.5f + entrance * 0.22f);
            float flicker = 0.91f + MathF.Sin(elapsed * 0.23f + i * 1.6f) * 0.09f;
            float opacity = fade * ink * flicker;
            int frame = ((seed + i * 43 + i * i * 7) % 18 + 18) % 18;
            var source = new Rectangle(0, frame * 6, 6, 6);
            var position = new Vector2(x - (i + 1) * 7 * direction, y + offsetY);

            if (options.glow && options.level >= 2 && age < 12)
                Main.EntitySpriteDraw(texture, position, source, Color.Multiply(new Color(255, 211, 78), opacity * 0.42f), rotation, origin, size * 1.55f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, position, source, Color.Multiply(tint, opacity), rotation, origin, size, SpriteEffects.None, 0);
            if (options.glow && options.level >= 3 && age < 15 && i % 2 == 0)
                Main.EntitySpriteDraw(texture, position, source, Color.Multiply(Color.White, opacity * 0.34f), rotation, origin, size * 0.64f, SpriteEffects.None, 0);
        }
        return false;
    }
}
