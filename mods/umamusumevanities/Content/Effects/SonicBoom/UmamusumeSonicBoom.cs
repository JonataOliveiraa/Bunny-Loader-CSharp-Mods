using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using umamusumevanities.Common.Effects;

namespace umamusumevanities.Content.Effects.SonicBoom;

public class UmamusumeSonicBoom : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 24;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = false;
    }

    public override bool? CanDamage() => false;

    public override void AI() => Projectile.velocity = new Vector2(Projectile.velocity.X * 0.9f, 0);

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        int remaining = Math.Max(0, Math.Min(24, proj.timeLeft));
        int elapsed = 24 - remaining;
        int frame = Math.Min(5, elapsed / 4);
        float fade = Math.Min(1, elapsed / 3f) * Math.Min(1, remaining / 10f);
        if (fade <= 0)
            return false;

        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Vector2 position = proj.Center - Main.screenPosition;
        Color color = Color.Multiply(lightColor, fade);
        var origin = new Vector2(32, 32);
        Main.EntitySpriteDraw(texture, position, new Rectangle(0, frame * 64, 64, 64), color, 0f, origin, 1f, SpriteEffects.None, 0);
        Main.EntitySpriteDraw(texture, position, new Rectangle(64, frame * 64, 64, 64), color, 0f, origin, 1f, SpriteEffects.None, 0);
        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        if (options.glow && options.level >= 3 && elapsed < 14)
            Main.EntitySpriteDraw(texture, position, new Rectangle(64, frame * 64, 64, 64), Color.Multiply(new Color(197, 224, 255), fade * 0.22f), 0f, origin, 1.22f, SpriteEffects.None, 0);
        return false;
    }
}
