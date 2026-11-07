using System;
using System.Collections.Generic;
using kineticgore.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace kineticgore.Content.Projectiles;

// What an arrow is stuck in: an NPC, or a corpse, which the token tells from a reused slot.
public readonly struct StuckTarget
{
    readonly bool corpse;
    readonly int index;
    readonly float token;

    StuckTarget(bool corpse, int index, float token)
    {
        this.corpse = corpse;
        this.index = index;
        this.token = token;
    }

    public static StuckTarget OfNPC(int index) => new(false, index, 0);

    public static StuckTarget OfCorpse(int index, float token) => new(true, index, token);

    public bool TryGet(out Vector2 center, out float rotation)
    {
        center = default;
        rotation = 0;
        if (!corpse)
        {
            NPC npc = Main.npc[index];
            if (npc.IsNull || !npc.active)
                return false;
            center = npc.Center;
            rotation = npc.rotation;
            return true;
        }
        Projectile proj = Main.projectile[index];
        if (proj.IsNull || !proj.active || proj.ai[0] != token)
            return false;
        center = proj.Center;
        rotation = proj.rotation;
        return true;
    }
}

public class StuckArrow : ModProjectile
{
    const int Lifetime = 90;

    sealed class ArrowInfo
    {
        public int Token;
        public StuckTarget Target;
        public int ArrowType;
        public Vector2 Offset;
        public float RotationOffset;
    }

    static readonly Dictionary<int, ArrowInfo> arrows = new();

    public static int SpawnStuckArrow(StuckTarget target, in ProjectileHit hit)
    {
        if (!target.TryGet(out Vector2 anchorCenter, out float anchorRotation))
            return -1;
        float speed = MathF.Sqrt(hit.VelocityX * hit.VelocityX + hit.VelocityY * hit.VelocityY);
        if (speed == 0)
            speed = 1;
        Vector2 jitter = GoreShared.Circular(3, 3);
        float x = hit.X + hit.W / 2f + hit.VelocityX / speed * 12 + jitter.X;
        float y = hit.Y + hit.H / 2f + hit.VelocityY / speed * 12 + jitter.Y;
        int token = GoreShared.NewToken();
        int type = ModContent.ProjectileType<StuckArrow>();
        int index = Projectile.NewProjectile(Projectile.GetNoneSource(), new Vector2(x, y), Vector2.Zero, type, 0, 0f, Main.myPlayer, token, 0f, 0f);
        if (index < 0 || index >= Main.maxProjectiles)
            return -1;
        float rotation = hit.Rotation + (Random.Shared.NextSingle() - 0.5f) * 0.2f;
        arrows[index] = new ArrowInfo
        {
            Token = token,
            Target = target,
            ArrowType = hit.Type > 0 ? hit.Type : 1,
            Offset = GoreShared.Rotate(x - anchorCenter.X, y - anchorCenter.Y, -anchorRotation),
            RotationOffset = rotation - anchorRotation,
        };
        Main.projectile[index].rotation = rotation;
        return index;
    }

    static ArrowInfo? InfoOf(Projectile proj)
    {
        return arrows.TryGetValue(proj.whoAmI, out ArrowInfo? info) && info.Token == proj.ai[0] ? info : null;
    }

    public override void SetDefaults()
    {
        Projectile.width = 10;
        Projectile.height = 10;
        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.alpha = 0;
        Projectile.ignoreWater = true;
        Projectile.tileCollide = false;
    }

    public override void AI()
    {
        Projectile proj = Projectile;
        ArrowInfo? info = InfoOf(proj);
        if (info == null || !info.Target.TryGet(out Vector2 anchorCenter, out float anchorRotation) || Main.netMode == NetmodeID.Server)
        {
            proj.Kill();
            return;
        }
        Vector2 offset = GoreShared.Rotate(info.Offset.X, info.Offset.Y, anchorRotation);
        proj.Center = anchorCenter + offset;
        proj.velocity = Vector2.Zero;
        proj.rotation = anchorRotation + info.RotationOffset;
        if (proj.timeLeft < 20)
            proj.alpha = Math.Min(255, proj.alpha + 12);
    }

    public override void OnKill(int timeLeft)
    {
        Projectile proj = Projectile;
        if (InfoOf(proj) != null)
            arrows.Remove(proj.whoAmI);
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        ArrowInfo? info = InfoOf(proj);
        if (info == null || !GoreShared.TryProjectileTexture(info.ArrowType, out Texture2D texture))
            return false;
        Vector2 center = proj.Center;
        Color light = Lighting.GetColor((int)center.X >> 4, (int)center.Y >> 4);
        Color color = Color.Multiply(light, (255 - proj.alpha) / 255f);
        Vector2 screen = Main.screenPosition;
        Main.EntitySpriteDraw(texture, new Vector2(center.X - screen.X, center.Y - screen.Y), new Rectangle(0, 0, texture.Width, texture.Height), color,
            proj.rotation, new Vector2(texture.Width / 2f, texture.Height / 2f), proj.scale, SpriteEffects.None, 0f);
        return false;
    }
}
