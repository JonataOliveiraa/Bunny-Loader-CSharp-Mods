using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace umamusumevanities.Content.Effects.GoldShipAnchor;

public class GoldShipAnchor : ModProjectile
{
    public override void SetDefaults()
    {
        Projectile.width = 16;
        Projectile.height = 12;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 150;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = false;
        Projectile.hostile = false;
    }

    public override bool? CanDamage() => false;

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Land();
        return false;
    }

    void Land()
    {
        Projectile.ai[1] = 1;
        Projectile.velocity = Vector2.Zero;
        Projectile.tileCollide = false;
        Projectile.timeLeft = Math.Min(Projectile.timeLeft, 45);
    }

    public override void AI()
    {
        Projectile proj = Projectile;
        Player player = Main.player[proj.owner];
        if (player.IsNull || !player.active || player.dead)
        {
            proj.Kill();
            return;
        }
        if (++proj.ai[0] <= 6)
            return;
        if (proj.ai[1] == 1)
        {
            proj.velocity = Vector2.Zero;
            return;
        }
        proj.tileCollide = true;
        proj.velocity = new Vector2(proj.velocity.X, Math.Min(proj.velocity.Y + 0.35f, 16));
        proj.rotation = MathF.Atan2(proj.velocity.Y, proj.velocity.X);
    }

    public override bool PreDraw(Player owner, ref Color lightColor)
    {
        Projectile proj = Projectile;
        Player player = Main.player[proj.owner];
        if (player.IsNull || !player.active)
            return false;
        float alpha = Math.Min(1, proj.timeLeft / 45f);
        if (alpha <= 0)
            return false;

        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Vector2 start = player.Center;
        Vector2 end = proj.Center;
        float dx = end.X - start.X;
        float dy = end.Y - start.Y;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        float angle = MathF.Atan2(dy, dx);
        Vector2 camera = Main.screenPosition;
        var chainRect = new Rectangle(0, 18, 12, 10);
        var origin = new Vector2(6, 5);
        int count = (int)MathF.Ceiling(length / 12);
        int screenWidth = Main.screenWidth;
        int screenHeight = Main.screenHeight;
        int drawn = 0;
        int lastTileX = int.MinValue;
        int lastTileY = int.MinValue;
        Color tileLight = default;

        for (int i = 0; i < count && drawn < 85; i++)
        {
            float distance = (i + 0.5f) * 12;
            float t = distance / Math.Max(1, length);
            if (t >= 1)
                break;
            float x = start.X + dx * t - camera.X;
            float y = start.Y + dy * t - camera.Y;
            if (x < -20 || y < -20 || x > screenWidth + 20 || y > screenHeight + 20)
                continue;
            int tileX = (int)Math.Floor((x + camera.X) / 16);
            int tileY = (int)Math.Floor((y + camera.Y) / 16);
            if (tileX != lastTileX || tileY != lastTileY)
            {
                tileLight = Lighting.GetColor(tileX, tileY);
                lastTileX = tileX;
                lastTileY = tileY;
            }
            Main.EntitySpriteDraw(texture, new Vector2(x, y), chainRect, Color.Multiply(tileLight, alpha), angle, origin, 1f, SpriteEffects.None, 0);
            drawn++;
        }

        Main.EntitySpriteDraw(texture, end - camera, new Rectangle(0, 0, 26, 18), Color.Multiply(lightColor, alpha), proj.rotation, new Vector2(13, 9), 1f, SpriteEffects.None, 0);
        return false;
    }
}
