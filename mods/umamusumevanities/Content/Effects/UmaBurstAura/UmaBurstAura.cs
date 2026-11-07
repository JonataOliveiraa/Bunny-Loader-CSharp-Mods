using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using umamusumevanities.Common.Effects;

namespace umamusumevanities.Content.Effects.UmaBurstAura;

public class UmaBurstAura : ModProjectile
{
    static readonly Color purpleAura = new(205, 120, 255);
    static readonly Color goldAura = new(255, 220, 110);
    static readonly Rectangle auraSource = new(0, 0, 100, 100);
    static readonly Vector2 auraOrigin = new(50, 50);

    public override void SetDefaults()
    {
        Projectile.width = 2;
        Projectile.height = 2;
        Projectile.aiStyle = 0;
        Projectile.timeLeft = 22;
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
        if (player.IsNull || !player.active || player.dead)
        {
            proj.Kill();
            return;
        }
        proj.Center = new Vector2(player.Center.X + proj.ai[1], player.Center.Y + proj.ai[2]);
        proj.velocity = Vector2.Zero;
        proj.gfxOffY = player.gfxOffY;
        proj.ai[2] -= 0.25f;
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        Color tint = (int)proj.ai[0] == 3 ? purpleAura : goldAura;
        int remaining = Math.Max(0, Math.Min(22, proj.timeLeft));
        float fade = Math.Min(1, (22 - remaining) / 4f) * Math.Min(1, remaining / 16f);
        if (fade <= 0)
            return false;
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Vector2 pos = proj.Center - Main.screenPosition;
        pos.Y += proj.gfxOffY;
        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        Main.EntitySpriteDraw(texture, pos, auraSource, Color.Multiply(tint, fade * 0.33f), 0f, auraOrigin, proj.scale, SpriteEffects.None, 0);
        if (options.glow && options.level >= 3)
            Main.EntitySpriteDraw(texture, pos, auraSource, Color.Multiply(Color.White, fade * 0.12f), 0f, auraOrigin, proj.scale * 0.75f, SpriteEffects.None, 0);
        return false;
    }
}
