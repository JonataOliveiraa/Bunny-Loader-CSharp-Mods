using System;
using System.Collections.Generic;
using kineticgore.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace kineticgore.Content.Projectiles;

public static class CorpsePart
{
    public const int Whole = 0;
    public const int Headless = 1;
    public const int HeadOnBody = 2;
    public const int Head = 3;
    public const int Upper = 4;
    public const int Legs = 5;
}

// What a corpse keeps of the NPC it came from.
public class CorpseBody
{
    public int Type;
    public int NetID;
    public float Scale;
    public int Width;
    public int Height;
    public bool Humanoid;
    public bool Slime;
    public Color? Tint;
    public int Alpha;
    public bool FlyingBody;
    public int Direction;
    public Texture2D Texture;
    public bool HasTexture;
    public int TextureWidth;
    public int FrameHeight;
}

public class CorpseInfo : CorpseBody
{
    public int Token;
    public int Dismember;
    public float Angular;
    public float HeadRotation;
    public float HeadVelocity;
    public int Integrity = 100;
    public int HitCooldown;
    public float Squash = 1;
    public float SquashVelocity;
    public int Bleed;
    public bool Started;
    public float X;
    public float Y;
    public int W = 14;
    public int H = 14;

    public CorpseInfo(CorpseBody body)
    {
        Type = body.Type;
        NetID = body.NetID;
        Scale = body.Scale;
        Width = body.Width;
        Height = body.Height;
        Humanoid = body.Humanoid;
        Slime = body.Slime;
        Tint = body.Tint;
        Alpha = body.Alpha;
        FlyingBody = body.FlyingBody;
        Direction = body.Direction;
        Texture = body.Texture;
        HasTexture = body.HasTexture;
        TextureWidth = body.TextureWidth;
        FrameHeight = body.FrameHeight;
    }
}

public class PhysicsCorpse : ModProjectile
{
    const float Neck = 0.38f;
    const float Waist = 0.75f;
    const int Overlap = 2;
    const int MinSize = 14;
    const int HitCooldownTicks = 45;
    const float TwoPi = MathF.PI * 2;

    static readonly List<int> stale = new();

    public static int SpawnCorpse(CorpseBody body, Vector2 position, Vector2 velocity, int dismember)
    {
        int type = ModContent.ProjectileType<PhysicsCorpse>();
        LimitCorpses(type);
        int token = GoreShared.NewToken();
        int index = Projectile.NewProjectile(Projectile.GetNoneSource(), position, velocity, type, 0, 0f, Main.myPlayer, token, 0f, 0f);
        if (index < 0 || index >= Main.maxProjectiles)
            return -1;
        GoreShared.Corpses[index] = new CorpseInfo(body)
        {
            Token = token,
            Dismember = dismember,
            X = position.X,
            Y = position.Y,
        };
        return index;
    }

    public static CorpseBody FromNPC(NPC npc)
    {
        int type = npc.type;
        bool hasTexture = GoreShared.TryNpcTexture(type, out Texture2D texture);
        int frames = Math.Max(1, Main.npcFrameCount[type]);
        Color color = npc.color;
        return new CorpseBody
        {
            Type = type,
            NetID = npc.netID,
            Scale = npc.scale != 0 ? npc.scale : 1,
            Width = npc.width,
            Height = npc.height,
            Humanoid = GoreShared.IsHumanoid(npc.aiStyle, npc.townNPC, type),
            Slime = GoreShared.IsSlime(npc.aiStyle, type),
            Tint = color.R != 0 || color.G != 0 || color.B != 0 || color.A != 0 ? color : null,
            Alpha = npc.alpha,
            FlyingBody = npc.noGravity,
            Direction = npc.spriteDirection,
            Texture = texture,
            HasTexture = hasTexture,
            TextureWidth = hasTexture ? texture.Width : 0,
            FrameHeight = hasTexture ? texture.Height / frames : 0,
        };
    }

    static void LimitCorpses(int type)
    {
        Dictionary<int, CorpseInfo> corpses = GoreShared.Corpses;
        int max = GoreShared.Config.MaxCorpses;
        if (corpses.Count < max)
            return;
        int oldest = -1;
        int oldestTime = int.MaxValue;
        stale.Clear();
        foreach (int index in corpses.Keys)
        {
            Projectile proj = Main.projectile[index];
            if (!proj.active || proj.type != type)
            {
                stale.Add(index);
                continue;
            }
            if (proj.timeLeft < oldestTime)
            {
                oldestTime = proj.timeLeft;
                oldest = index;
            }
        }
        foreach (int index in stale)
            corpses.Remove(index);
        if (corpses.Count >= max && oldest >= 0)
        {
            corpses.Remove(oldest);
            Main.projectile[oldest].Kill();
        }
    }

    public override void SetDefaults()
    {
        Projectile.width = MinSize;
        Projectile.height = MinSize;
        Projectile.aiStyle = -1;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 1200;
        Projectile.tileCollide = true;
        Projectile.ignoreWater = false;
    }

    public override void AI()
    {
        Projectile proj = Projectile;
        CorpseInfo? found = GoreShared.InfoOf(proj);
        if (found == null || Main.netMode == NetmodeID.Server)
        {
            proj.Kill();
            return;
        }
        CorpseInfo info = found;
        KineticGoreConfig config = GoreShared.Config;
        float vx = proj.velocity.X;
        float vy = proj.velocity.Y;

        if (!info.Started)
        {
            info.Started = true;
            proj.timeLeft = Math.Max(60, GoreShared.Ticks(config.CorpseDuration));
            if (info.FlyingBody)
                vy += 4;
            if (info.Dismember == CorpsePart.Head)
            {
                Vector2 kick = GoreShared.Circular(1, 1);
                vx += kick.X;
                vy += kick.Y;
            }
            else if (info.Dismember == CorpsePart.Headless)
            {
                info.Bleed = GoreShared.Ticks(config.BleedDuration);
            }
            info.HeadRotation = proj.rotation;
        }

        if (config.EnableRagdollCollision && Separate(proj, out float pushX, out float pushY))
        {
            proj.position = new Vector2(proj.position.X + pushX, proj.position.Y + pushY);
            if (pushY != 0)
            {
                vx *= 0.9f;
                if (vy > 0 && pushY < 0)
                    vy = 0;
            }
        }

        Resize(proj, info);

        if (info.Dismember == CorpsePart.Headless && info.Bleed > 0)
        {
            info.Bleed--;
            if (info.Bleed % 3 == 0 && info.FrameHeight > 0)
            {
                float neck = info.FrameHeight * Neck - info.FrameHeight / 2f;
                Vector2 offset = GoreShared.Rotate(0, neck, proj.rotation);
                Vector2 spray = GoreShared.Rotate(0, -GoreShared.Rand(2, 5), proj.rotation);
                Vector2 jitter = GoreShared.Circular(1, 1);
                Vector2 at = proj.Center;
                float speed = config.BloodVelocity;
                GoreShared.SpawnBlood(new Vector2(at.X + offset.X, at.Y + offset.Y),
                    new Vector2((spray.X + jitter.X + vx * 0.5f) * speed, (spray.Y + jitter.Y + vy * 0.5f) * speed), 0, config.BloodScale);
            }
        }

        if (proj.timeLeft < 60)
            proj.alpha = GoreShared.JsRound(255 - 255.0 * proj.timeLeft / 60);

        int scaledW = GoreShared.JsRound(info.Scale * proj.width);
        int scaledH = GoreShared.JsRound(info.Scale * proj.height);
        if (Collision.WetCollision(proj.position, scaledW, scaledH))
        {
            vy -= 0.4f;
            vx *= 0.95f;
            vy *= 0.95f;
            if (vy < -2)
                vy = -2;
            info.Angular *= 0.9f;
        }
        else
        {
            vy += config.Gravity * 0.85f;
            if (vy > 16)
                vy = 16;
        }

        Vector2 center = proj.Center;
        int tileX = (int)MathF.Floor(center.X / 16);
        int tileY = (int)MathF.Floor((center.Y + 10) / 16);
        bool hasTile = tileX >= 0 && tileY >= 0 && tileX < Main.maxTilesX && tileY < Main.maxTilesY;
        Tile tile = hasTile ? Main.tile[tileX, tileY] : default;
        bool solidBelow = hasTile && tile.active() && Main.tileSolid[tile.type];
        if (vy == 0 || solidBelow)
        {
            float friction = info.Dismember == CorpsePart.Head ? MathF.Min(0.98f, config.Friction + 0.06f) : config.Friction;
            int slope = hasTile ? tile.slope() : 0;
            if (slope != 0 || (hasTile && tile.halfBrick()))
            {
                if (slope == 2)
                    vx -= 0.5f;
                if (slope == 1)
                    vx += 0.5f;
                info.Angular += vx * 0.04f;
            }
            else
            {
                vx *= friction;
                info.Angular *= config.LegacyRagdollPhysics ? friction : friction * 0.8f;
            }
            if (MathF.Abs(vx) < 0.1f)
                vx = 0;
        }
        else if (!config.LegacyRagdollPhysics)
        {
            info.Angular += (vx * 0.015f - info.Angular) * 0.2f;
        }
        else
        {
            info.Angular = vx * 0.05f;
        }

        if (info.Slime)
        {
            proj.rotation += (0 - proj.rotation) * 0.05f;
            info.Angular *= 0.9f;
            info.SquashVelocity += (1 - info.Squash) * config.SlimeSpringStiffness;
            info.SquashVelocity *= config.SlimeSpringDamping;
            info.Squash += info.SquashVelocity;
        }
        else
        {
            proj.rotation += info.Angular;
        }

        if (info.Humanoid && (info.Dismember == CorpsePart.Whole || info.Dismember == CorpsePart.HeadOnBody || info.Dismember == CorpsePart.Upper))
        {
            info.HeadVelocity += (proj.rotation - info.HeadRotation) * config.HeadSpringStiffness * 0.6f;
            info.HeadVelocity *= config.HeadSpringDamping;
            info.HeadRotation += info.HeadVelocity;
        }
        else if (!info.Humanoid)
        {
            info.HeadRotation = proj.rotation;
        }

        proj.velocity = new Vector2(vx, vy);
        if (info.HitCooldown > 0)
            info.HitCooldown--;
        else
            CheckHits(proj, info);

        info.X = proj.position.X;
        info.Y = proj.position.Y;
        info.W = proj.width;
        info.H = proj.height;
    }

    static bool Separate(Projectile proj, out float pushX, out float pushY)
    {
        pushX = 0;
        pushY = 0;
        float x = proj.position.X, y = proj.position.Y;
        int w = proj.width, h = proj.height;
        float cx = x + w / 2f, cy = y + h / 2f;
        int self = proj.whoAmI;
        foreach (KeyValuePair<int, CorpseInfo> pair in GoreShared.Corpses)
        {
            CorpseInfo other = pair.Value;
            if (pair.Key == self || !other.Started)
                continue;
            float ox = other.X + other.W / 2f, oy = other.Y + other.H / 2f;
            if ((ox - cx) * (ox - cx) + (oy - cy) * (oy - cy) > 3600)
                continue;
            float left = MathF.Max(x, other.X), right = MathF.Min(x + w, other.X + other.W);
            float top = MathF.Max(y, other.Y), bottom = MathF.Min(y + h, other.Y + other.H);
            if (right <= left || bottom <= top)
                continue;
            pushX = 0;
            pushY = 0;
            if (right - left < bottom - top)
                pushX = (cx < ox ? -1 : 1) * (right - left) * 0.2f;
            else
                pushY = (cy < oy ? -1 : 1) * (bottom - top) * 0.2f;
            if (pushY > 0 && Collision.SolidCollision(new Vector2(x + pushX, y + pushY), w, h))
                pushY = 0;
            if (Collision.SolidCollision(new Vector2(x + pushX, y + pushY), w, h))
                continue;
            return true;
        }
        pushX = 0;
        pushY = 0;
        return false;
    }

    static void Resize(Projectile proj, CorpseInfo info)
    {
        int w = info.Width;
        int h = info.Height;
        if (info.Dismember == CorpsePart.Head)
        {
            w = MinSize;
            h = MinSize;
        }
        else if (info.Dismember == CorpsePart.Headless)
        {
            h = Math.Max(MinSize, (int)MathF.Floor(info.Height * 0.65f));
        }
        else if (info.Dismember == CorpsePart.Upper)
        {
            h = (int)MathF.Floor(info.Height * 0.7f);
        }
        else if (info.Dismember == CorpsePart.Legs)
        {
            h = (int)MathF.Floor(info.Height * 0.35f);
        }
        if (!info.Slime && info.Dismember != CorpsePart.Head)
        {
            float angle = proj.rotation % TwoPi;
            if (angle < 0)
                angle += TwoPi;
            if ((angle > MathF.PI / 4 && angle < MathF.PI * 3 / 4) || (angle > 3.926991f && angle < 5.4977875f))
                (w, h) = (h, w);
            w = Math.Max(MinSize, w);
            h = Math.Max(MinSize, h);
        }
        if (proj.width == w && proj.height == h)
            return;
        Vector2 center = proj.Center;
        proj.width = w;
        proj.height = h;
        proj.Center = center;
        int scaledW = GoreShared.JsRound(info.Scale * w);
        int scaledH = GoreShared.JsRound(info.Scale * h);
        if (Collision.SolidCollision(proj.position, scaledW, scaledH))
        {
            var up = new Vector2(proj.position.X, proj.position.Y - 16);
            if (!Collision.SolidCollision(up, scaledW, scaledH))
                proj.position = up;
        }
    }

    static void CheckHits(Projectile proj, CorpseInfo info)
    {
        Vector2 center = proj.Center;
        foreach (SwingHit swing in GoreShared.Swings)
        {
            float dx = swing.Center.X - center.X, dy = swing.Center.Y - center.Y;
            if (dx * dx + dy * dy < swing.Range * swing.Range)
            {
                OnHitByPlayer(proj, info, swing.Direction);
                return;
            }
        }
        float x = proj.position.X, y = proj.position.Y;
        int w = proj.width, h = proj.height;
        KineticGoreConfig config = GoreShared.Config;
        foreach (ProjectileHit hit in GoreShared.Projectiles)
        {
            if (hit.X >= x + w || hit.X + hit.W <= x || hit.Y >= y + h || hit.Y + hit.H <= y)
                continue;
            if (hit.Arrow && config.ArrowsStickToEnemies && hit.Owner == Main.myPlayer)
                StickArrowTo(proj, hit);
            float direction = hit.X + hit.W / 2f < center.X ? 1 : -1;
            if (MathF.Abs(hit.VelocityX) > 0.1f)
                direction = GoreShared.Sign(hit.VelocityX);
            if (hit.Spear && hit.Owner != 255)
                direction = Main.player[hit.Owner].direction;
            OnHitByPlayer(proj, info, direction);
            return;
        }
    }

    static void StickArrowTo(Projectile proj, in ProjectileHit hit)
    {
        StuckArrow.SpawnStuckArrow(StuckTarget.OfCorpse(proj.whoAmI, proj.ai[0]), hit);
        Main.projectile[hit.Index].Kill();
    }

    static void OnHitByPlayer(Projectile proj, CorpseInfo info, float direction)
    {
        KineticGoreConfig config = GoreShared.Config;
        info.HitCooldown = HitCooldownTicks;
        Vector2 v = proj.velocity;
        proj.velocity = new Vector2(v.X + direction * GoreShared.Rand(2, 5) * config.KnockbackMultiplier, v.Y - GoreShared.Rand(2, 4) * config.KnockbackMultiplier);
        info.Angular += direction * (config.LegacyRagdollPhysics ? 0.2f : 0.05f);
        info.HeadVelocity += direction * 0.3f;
        if (info.Slime)
            info.SquashVelocity += 0.2f;
        GoreShared.PlayDeathSound(proj.Center);
        LoseLimb(proj, info, true);

        if (!info.Humanoid || info.Dismember != CorpsePart.Whole || GoreShared.Chance() * 100 >= config.DismembermentChance)
            return;
        Vector2 center = proj.Center;
        Vector2 velocity = proj.velocity;
        if (GoreShared.Chance() < 0.5f)
        {
            info.Dismember = CorpsePart.Headless;
            info.Bleed = GoreShared.Ticks(config.BleedDuration);
            Vector2 neck = GoreShared.Rotate(0, -10, proj.rotation);
            var at = new Vector2(center.X + neck.X, center.Y + neck.Y);
            SpawnCorpse(info, at, new Vector2(velocity.X * 1.1f + direction * 2, velocity.Y * 1.1f - 3), CorpsePart.Head);
            for (int i = 0; i < 10; i++)
                GoreShared.SpawnBloodIn(at, 10, 10, 0, 0, 0, config.BloodScale);
        }
        else
        {
            info.Dismember = CorpsePart.Upper;
            SpawnCorpse(info, center, new Vector2(velocity.X * 0.8f + direction * 1.5f, velocity.Y * 0.8f - 1), CorpsePart.Legs);
            int count = (int)MathF.Floor(8 * config.BloodAmountMultiplier);
            for (int i = 0; i < count; i++)
                GoreShared.SpawnBloodIn(center, 10, 10, 0, 0, 0, config.BloodScale);
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile proj = Projectile;
        CorpseInfo? found = GoreShared.InfoOf(proj);
        if (found == null)
            return true;
        CorpseInfo info = found;
        KineticGoreConfig config = GoreShared.Config;
        float speed = oldVelocity.Length();
        if (speed > 3)
        {
            GoreShared.PlayDigSound(proj.Center);
            info.HeadVelocity += GoreShared.Rand(-0.1f, 0.1f);
            if (info.Slime)
                info.SquashVelocity -= 0.15f;
        }
        if (info.FlyingBody && speed > 6)
        {
            Splatter(proj);
            return true;
        }
        float vx = proj.velocity.X;
        float vy = proj.velocity.Y;
        if (vx != oldVelocity.X)
            vx = -oldVelocity.X * config.Bounciness;
        if (vy != oldVelocity.Y)
        {
            if (oldVelocity.Y > 8)
            {
                LoseLimb(proj, info, false);
                info.HeadVelocity += GoreShared.Sign(vx) * (config.LegacyRagdollPhysics ? 0.3f : 0.1f);
                if (info.Slime)
                    info.SquashVelocity -= 0.3f;
            }
            vy = -oldVelocity.Y * config.Bounciness;
        }
        proj.velocity = new Vector2(vx * 0.9f, vy * 0.9f);
        return false;
    }

    static void LoseLimb(Projectile proj, CorpseInfo info, bool forced)
    {
        if (info.Integrity <= 0 && !forced)
            return;
        info.Integrity -= 15;
        KineticGoreConfig config = GoreShared.Config;
        int count = (int)MathF.Floor(5 * config.BloodAmountMultiplier);
        if (count < 1 && config.BloodAmountMultiplier > 0.1f)
            count = 1;
        Vector2 velocity = proj.velocity;
        for (int i = 0; i < count; i++)
        {
            Dust dust = GoreShared.SpawnBloodIn(proj.position, proj.width, proj.height, 0, 0, 0, 1);
            if (dust.IsNull)
                continue;
            Vector2 jitter = GoreShared.Circular(1.5f, 1.5f);
            dust.velocity = new Vector2(-velocity.X * 0.2f + jitter.X, -velocity.Y * 0.2f + jitter.Y);
            dust.scale = GoreShared.Rand(1, 1.4f) * config.BloodScale;
            dust.noGravity = false;
        }
        if (info.Slime)
            return;
        int more = (int)MathF.Floor(6 * config.BloodAmountMultiplier);
        for (int i = 0; i < more; i++)
            GoreShared.SpawnBloodIn(proj.position, proj.width, proj.height, velocity.X * 0.3f, velocity.Y * 0.3f, 80, config.BloodScale);
    }

    static void Splatter(Projectile proj)
    {
        KineticGoreConfig config = GoreShared.Config;
        Vector2 center = proj.Center;
        GoreShared.PlayDeathSound(center);
        int count = (int)MathF.Floor(20 * config.BloodAmountMultiplier);
        for (int i = 0; i < count; i++)
        {
            Vector2 v = GoreShared.Circular(5, 5);
            GoreShared.SpawnBlood(center, new Vector2(v.X * config.BloodVelocity, (v.Y - 2) * config.BloodVelocity), 0, config.BloodScale);
        }
        for (int i = 0; i < 3; i++)
        {
            Vector2 v = GoreShared.Circular(2, 2);
            Gore.NewGore(center, new Vector2(v.X, v.Y - 1), 61 + Random.Shared.Next(3), 0.7f);
        }
        GoreShared.Corpses.Remove(proj.whoAmI);
        proj.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        Projectile proj = Projectile;
        if (GoreShared.InfoOf(proj) != null)
            GoreShared.Corpses.Remove(proj.whoAmI);
    }

    public override bool PreDraw(Player player, ref Color lightColor)
    {
        Projectile proj = Projectile;
        CorpseInfo? found = GoreShared.InfoOf(proj);
        if (found == null || !found.HasTexture)
            return false;
        CorpseInfo info = found;
        Texture2D texture = info.Texture;
        int width = info.TextureWidth;
        int frameHeight = info.FrameHeight;
        Vector2 center = proj.Center;
        Vector2 screen = Main.screenPosition;
        var at = new Vector2(center.X - screen.X, center.Y - screen.Y);
        Color color = Lighting.GetColor((int)center.X >> 4, (int)center.Y >> 4);
        color = Color.Multiply(color, (255 - proj.alpha) / 255f);
        if (info.Tint is Color tint)
            color = Utils.MultiplyRGBA(tint, color);
        SpriteEffects effects = info.Direction == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        float rotation = proj.rotation;

        if (info.Slime)
        {
            color = Color.Multiply(color, (255 - info.Alpha) / 255f);
            float lift = frameHeight * info.Squash - frameHeight;
            Main.EntitySpriteDraw(texture, new Vector2(at.X, at.Y - lift * 0.5f), new Rectangle(0, 0, width, frameHeight), color, rotation,
                new Vector2(width / 2f, frameHeight / 2f), new Vector2(info.Scale / info.Squash, info.Scale * info.Squash), SpriteEffects.None, 0f);
            return false;
        }
        if (!info.Humanoid)
        {
            Main.EntitySpriteDraw(texture, at, new Rectangle(0, 0, width, frameHeight), color, rotation, new Vector2(width / 2f, frameHeight / 2f), info.Scale, effects, 0f);
            return false;
        }

        int neck = (int)MathF.Floor(frameHeight * Neck);
        int waist = (int)MathF.Floor(frameHeight * Waist);
        var head = new Rectangle(0, 0, width, neck + Overlap);
        float scale = info.Scale;
        if (info.Dismember == CorpsePart.Head)
        {
            Main.EntitySpriteDraw(texture, at, head, color, rotation, new Vector2(width / 2f, neck / 2f), scale, effects, 0f);
            return false;
        }
        if (info.Dismember == CorpsePart.HeadOnBody)
        {
            Main.EntitySpriteDraw(texture, at, head, color, rotation, new Vector2(width / 2f, neck), scale, effects, 0f);
            return false;
        }
        if (info.Dismember == CorpsePart.Legs)
        {
            Main.EntitySpriteDraw(texture, at, new Rectangle(0, waist, width, frameHeight - waist), color, rotation, new Vector2(width / 2f, 0), scale, effects, 0f);
            return false;
        }
        Vector2 offset = GoreShared.Rotate(0, (neck - frameHeight / 2f) * scale, rotation);
        var neckAt = new Vector2(at.X + offset.X, at.Y + offset.Y);
        var headOrigin = new Vector2(width / 2f, neck);
        var bodyOrigin = new Vector2(width / 2f, Overlap);
        if (info.Dismember == CorpsePart.Upper)
        {
            Main.EntitySpriteDraw(texture, neckAt, new Rectangle(0, neck - Overlap, width, waist - neck + Overlap), color, rotation, bodyOrigin, scale, effects, 0f);
            Main.EntitySpriteDraw(texture, neckAt, head, color, info.HeadRotation, headOrigin, scale, effects, 0f);
            return false;
        }
        Main.EntitySpriteDraw(texture, neckAt, new Rectangle(0, neck - Overlap, width, frameHeight - neck + Overlap), color, rotation, bodyOrigin, scale, effects, 0f);
        if (info.Dismember != CorpsePart.Headless)
            Main.EntitySpriteDraw(texture, neckAt, head, color, info.HeadRotation, headOrigin, scale, effects, 0f);
        return false;
    }
}
