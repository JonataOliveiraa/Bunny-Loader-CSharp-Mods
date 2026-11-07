using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using umamusumevanities.Common.Audio;
using umamusumevanities.Common.Effects;
using umamusumevanities.Content.Effects.AgnesTachyonLetters;
using umamusumevanities.Content.Effects.GoldShipAnchor;
using umamusumevanities.Content.Effects.MatikanetannhauserStars;
using umamusumevanities.Content.Effects.RiceShowerPetal;
using umamusumevanities.Content.Effects.RunningTrails;
using umamusumevanities.Content.Effects.SonicBoom;
using umamusumevanities.Content.Effects.SymboliRudolfFlash;
using umamusumevanities.Content.Effects.UmaBurstAura;

namespace umamusumevanities.Common.Players;

public class UmamusumeRunningPlayer : ModPlayer
{
    static readonly string[][] sets =
    {
        new[] { "SymboliRudolfHair", "EmperorsPathUpper", "EmperorsPathLower" },
        new[] { "TokaiTeioHair", "PeakJoyUpper", "PeakJoyLower" },
        new[] { "OguriCapHair", "StarlightBeatUpper", "StarlightBeatLower" },
        new[] { "GoldShipHair", "RedStrifeUpper", "RedStrifeLower" },
        new[] { "RiceShowerHair", "RosyDreamsUpper", "RosyDreamsLower" },
        new[] { "ManhattanCafeHair", "CreepingShadowUpper", "CreepingShadowLower" },
        new[] { "AgnesTachornHair", "TachNologyUpper", "TachNologyLower" },
        new[] { "MatikanetannhauserHair", "ClippetyTippetyClopUpper", "ClippetyTippetyClopLower" },
    };

    static readonly string[] globalSets = { "SchoolUniform", "TrackSuit", "Stage" };

    static readonly Color[] palettes =
    {
        new(79, 136, 204),
        new(255, 255, 255),
        new(245, 245, 255),
        new(255, 60, 60),
        new(156, 146, 230),
        new(255, 218, 88),
        new(255, 218, 88),
        new(255, 255, 255),
        new(190, 214, 255),
    };

    static readonly int[] burstDusts =
    {
        DustID.Electric, DustID.FireworksRGB, DustID.Smoke,
        DustID.RedTorch, DustID.PlanteraPetal, DustID.YellowTorch,
        DustID.Electric, DustID.GemDiamond, DustID.BlueTorch,
    };

    static readonly int[] accentDusts =
    {
        DustID.BlueTorch, DustID.FireworksRGB, DustID.Smoke,
        DustID.RedTorch, DustID.PlanteraPetal, DustID.BlueFairy,
        DustID.YellowTorch, DustID.GemDiamond, DustID.BlueTorch,
    };

    static Dictionary<int, int>? itemBits;

    int runTimer;
    int riceTimer;
    int burstCooldown;
    int burstTimer;
    float previousVx;
    bool previousLeft;
    bool previousRight;
    int leftTap;
    int rightTap;
    int burstGate;
    int colorStep;
    int lettersCadence;

    static float RandRange(float min, float max) => Main.rand.NextFloat(min, max);

    static Dictionary<int, int> GetItemBits()
    {
        if (itemBits != null)
            return itemBits;
        var bits = new Dictionary<int, int>();
        int found = 0;
        void Add(string name, int bit)
        {
            if (!ModContent.TryFind("umamusumevanities", name, out ModItem item))
                return;
            bits.TryGetValue(item.Type, out int value);
            bits[item.Type] = value | (1 << bit);
            found++;
        }
        for (int i = 0; i < sets.Length; i++)
        {
            for (int j = 0; j < 3; j++)
                Add(sets[i][j], i * 3 + j);
        }
        for (int i = 0; i < globalSets.Length; i++)
        {
            Add(globalSets[i] + "Upper", 24 + i * 2);
            Add(globalSets[i] + "Lower", 25 + i * 2);
        }
        if (found == 30)
            itemBits = bits;
        return bits;
    }

    static int GetTheme(Player player)
    {
        Dictionary<int, int> types = GetItemBits();
        int equipped = 0;
        int count = Math.Min(player.armor.Length, 20);
        for (int i = 0; i < count; i++)
        {
            Item item = player.armor[i];
            if (!item.IsNull && item.stack > 0 && types.TryGetValue(item.type, out int bits))
                equipped |= bits;
        }
        for (int i = 0; i < sets.Length; i++)
        {
            int required = 7 << (i * 3);
            if ((equipped & required) == required)
                return i;
        }
        for (int i = 0; i < globalSets.Length; i++)
        {
            int required = 3 << (24 + i * 2);
            if ((equipped & required) == required)
                return 8;
        }
        return -1;
    }

    static Dust SpawnDust(float x, float y, float vx, float vy, Color color, float scale, int type = DustID.TintableDust)
    {
        Dust dust = Dust.NewDustPerfect(new Vector2(x, y), type, new Vector2(vx, vy), 0, color, scale);
        dust.noGravity = true;
        return dust;
    }

    static int NewEffect(Player player, Vector2 position, Vector2 velocity, int type, float ai0, float ai1, float ai2) =>
        Projectile.NewProjectile(player.GetProjectileSource_Item(player.HeldItem), position, velocity, type, 0, 0, player.whoAmI, ai0, ai1, ai2);

    static void SetScale(int index, float scale)
    {
        if (index >= 0 && index < Main.maxProjectiles)
            Main.projectile[index].scale = scale;
    }

    public override void PostUpdate()
    {
        Player player = Player;
        OguriCapAfterimage.Update(player);
        if (!player.active || player.dead)
        {
            runTimer = 0;
            riceTimer = 0;
            burstTimer = 0;
            lettersCadence = 0;
            previousVx = 0;
            previousLeft = false;
            previousRight = false;
            return;
        }

        int theme = GetTheme(player);
        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        float vx = player.velocity.X;
        float speed = Math.Abs(vx);
        float deltaVx = vx - previousVx;
        previousVx = vx;

        if (burstCooldown > 0)
            burstCooldown--;
        if (burstTimer > 0)
        {
            burstTimer--;
            if (theme >= 0 && options.amount > 0 && Main.rand.NextBool(UmamusumeVFXSettings.DensityInterval(3, options.amount)))
                EmitBurstAccent(player, theme);
            if ((theme == 2 || theme == 3) && options.amount > 0 && burstTimer % UmamusumeVFXSettings.DensityInterval(6, options.amount) == 0 && Main.rand.NextBool(2))
                EmitAura(player, theme);
        }

        if (theme >= 0 && options.amount > 0 && !player.frozen && speed >= 2.8f)
        {
            int interval = UmamusumeVFXSettings.DensityInterval(speed >= 5 ? 3 : 5, options.amount);
            if (++runTimer >= interval)
            {
                runTimer = 0;
                EmitTrail(player, theme, speed);
            }
            if (theme == 4 && speed >= 6 && ++riceTimer >= UmamusumeVFXSettings.DensityInterval(9, options.amount))
            {
                riceTimer = 0;
                if (Main.rand.NextBool(3))
                {
                    int count = UmamusumeVFXSettings.DensityCount(Main.rand.Next(1, 3), options.amount, 5);
                    for (int i = 0; i < count; i++)
                        EmitPetal(player);
                }
            }
        }
        else
        {
            runTimer = 0;
            riceTimer = 0;
            lettersCadence = 0;
        }

        if (player.whoAmI != Main.myPlayer)
            return;
        if (leftTap > 0)
            leftTap--;
        if (rightTap > 0)
            rightTap--;
        if (burstGate > 0)
            burstGate--;

        bool left = player.controlLeft;
        bool right = player.controlRight;
        if (left && !previousLeft)
        {
            if (leftTap > 0)
                burstGate = 5;
            leftTap = 20;
        }
        if (right && !previousRight)
        {
            if (rightTap > 0)
                burstGate = 5;
            rightTap = 20;
        }
        previousLeft = left;
        previousRight = right;

        if (theme >= 0 && burstGate > 0 && burstCooldown == 0 && deltaVx * Math.Sign(vx) >= 2)
        {
            burstGate = 0;
            burstCooldown = 24;
            burstTimer = 22;
            EmitBurst(player, theme, options);
        }
    }

    void EmitTrail(Player player, int theme, float speed)
    {
        if (player.whoAmI != Main.myPlayer)
            return;
        int streakType = ModContent.ProjectileType<UmamusumeTrailStreak>();
        int direction = player.velocity.X >= 0 ? 1 : -1;
        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        int count = UmamusumeVFXSettings.DensityCount(speed >= 6 ? 2 : 1, options.amount, 4);
        int free = Math.Max(0, 38 - player.ownedProjectileCounts[streakType]);
        int budget = Math.Min(count, free);
        if (budget == 0)
            return;
        Vector2 center = player.Center;

        for (int i = 0; i < budget; i++)
        {
            int variant = theme == 1 ? colorStep++ % 6 : theme == 4 || theme == 5 ? Main.rand.Next(0, 4) : 0;
            float sx = center.X - direction * (player.width * 0.35f + 3 + i * 6);
            float sy = center.Y + RandRange(-player.height * 0.4f, player.height * 0.4f);
            int index = NewEffect(player, new Vector2(sx, sy), Vector2.Zero, streakType, theme, variant, direction);
            SetScale(index, RandRange(0.7f, 1.12f));
        }

        if (theme == 6 && ++lettersCadence >= UmamusumeVFXSettings.DensityInterval(3, options.amount))
        {
            lettersCadence = 0;
            EmitLetters(player);
        }

        float x = center.X - direction * (player.width * 0.5f + 11);
        if (theme == 0 && Main.rand.NextBool(4))
        {
            SpawnDust(x, center.Y, -direction * 0.18f, -0.12f, new Color(75, 154, 255), 0.8f, DustID.BlueTorch);
        }
        else if (theme == 1 && Main.rand.NextBool(3))
        {
            SpawnDust(x, center.Y + RandRange(-10, 10), 0, -0.12f, Color.White, 0.75f, DustID.FireworksRGB);
        }
        else if (theme == 2 && player.velocity.Y == 0 && Main.rand.NextBool(2))
        {
            Dust smoke = SpawnDust(x, player.Bottom.Y - 5, -direction * 0.16f, -0.45f, Color.White, 1f, DustID.Smoke);
            smoke.noGravity = false;
        }
        else if (theme == 3 && Main.rand.NextBool(4))
        {
            SpawnDust(x, center.Y, 0, -0.2f, new Color(255, 60, 60), 0.8f, DustID.RedTorch);
        }
        else if (theme == 5 && Main.rand.NextBool(3))
        {
            SpawnDust(x, center.Y + RandRange(-12, 12), 0, -0.12f, new Color(128, 205, 255), 0.7f, DustID.BlueFairy);
        }
        else if (theme == 6 && Main.rand.NextBool(3))
        {
            SpawnDust(x, center.Y + RandRange(-10, 10), 0, -0.15f, new Color(255, 215, 80), 0.75f, DustID.YellowTorch);
        }
        else if (theme == 7 && Main.rand.NextBool(3))
        {
            SpawnDust(x, center.Y + RandRange(-12, 12), 0, -0.1f, Color.White, 0.75f, DustID.GemDiamond);
        }
    }

    static void EmitPetal(Player player, bool burst = false, bool whiteAccent = false)
    {
        if (player.whoAmI != Main.myPlayer)
            return;
        int petalType = ModContent.ProjectileType<RiceShowerPetal>();
        VFXOptions options = UmamusumeVFXSettings.GetVFX();
        if (options.amount <= 0 || player.ownedProjectileCounts[petalType] >= Math.Min(40, UmamusumeVFXSettings.DensityCount(26, options.amount, 40)))
            return;
        int sign = Math.Sign(player.velocity.X);
        int direction = sign != 0 ? sign : player.direction;
        float x = burst ? RandRange(-player.width * 0.55f, player.width * 0.55f) : -direction * RandRange(4, 14);
        float y = RandRange(-player.height * 0.5f, player.height * 0.5f);
        float vx = -direction * RandRange(0.4f, 1.2f) + player.velocity.X * RandRange(0.2f, 0.5f);
        float vy = RandRange(-1.2f, -0.4f) + player.velocity.Y * 0.3f;
        bool white = burst ? whiteAccent : Main.rand.NextBool(6);
        Vector2 center = player.Center;
        int index = NewEffect(player, new Vector2(center.X + x, center.Y + y), new Vector2(vx, vy), petalType, white ? 1 : 0, Main.rand.Next(0, 10000), 0);
        SetScale(index, RandRange(0.9f, 1.4f));
    }

    void EmitBurst(Player player, int theme, VFXOptions options)
    {
        Color color = palettes[theme];
        Vector2 center = player.Center;
        int sparkCount = UmamusumeVFXSettings.DensityCount(7, options.amount, 15);
        for (int i = 0; i < sparkCount; i++)
        {
            float angle = i * MathF.PI * 2 / Math.Max(1, sparkCount);
            float speed = RandRange(1.5f, 3.2f);
            Dust dust = SpawnDust(center.X + MathF.Cos(angle) * 7, center.Y + MathF.Sin(angle) * 7,
                MathF.Cos(angle) * speed + player.velocity.X * 0.15f, MathF.Sin(angle) * speed, color, RandRange(0.7f, 1.2f), burstDusts[theme]);
            if (theme == 2)
                dust.noGravity = false;
        }
        if (theme == 3)
            SpawnDust(center.X, center.Y - 9, 0, -0.8f, new Color(165, 99, 229), 1.2f, DustID.PurpleTorch);
        if (theme == 0)
            SpawnDust(center.X, center.Y, 0, 0, new Color(90, 160, 245), 1.2f, DustID.Electric);

        if (theme == 7 && options.amount > 0)
            EmitStars(player);
        if (theme == 0 && options.amount > 0)
            EmitRudolfFlash(player);
        if (theme == 4)
        {
            int petalCount = UmamusumeVFXSettings.DensityCount(13, options.amount, 22);
            int whites = Math.Max(1, (int)Math.Floor(petalCount * 3 / 13.0 + 0.5));
            for (int i = 0; i < petalCount; i++)
                EmitPetal(player, true, i < whites);
        }
        if (theme == 3 && options.amount > 0)
            EmitAnchor(player);
        if (theme == 2 && options.amount > 0)
            OguriCapAfterimage.Activate(player);
        if ((theme == 2 || theme == 3) && options.amount > 0)
            EmitAura(player, theme);

        if (options.amount > 0)
            NewEffect(player, new Vector2(center.X, center.Y - 3), new Vector2(player.velocity.X, 0), ModContent.ProjectileType<UmamusumeSonicBoom>(), 0, 0, 0);
        UmamusumeAudio.PlaySonicBoom(center);
    }

    static void EmitRudolfFlash(Player player)
    {
        int flashType = ModContent.ProjectileType<SymboliRudolfFlash>();
        if (player.ownedProjectileCounts[flashType] >= 1)
            return;
        NewEffect(player, player.Center, Vector2.Zero, flashType, 0, 0, 0);
    }

    static void EmitAnchor(Player player)
    {
        int anchorType = ModContent.ProjectileType<GoldShipAnchor>();
        if (player.ownedProjectileCounts[anchorType] >= 3)
            return;
        NewEffect(player, player.Center, player.velocity * 1.5f, anchorType, 0, 0, 0);
    }

    static void EmitStars(Player player)
    {
        int starsType = ModContent.ProjectileType<MatikanetannhauserStars>();
        if (player.ownedProjectileCounts[starsType] >= 7)
            return;
        int count = UmamusumeVFXSettings.DensityCount(Main.rand.Next(5, 8), UmamusumeVFXSettings.GetVFX().amount, 9);
        for (int i = 0; i < count; i++)
        {
            float angle = (i + RandRange(-0.15f, 0.15f)) * MathF.PI * 2 / count;
            int direction = Main.rand.NextBool(2) ? 1 : -1;
            float angularSpeed = direction * RandRange(0.09f, 0.14f);
            int index = NewEffect(player, player.Center, Vector2.Zero, starsType, angle, angularSpeed, 0);
            SetScale(index, RandRange(0.8f, 1.05f));
        }
    }

    static void EmitLetters(Player player)
    {
        if (player.whoAmI != Main.myPlayer)
            return;
        int lettersType = ModContent.ProjectileType<AgnesTachyonLetters>();
        VFXOptions opts = UmamusumeVFXSettings.GetVFX();
        if (opts.amount <= 0 || player.ownedProjectileCounts[lettersType] >= Math.Min(12, UmamusumeVFXSettings.DensityCount(7, opts.amount, 12)))
            return;
        int direction = player.velocity.X >= 0 ? 1 : -1;
        float y = RandRange(player.position.Y, player.position.Y + player.height);
        NewEffect(player, new Vector2(player.Center.X, y), Vector2.Zero, lettersType, direction, Main.rand.Next(3, 15), Main.rand.Next(0, 10000));
    }

    static void EmitAura(Player player, int theme)
    {
        int auraType = ModContent.ProjectileType<UmaBurstAura>();
        VFXOptions opts = UmamusumeVFXSettings.GetVFX();
        if (opts.amount <= 0 || player.ownedProjectileCounts[auraType] >= Math.Min(5, UmamusumeVFXSettings.DensityCount(3, opts.amount, 5)))
            return;
        float offsetX = RandRange(-player.width * 0.5f, player.width * 0.5f);
        float offsetY = RandRange(-player.height * 0.5f, player.height * 0.1f);
        int index = NewEffect(player, player.Center, Vector2.Zero, auraType, theme, offsetX, offsetY);
        SetScale(index, RandRange(0.55f, 0.85f));
    }

    static void EmitBurstAccent(Player player, int theme)
    {
        if (theme == 4)
            return;
        Vector2 center = player.Center;
        float x = center.X + RandRange(-player.width * 0.5f, player.width * 0.5f);
        float y = center.Y + RandRange(-player.height * 0.4f, player.height * 0.4f);
        Dust dust = SpawnDust(x, y, RandRange(-0.6f, 0.6f), RandRange(-1, 0), palettes[theme], 0.65f, accentDusts[theme]);
        if (theme == 2)
            dust.noGravity = false;
    }
}
