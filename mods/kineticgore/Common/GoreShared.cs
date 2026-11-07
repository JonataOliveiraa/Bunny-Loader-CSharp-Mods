using System;
using System.Collections.Generic;
using kineticgore.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace kineticgore.Common;

public struct SwingHit
{
    public Vector2 Center;
    public int Direction;
    public float Range;
}

public struct MusicianHit
{
    public Vector2 Center;
    public int Direction;
}

public struct ProjectileHit
{
    public int Index;
    public int Type;
    public float X;
    public float Y;
    public int W;
    public int H;
    public float VelocityX;
    public float VelocityY;
    public float Rotation;
    public bool Arrow;
    public int Owner;
    public bool Spear;
}

// What is shared by the blood, the corpses and the NPCs.
public static class GoreShared
{
    public const int Blood = 5;
    const int TicksPerSecond = 60;

    public static KineticGoreConfig Config => KineticGoreConfig.Instance;

    public static bool Visual => Main.netMode != NetmodeID.Server;

    // Math.round of the JavaScript: halves go up, MathF.Round would send them to the even number.
    public static int JsRound(double value) => (int)Math.Floor(value + 0.5);

    public static int Ticks(float seconds) => JsRound(seconds * TicksPerSecond);

    public static float Rand(float min, float max) => min + Random.Shared.NextSingle() * (max - min);

    public static float Chance() => Random.Shared.NextSingle();

    public static Vector2 Circular(float rx, float ry)
    {
        float angle = Random.Shared.NextSingle() * MathF.PI * 2;
        float r = MathF.Sqrt(Random.Shared.NextSingle());
        return new Vector2(MathF.Cos(angle) * rx * r, MathF.Sin(angle) * ry * r);
    }

    public static Vector2 Rotate(float x, float y, float angle)
    {
        float c = MathF.Cos(angle), s = MathF.Sin(angle);
        return new Vector2(x * c - y * s, x * s + y * c);
    }

    // MathF.Sign throws on NaN.
    public static float Sign(float value) => value > 0 ? 1 : value < 0 ? -1 : 0;

    public static void PlayDeathSound(Vector2 position) => SoundEngine.PlaySound(SoundID.NPCDeath1, position, 0f, 1f);

    public static void PlayDigSound(Vector2 position) => SoundEngine.PlaySound(SoundID.Dig, position, 1, 0f);

    public static bool IsSpider(int type) => type is 164 or 165 or 239 or 240 or 163 or 238 or 236 or 237 or 513;

    public static bool IsHumanoid(int aiStyle, bool townNPC, int type) => (aiStyle == 3 || townNPC) && !IsSpider(type);

    public static bool IsSlime(int aiStyle, int type) => aiStyle == 1 || type == 1 || type == -3;

    public static bool ShouldIgnore(NPC npc)
    {
        if (npc.boss || npc.lifeMax < 5)
            return true;
        var bossLike = NPCID.Sets.ShouldBeCountedAsBossForBestiary;
        if ((uint)npc.type < (uint)bossLike.Length && bossLike[npc.type])
            return true;
        return npc.friendly && !npc.townNPC;
    }

    // A game image the phone does not have throws when it loads.
    public static bool TryNpcTexture(int type, out Texture2D texture)
    {
        try
        {
            texture = TextureAssets.Npc[type].Value;
            return !texture.IsNull;
        }
        catch (Exception)
        {
            texture = default;
            return false;
        }
    }

    public static bool TryProjectileTexture(int type, out Texture2D texture)
    {
        try
        {
            texture = TextureAssets.Projectile[type].Value;
            return !texture.IsNull;
        }
        catch (Exception)
        {
            texture = default;
            return false;
        }
    }

    // Sticky blood: each drop flies for up to Flying ticks, then stays Stuck until it fades.
    struct BloodDrop
    {
        public Dust Dust;
        public int T;
    }

    const int MaxBlood = 700;
    const int Flying = 150;
    const int Stuck = 10000;
    static readonly List<BloodDrop> blood = new(MaxBlood);

    public static Dust SpawnBlood(Vector2 position, Vector2 velocity, int alpha = 0, float scale = 1)
    {
        Dust dust = Dust.NewDustPerfect(position, Blood, velocity, alpha, Color.Transparent, scale);
        if (!dust.IsNull)
            Track(dust);
        return dust;
    }

    public static Dust SpawnBloodIn(Vector2 position, int width, int height, float speedX, float speedY, int alpha, float scale)
    {
        Dust dust = Dust.NewDustDirect(position, width, height, Blood, speedX, speedY, alpha, Color.Transparent, scale);
        if (!dust.IsNull)
            Track(dust);
        return dust;
    }

    static void Track(Dust dust)
    {
        if (blood.Count >= MaxBlood)
            blood.RemoveAt(0);
        blood.Add(new BloodDrop { Dust = dust, T = Flying });
    }

    public static void UpdateBlood()
    {
        KineticGoreConfig config = Config;
        if (!config.EnableStickyBlood)
        {
            blood.Clear();
            return;
        }
        int duration = Math.Max(1, Ticks(config.StickyBloodDuration));
        float scale = config.BloodScale;
        int write = 0;
        for (int read = 0; read < blood.Count; read++)
        {
            BloodDrop entry = blood[read];
            Dust dust = entry.Dust;
            if (!dust.active || dust.type != Blood)
                continue;
            if (entry.T <= Flying)
            {
                entry.T--;
                dust.alpha = 0;
                dust.scale = scale;
                if (entry.T <= 0)
                {
                    Dust.SetNotActive(dust);
                    continue;
                }
                bool landed = Collision.SolidCollision(dust.position, 2, 2);
                if (!landed && MathF.Abs(dust.velocity.Y) < 0.1f)
                {
                    float vx = dust.velocity.X;
                    dust.velocity.X = vx * 0.5f;
                    if (MathF.Abs(vx * 0.5f) < 0.2f && Collision.SolidCollision(new Vector2(dust.position.X, dust.position.Y + 4), 2, 2))
                        landed = true;
                }
                if (landed)
                {
                    entry.T = Stuck + duration;
                    dust.velocity = Vector2.Zero;
                    dust.noGravity = true;
                }
            }
            else
            {
                dust.velocity = Vector2.Zero;
                dust.noGravity = true;
                dust.rotation = 0;
                entry.T--;
                int left = entry.T - Stuck;
                if (left <= 0)
                {
                    Dust.SetNotActive(dust);
                    continue;
                }
                dust.alpha = JsRound(255 * (1 - (double)left / duration));
                dust.scale = scale;
            }
            blood[write++] = entry;
        }
        blood.RemoveRange(write, blood.Count - write);
    }

    public static void ClearBlood() => blood.Clear();

    public static readonly List<SwingHit> Swings = new();
    public static readonly List<ProjectileHit> Projectiles = new();
    public static readonly List<MusicianHit> Musicians = new();

    static bool IsMusic(int type) => type is 494 or 1305 or 4372 or 508;

    public static void CollectHitters(int corpseType, int arrowType, bool wantProjectiles)
    {
        Swings.Clear();
        Projectiles.Clear();
        Musicians.Clear();
        for (int i = 0; i < 255; i++)
        {
            Player player = Main.player[i];
            if (player.IsNull || !player.active || player.dead || player.itemAnimation <= 0)
                continue;
            Item item = player.HeldItem;
            if (item.IsNull)
                continue;
            if (IsMusic(item.type))
                Musicians.Add(new MusicianHit { Center = player.Center, Direction = player.direction });
            if (item.damage <= 0 && item.pick <= 0 && item.axe <= 0 && item.hammer <= 0)
                continue;
            float scale = player.GetAdjustedItemScale(item);
            if (scale == 0)
                scale = 1;
            Swings.Add(new SwingHit { Center = player.Center, Direction = player.direction, Range = 100 * scale });
        }
        if (!wantProjectiles)
            return;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile proj = Main.projectile[i];
            if (!proj.active || !proj.friendly || proj.damage <= 0)
                continue;
            int type = proj.type;
            if (type == corpseType || type == arrowType)
                continue;
            Projectiles.Add(new ProjectileHit
            {
                Index = i,
                Type = type,
                X = proj.position.X,
                Y = proj.position.Y,
                W = proj.width,
                H = proj.height,
                VelocityX = proj.velocity.X,
                VelocityY = proj.velocity.Y,
                Rotation = proj.rotation,
                Arrow = proj.arrow,
                Owner = proj.owner,
                Spear = proj.aiStyle == 19,
            });
        }
    }

    // By projectile index. Nothing goes over the network: each device simulates its own bodies.
    public static readonly Dictionary<int, CorpseInfo> Corpses = new();

    static int nextToken = 1;

    public static int NewToken() => nextToken = nextToken % 1000000 + 1;

    public static CorpseInfo? InfoOf(Projectile proj)
    {
        return Corpses.TryGetValue(proj.whoAmI, out CorpseInfo? info) && info.Token == proj.ai[0] ? info : null;
    }
}
