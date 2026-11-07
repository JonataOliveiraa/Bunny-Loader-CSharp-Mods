using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using umamusumevanities.Common.Players;

namespace umamusumevanities.Content.Effects.Hair;

public class UmamusumeHairVisual : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 2;
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
        Player player = Main.player[proj.owner];
        if (player.IsNull || !player.active || player.dead || (!player.mount.IsNull && player.mount.Active))
        {
            proj.Kill();
            return;
        }
        int hair = UmamusumeHairPlayer.GetVisibleUmaHair(player);
        if (hair < 0)
        {
            proj.Kill();
            return;
        }
        proj.ai[0] = hair;
        proj.spriteDirection = player.direction < 0 ? -1 : 1;
        proj.Center = new Vector2(player.Center.X, player.position.Y + player.height - 32);
        proj.velocity = Vector2.Zero;
        proj.rotation = player.GetModPlayer<UmamusumeHairPlayer>().rotation + player.headRotation;
        proj.gfxOffY = player.gfxOffY;
        proj.timeLeft = 2;
    }

    public override bool PreDraw(Player owner, ref Color lightColor)
    {
        Projectile proj = Projectile;
        Player player = Main.player[proj.owner];
        if (player.IsNull || !player.active)
            return false;
        int index = Math.Max(0, Math.Min(7, (int)proj.ai[0]));
        Rectangle body = player.bodyFrame;
        int frame = body.Height > 0 ? Math.Max(0, Math.Min(19, body.Y / body.Height)) : 0;
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        var source = new Rectangle(index * 40, frame * 56, 40, 56);
        SpriteEffects effects = proj.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Vector2 position = proj.Center - Main.screenPosition;
        position.Y += proj.gfxOffY;
        Main.EntitySpriteDraw(texture, position, source, lightColor, proj.rotation, new Vector2(20, 20), 1f, effects, 0);
        return false;
    }
}
