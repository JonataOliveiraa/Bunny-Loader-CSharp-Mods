using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using umamusumevanities.Common.Effects;

namespace umamusumevanities.Content.Effects.SymboliRudolfFlash;

public class SymboliRudolfFlash : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 21;
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
        proj.Center = owner.Center;
        proj.gfxOffY = owner.gfxOffY;
        proj.velocity = Vector2.Zero;
        proj.drawLayer = ProjectileDrawLayerID.OverPlayers;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        Vector2 camera = Main.screenPosition;
        Vector2 center = proj.Center;
        float x = center.X - camera.X;
        float y = center.Y + proj.gfxOffY - camera.Y;
        if (x < -70 || y < -70 || x > Main.screenWidth + 70 || y > Main.screenHeight + 70)
            return false;
        int elapsed = 21 - proj.timeLeft;
        int frame = Math.Min(6, elapsed / 3);
        float fade = Math.Min(1, Math.Min((elapsed + 1) / 3f, proj.timeLeft / 5f));
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        var position = new Vector2(x, y);
        var source = new Rectangle(0, frame * 96, 96, 96);
        var origin = new Vector2(48, 48);
        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        if (options.glow && options.level >= 1)
            Main.EntitySpriteDraw(texture, position, source, Color.Multiply(new Color(132, 195, 255), fade * (options.level >= 3 ? 0.31f : 0.2f)), proj.rotation, origin, proj.scale * 1.13f, SpriteEffects.None, 0);
        Main.EntitySpriteDraw(texture, position, source, Color.Multiply(Color.White, fade * 0.82f), proj.rotation, origin, proj.scale, SpriteEffects.None, 0);
        if (options.glow && options.level >= 3 && elapsed >= 3 && elapsed < 12)
            Main.EntitySpriteDraw(texture, position, source, Color.Multiply(new Color(172, 220, 255), fade * 0.23f), proj.rotation, origin, proj.scale * 1.36f, SpriteEffects.None, 0);
        return false;
    }
}
