using System;
using System.Collections.Generic;
using kineticgore.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace kineticgore.Common;

public class RealisticDeathGlobalNPC : GlobalNPC
{
    const float Neck = 0.38f;
    const int Overlap = 2;
    const float MusicRange = 600;
    const int NewGoreTime = 599;

    public override bool InstancePerEntity => true;

    public float headRotation;
    public float headVelocity;
    public float squash = 1;

    // Used by Gibs, which never runs inside itself: its HitEffect call returns on life -999.
    static bool[] goreWasActive = Array.Empty<bool>();
    static readonly List<(Gore Gore, float Area)> fresh = new();

    public override void PostAI(NPC npc)
    {
        if (!GoreShared.Visual || GoreShared.ShouldIgnore(npc))
            return;
        KineticGoreConfig config = GoreShared.Config;
        Vector2 velocity = npc.velocity;
        if (GoreShared.IsHumanoid(npc.aiStyle, npc.townNPC, npc.type))
        {
            float bob = config.HeadBobIntensity;
            float time = Main.GameUpdateCount;
            float target = npc.rotation - velocity.X * 0.1f;
            if (MathF.Abs(velocity.X) > 0.1f)
                target += MathF.Sin(time * 0.35f) * (velocity.X * 0.12f * bob);
            if (config.EnableHeadBanging && NearestMusician(npc, out MusicianHit musician))
            {
                npc.spriteDirection = musician.Center.X > npc.Center.X ? 1 : -1;
                target += MathF.Sin(time * 0.2f) * (0.6f * bob) * -npc.spriteDirection;
            }
            headVelocity += (target - headRotation) * config.HeadSpringStiffness;
            headVelocity *= config.HeadSpringDamping;
            headRotation = Math.Clamp(headRotation + headVelocity, -0.8f, 0.8f);
        }
        if (GoreShared.IsSlime(npc.aiStyle, npc.type))
        {
            float jiggle = config.SlimeJiggleIntensity;
            float stretch = MathF.Min(1 + MathF.Abs(velocity.Y) * 0.04f * jiggle, 1 + 0.3f * jiggle);
            squash += (stretch - squash) * 0.1f;
            if (velocity.Y == 0 && squash > 1.1f)
                squash = 0.7f;
            squash += (1 - squash) * 0.05f;
        }
    }

    static bool NearestMusician(NPC npc, out MusicianHit best)
    {
        Vector2 center = npc.Center;
        best = default;
        bool found = false;
        float bestDistance = MusicRange;
        foreach (MusicianHit musician in GoreShared.Musicians)
        {
            float distance = Vector2.Distance(musician.Center, center);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = musician;
                found = true;
            }
        }
        return found;
    }

    public override void HitEffect(NPC npc, NPC.HitInfo hit)
    {
        if (npc.life == -999 || !GoreShared.Visual)
            return;
        KineticGoreConfig config = GoreShared.Config;
        int direction = hit.HitDirection != 0 ? hit.HitDirection : 1;
        int damage = hit.Damage;
        bool ignore = GoreShared.ShouldIgnore(npc);

        if (GoreShared.IsHumanoid(npc.aiStyle, npc.townNPC, npc.type) && config.EnableHeadFlinching)
        {
            float flinch = config.ReverseHeadFlinch ? -0.4f : 0.4f;
            headVelocity += Math.Clamp(direction * flinch * config.HeadBobIntensity, -0.5f, 0.5f);
        }

        float steps = MathF.Floor(MathF.Max(5, damage) / 2.5f);
        int count = (int)MathF.Floor(Math.Clamp(steps, 4, 50) * config.BloodAmountMultiplier);
        Vector2 center = npc.Center;
        var origin = new Vector2(center.X + npc.width / 2f * direction, center.Y + GoreShared.Rand(-npc.height / 4f, npc.height / 4f));
        for (int i = 0; i < count; i++)
        {
            var speed = new Vector2(-direction * GoreShared.Rand(2, 6) * config.BloodVelocity, GoreShared.Rand(-4, 4) * config.BloodVelocity);
            GoreShared.SpawnBlood(origin, speed, 100, config.BloodScale);
        }

        if (npc.life > 0)
        {
            if (!ignore && config.GibAmountMultiplier > 0 && GoreShared.Chance() < 0.3f)
                Gibs(npc, direction, damage);
            return;
        }
        if (ignore)
            return;
        RemoveDeathGores(npc);
        SpawnCorpses(npc);
    }

    // Makes the game run the NPC's death gores, then keeps only a few small ones as gibs.
    static void Gibs(NPC npc, int direction, int damage)
    {
        KineticGoreConfig config = GoreShared.Config;
        int total = Main.gore.Length;
        if (goreWasActive.Length < total)
            goreWasActive = new bool[total];
        for (int i = 0; i < total; i++)
            goreWasActive[i] = Main.gore[i].active;
        int life = npc.life;
        npc.life = -999;
        try
        {
            npc.HitEffect(direction, damage != 0 ? damage : 10);
        }
        finally
        {
            npc.life = life;
        }

        // Smallest first, and in order within the same size.
        fresh.Clear();
        for (int i = 0; i < total; i++)
        {
            if (goreWasActive[i])
                continue;
            Gore gore = Main.gore[i];
            if (!gore.active || gore.timeLeft < NewGoreTime)
                continue;
            if (gore.Width >= 20 && gore.Height >= 20)
            {
                gore.active = false;
                continue;
            }
            float area = gore.Width * gore.Height;
            int at = fresh.Count;
            while (at > 0 && fresh[at - 1].Area > area)
                at--;
            fresh.Insert(at, (gore, area));
        }

        int keep = (int)MathF.Floor((1 + Random.Shared.Next(3)) * config.GibAmountMultiplier);
        if (keep < 1 && config.GibAmountMultiplier > 0.01f && GoreShared.Chance() < 0.5f)
            keep = 1;
        for (int i = 0; i < fresh.Count; i++)
        {
            Gore gore = fresh[i].Gore;
            if (i < keep)
            {
                Vector2 jitter = GoreShared.Circular(1.5f, 1.5f);
                gore.velocity = new Vector2(direction * 3 + jitter.X, -3 + jitter.Y);
            }
            else
            {
                gore.active = false;
            }
        }
        fresh.Clear();
    }

    static void RemoveDeathGores(NPC npc)
    {
        Vector2 position = npc.position;
        float range = MathF.Max(npc.width, npc.height) * 1.5f;
        int total = Main.gore.Length;
        for (int i = 0; i < total; i++)
        {
            Gore gore = Main.gore[i];
            if (!gore.active || gore.timeLeft < NewGoreTime)
                continue;
            if (Vector2.Distance(gore.position, position) < range)
                gore.active = false;
        }
    }

    static void SpawnCorpses(NPC npc)
    {
        KineticGoreConfig config = GoreShared.Config;
        CorpseBody body = PhysicsCorpse.FromNPC(npc);
        Vector2 velocity = npc.velocity;
        float knockback = config.KnockbackMultiplier;
        int dismember = 0;
        if (body.Humanoid && GoreShared.Chance() * 100 < config.DismembermentChance)
        {
            dismember = CorpsePart.Headless;
            Vector2 kick = GoreShared.Circular(3, 3);
            PhysicsCorpse.SpawnCorpse(body, npc.Center, new Vector2(velocity.X * knockback + kick.X, velocity.Y * knockback + kick.Y), CorpsePart.Head);
            int count = (int)MathF.Floor(15 * config.BloodAmountMultiplier);
            for (int i = 0; i < count; i++)
            {
                Vector2 v = GoreShared.Circular(4, 4);
                var speed = new Vector2((velocity.X * 0.5f + v.X) * config.BloodVelocity, (velocity.Y * 0.5f + v.Y) * config.BloodVelocity);
                GoreShared.SpawnBlood(npc.Center, speed, 0, config.BloodScale);
            }
        }
        var launch = new Vector2(velocity.X * knockback, velocity.Y * knockback);
        if (launch.Length() < 2)
            launch = new Vector2(npc.direction * -2, -2.5f);
        PhysicsCorpse.SpawnCorpse(body, npc.Center, launch, dismember);
    }

    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        if (!GoreShared.Visual || GoreShared.ShouldIgnore(npc) || !projectile.arrow || !GoreShared.Config.ArrowsStickToEnemies)
            return;
        StuckArrow.SpawnStuckArrow(StuckTarget.OfNPC(npc.whoAmI), new ProjectileHit
        {
            X = projectile.position.X,
            Y = projectile.position.Y,
            W = projectile.width,
            H = projectile.height,
            VelocityX = projectile.velocity.X,
            VelocityY = projectile.velocity.Y,
            Rotation = projectile.rotation,
            Type = projectile.type,
        });
    }

    public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (GoreShared.ShouldIgnore(npc))
            return true;
        bool humanoid = GoreShared.IsHumanoid(npc.aiStyle, npc.townNPC, npc.type);
        bool slime = !humanoid && GoreShared.IsSlime(npc.aiStyle, npc.type);
        if (!humanoid && !slime)
            return true;
        if (npc.townNPC && npc.townNpcVariationIndex != 0)
            return true;
        if (!GoreShared.TryNpcTexture(npc.type, out Texture2D texture))
            return true;

        Rectangle frame = npc.frame;
        Vector2 center = npc.Center;
        float atX = center.X - screenPos.X;
        float atY = center.Y - screenPos.Y + npc.gfxOffY;
        SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Color color = drawColor;
        if (slime)
        {
            Color tint = npc.color;
            if (tint.R != 0 || tint.G != 0 || tint.B != 0 || tint.A != 0)
                color = Utils.MultiplyRGBA(tint, color);
            float lift = frame.Height * squash - frame.Height;
            Main.EntitySpriteDraw(texture, new Vector2(atX, atY - lift * 0.5f), frame, color, npc.rotation, new Vector2(frame.Width / 2f, frame.Height / 2f),
                new Vector2(npc.scale / squash, npc.scale * squash), effects, 0f);
            return false;
        }
        color = npc.GetAlpha(color);
        if (npc.townNPC)
            atY += GoreShared.Config.HumanoidVerticalOffset;
        int height = frame.Height;
        int neck = (int)MathF.Floor(height * Neck);
        var head = new Rectangle(frame.X, frame.Y, frame.Width, neck + Overlap);
        var body = new Rectangle(frame.X, frame.Y + neck - Overlap, frame.Width, height - neck + Overlap);
        float scale = npc.scale;
        Main.EntitySpriteDraw(texture, new Vector2(atX, atY), body, color, npc.rotation, new Vector2(frame.Width / 2f, height / 2f - (neck - Overlap)), scale, effects, 0f);
        Vector2 offset = GoreShared.Rotate(0, (neck - height / 2f) * scale, npc.rotation);
        Main.EntitySpriteDraw(texture, new Vector2(atX + offset.X, atY + offset.Y), head, color, headRotation, new Vector2(frame.Width / 2f, neck), scale, effects, 0f);
        return false;
    }
}
