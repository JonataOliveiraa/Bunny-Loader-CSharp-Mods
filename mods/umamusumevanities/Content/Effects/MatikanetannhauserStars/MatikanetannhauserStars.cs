using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using umamusumevanities.Common.Effects;

namespace umamusumevanities.Content.Effects.MatikanetannhauserStars;

public class MatikanetannhauserStars : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 70;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.drawLayer = ProjectileDrawLayerID.OverPlayers;
    }

    public override bool? CanDamage() => false;

    public override void AI()
    {
        Projectile proj = Projectile;
        Player owner = Main.player[proj.owner];
        if (owner.IsNull || !owner.active || owner.dead)
        {
            proj.Kill();
            return;
        }
        proj.ai[0] += proj.ai[1];
        proj.rotation += proj.ai[1] * 0.8f;
        float angle = proj.ai[0];
        proj.Center = new Vector2(owner.Center.X + MathF.Cos(angle) * 16, owner.position.Y - 6 + MathF.Sin(angle) * 5 + owner.gfxOffY);
        proj.velocity = Vector2.Zero;
        proj.drawLayer = ProjectileDrawLayerID.OverPlayers;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        int remaining = proj.timeLeft;
        int elapsed = 70 - remaining;
        float opacity = Math.Min(1, elapsed / 24f) * Math.Min(1, remaining / 15f);
        if (opacity <= 0)
            return false;
        Vector2 center = proj.Center;
        Vector2 camera = Main.screenPosition;
        float x = center.X - camera.X;
        float y = center.Y - camera.Y;
        if (x < -35 || y < -35 || x > Main.screenWidth + 35 || y > Main.screenHeight + 35)
            return false;

        float angle = proj.ai[0];
        float perspective = 1 + MathF.Sin(angle) * 0.24f;
        float pulse = 1 + MathF.Sin(elapsed * 0.3f + angle) * 0.12f;
        float scale = proj.scale * perspective * pulse;
        Color light = Lighting.GetColor((int)Math.Floor(center.X / 16), (int)Math.Floor(center.Y / 16));
        var tint = new Color(Math.Max(190, (int)light.R), Math.Max(185, (int)light.G), Math.Max(125, (int)light.B));
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        var position = new Vector2(x, y);
        var source = new Rectangle(0, 0, 18, 14);
        var origin = new Vector2(9, 7);

        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        if (options.level >= 1 && options.glow)
            Main.EntitySpriteDraw(texture, position, source, Color.Multiply(new Color(255, 222, 118), opacity * (options.level >= 3 ? 0.37f : 0.23f)), proj.rotation, origin, scale * 1.42f, SpriteEffects.None, 0);
        Main.EntitySpriteDraw(texture, position, source, Color.Multiply(tint, opacity * 0.95f), proj.rotation, origin, scale, SpriteEffects.None, 0);
        if (options.glow && options.level >= 2 && (elapsed < 16 || MathF.Sin(elapsed * 0.32f + angle * 2) > 0.84f))
            Main.EntitySpriteDraw(texture, position, source, Color.Multiply(Color.White, opacity * 0.34f), proj.rotation, origin, scale * 0.7f, SpriteEffects.None, 0);
        return false;
    }
}
