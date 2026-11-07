using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using umamusumevanities.Common.Players;

namespace umamusumevanities.Content.Effects.Tails;

public class UmamusumeTailVisual : ModProjectile
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
        int tail = UmamusumeTailPlayer.GetVisibleUmaTail(player);
        if (tail < 0)
        {
            proj.Kill();
            return;
        }
        proj.ai[0] = tail;
        int direction = player.direction < 0 ? -1 : 1;
        proj.spriteDirection = direction;
        proj.Center = new Vector2(player.Center.X - direction * 5, player.position.Y + player.height - 13);
        proj.velocity = Vector2.Zero;
        proj.rotation = player.GetModPlayer<UmamusumeTailPlayer>().tailRotation;
        proj.gfxOffY = player.gfxOffY;
        proj.timeLeft = 2;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        int tail = Math.Max(0, Math.Min(5, (int)proj.ai[0]));
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        var source = new Rectangle(0, tail * 10, 26, 10);
        bool left = proj.spriteDirection < 0;
        var origin = new Vector2(left ? 0 : 25, 0);
        SpriteEffects effects = left ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Vector2 position = proj.Center - Main.screenPosition;
        position.Y += proj.gfxOffY;
        Main.EntitySpriteDraw(texture, position, source, lightColor, proj.rotation, origin, 1.2f, effects, 0);
        return false;
    }
}
