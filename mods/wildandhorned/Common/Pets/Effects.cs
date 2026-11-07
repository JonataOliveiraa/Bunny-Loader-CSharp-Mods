// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using static WildAndHornedMod.Common.Pets.Runtime;

namespace WildAndHornedMod.Common.Pets;

public static class Effects
{
    public static void Sound(PetBase self, LegacySoundStyle style, float pitch, float volume)
    {
        if (Main.dedServ)
            return;
        SoundEngine.PlaySound(style, self.Projectile.Center, pitch, volume);
    }

    public static void Sound(PetBase self, int id, float pitch, float volume)
    {
        if (Main.dedServ)
            return;
        Vector2 center = self.Projectile.Center;
        SoundEngine.PlaySound(id, (int)center.X, (int)center.Y, 1, volume, pitch);
    }

    static float Rand(float low, float high) => Main.rand.NextFloat(low, high);

    static void Mote(PetBase self, float x, float y, int type, float vx = 0, float vy = -0.4f, float scale = 0.8f, bool gravity = false, int alpha = 120, Color? rgb = null)
    {
        Vector2 center = self.Projectile.Center;
        Dust dust = Dust.NewDustPerfect(new Vector2(center.X + x, center.Y + y), type, new Vector2(vx, vy), alpha, rgb ?? default, scale);
        dust.noGravity = !gravity;
    }

    static void Sparkle(PetBase self, float x, float y, int type, float scale = 0.8f) => Mote(self, x, y, type, 0, -0.3f, scale);

    public static void SmokeBurst(PetBase self, int count = 24, float scale = 1.4f, Color? rgb = null)
    {
        if (Main.dedServ)
            return;
        Color smoke = rgb ?? new Color(60, 44, 80);
        for (int i = 0; i < count; i++)
        {
            Mote(self, Rand(-25, 25), Rand(-25, 25), DustID.Smoke, Rand(-1.6f, 1.6f), Rand(-1.6f, 1.6f), scale, false, 120, smoke);
        }
    }

    static bool HostileNear(PetBase self)
    {
        Vector2 center = self.Projectile.Center;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC n = Main.npc[i];
            if (n.active && !n.friendly && !n.townNPC && n.lifeMax > 5 && n.damage > 0 && !n.CountsAsACritter &&
                Vector2.DistanceSquared(n.Center, center) < 480f * 480f)
                return true;
        }
        return false;
    }

    static bool TreasureNear(PetBase self)
    {
        Vector2 center = self.Projectile.Center;
        int cx = (int)(center.X / 16), cy = (int)(center.Y / 16);
        for (int x = cx - 15; x <= cx + 15; x++)
        {
            if (x < 1 || x >= Main.maxTilesX - 1)
                continue;
            for (int y = cy - 15; y <= cy + 15; y++)
            {
                if (y < 1 || y >= Main.maxTilesY - 1)
                    continue;
                Tile tile = Main.tile[x, y];
                if (tile.active() && Main.tileSpelunker[tile.type])
                    return true;
            }
        }
        return false;
    }

    public static void UpdateEffects(PetBase self)
    {
        Projectile p = self.Projectile;
        int frame = p.frame, previous = self.lastFrame;
        bool changed = frame != previous;
        int facing = p.spriteDirection != 0 ? p.spriteDirection : -1;
        self.lastFrame = frame;
        string name = self.Definition.Name;
        if (name == "Aunn")
        {
            if (Tick() % 30 == 0)
                self.enemyNear = HostileNear(self);
            self.alert = (int)Clamp(self.alert + (self.enemyNear ? 1 : -1), 0, 30);
        }
        else if (name == "Kosuzu")
        {
            self.aura = (int)Clamp(self.aura + (self.PetState > 2 && frame >= 1 && frame <= 5 ? 1 : -1), 0, 40);
        }
        else if (name == "Nazrin")
        {
            if ((Tick() + (uint)p.whoAmI) % 60 == 0)
                self.treasureNear = TreasureNear(self);
            self.glow = (int)Clamp(self.glow + (self.treasureNear ? 1 : -1), 0, 30);
        }
        if (Main.dedServ)
            return;
        switch (name)
        {
            case "Aunn":
                if (Chance(self.enemyNear ? 12 : 80))
                    Mote(self, Rand(-25, 25), Rand(-25, 25), DustID.GemEmerald, 0, -0.24f, 0.7f, false, 150);
                break;
            case "Okina":
                if (Chance(40))
                    Sparkle(self, 22 * facing + Rand(-6, 6), -16 + Rand(-6, 6), DustID.YellowStarDust, 0.9f);
                if (frame == 2 && Chance(6))
                    Sparkle(self, -19 * facing + Rand(-6, 6), 5 + Rand(-6, 6), DustID.YellowStarDust, 1.1f);
                break;
            case "Kosuzu":
                if (self.PetState > 2 && frame >= 1 && frame <= 5 && Chance(10))
                    Mote(self, 3 * facing + Rand(-5, 5), 16 + Rand(-3, 3), DustID.PurpleTorch, 0, -0.6f);
                break;
            case "Mamizou":
                var mamizouSmoke = new Color(232, 232, 242);
                if ((frame == 4 || frame == 5) && Chance(10))
                    Mote(self, 22 * facing, 2, DustID.Smoke, 0.15f * facing, -0.7f, 0.7f, false, 120, mamizouSmoke);
                if ((frame == 6 || frame == 7) && Chance(5))
                    Mote(self, 14 * facing, 6, DustID.Smoke, 0.5f * facing, -0.5f, 0.9f, false, 120, mamizouSmoke);
                if (changed && frame == 13)
                    Mote(self, 23 * facing, 18, DustID.Smoke, 0.3f * facing, -1, 1, false, 120, mamizouSmoke);
                if (changed && frame == 11)
                {
                    Sound(self, SoundID.Item8, 0.2f, 0.45f);
                    SmokeBurst(self, 18, 1.3f, new Color(235, 235, 245));
                }
                break;
            case "Nazrin":
                if ((frame == 3 || frame == 4) && Chance(4))
                    Sparkle(self, 17 * facing + Rand(-5, 5), 18 + Rand(-5, 5), DustID.GoldFlame, 0.9f);
                if (frame >= 5 && frame <= 7 && Chance(12))
                    Mote(self, (24 - (2 * (frame - 4) + 1)) * facing, 23, DustID.GemSapphire, 0, 0.3f, 0.6f);
                if (Chance(self.treasureNear ? 14 : 90))
                    Sparkle(self, 2 * facing, 18, self.treasureNear ? DustID.GoldFlame : DustID.GemSapphire, 0.6f);
                break;
            case "Suwako":
                if (frame == 1 && previous == 2)
                {
                    for (int i = 0; i < 6; i++)
                        Mote(self, 2 * facing + Rand(-8, 8), 30 + Rand(-2, 2), DustID.Dirt, Rand(-1, 1), -1.2f, 0.9f, true, 100);
                }
                if (frame == 6 && Chance(3))
                    Sparkle(self, 21 * facing + Rand(-3, 3), 10 + Rand(-6, 6), DustID.Silver, 0.7f);
                if (Chance(120))
                    Sparkle(self, Rand(-28, 28), Rand(-22, 22), DustID.GreenTorch);
                break;
            case "Kanako":
                if (frame == 3 && Chance(3))
                    Sparkle(self, 23 * facing + Rand(-4, 4), Rand(-4, 4), DustID.GoldFlame, 0.9f);
                if (changed && frame == 6)
                {
                    Sound(self, SoundID.Dig, -0.3f, 0.5f);
                    for (int i = 0; i < 8; i++)
                        Mote(self, -19 * facing + Rand(-6, 6), 30 + Rand(-2, 2), DustID.Dirt, Rand(-1.2f, 1.2f), -1.4f, 1, true, 80);
                }
                break;
            case "InuSakuya":
                if (Chance(100))
                    Sparkle(self, Rand(-18, 18), Rand(-18, 18), DustID.GemDiamond, 0.6f);
                break;
            case "Shion":
                if ((frame == 5 || frame == 6) && Chance(frame == 6 ? 2 : 4))
                    Mote(self, (Chance(2) ? 20 : -20) * facing + Rand(-4, 4), 24 + Rand(-10, 10), DustID.BlueTorch, 0, -1.2f, frame == 6 ? 1.3f : 0.9f, false, 100);
                if (changed && frame == 7)
                    Mote(self, 3 * facing, 7, DustID.Smoke, -0.4f * facing, -0.5f, 0.8f, false, 160);
                break;
            case "Joon":
                if ((frame == 2 || frame == 3) && Chance(6))
                    Sparkle(self, 19 * facing + Rand(-6, 6), 3 + Rand(-6, 6), DustID.GoldFlame, 0.7f);
                if (changed && frame == 6)
                    Sparkle(self, -7 * facing, 2, DustID.GemDiamond, 0.9f);
                if (changed && frame == 8)
                    Sparkle(self, -13 * facing, -16, DustID.GoldCoin, 0.9f);
                if (Chance(90))
                    Sparkle(self, Rand(-18, 18), Rand(-22, 22), DustID.GoldCoin, 0.6f);
                break;
            case "Momiji":
                if (changed && frame == 6)
                {
                    Sound(self, SoundID.MenuTick, -0.4f, 0.6f);
                    for (int i = 0; i < 3; i++)
                        Sparkle(self, 23 * facing + Rand(-3, 3), 16 + Rand(-3, 3), DustID.GoldFlame, 0.6f);
                }
                if (changed && frame == 7)
                    Sparkle(self, 15 * facing, -16, DustID.SilverCoin, 0.9f);
                break;
        }
    }

    public static void AddPetLight(PetBase self)
    {
        if (!self.Definition.Light || Main.dedServ)
            return;
        (float r, float g, float b) = Logic.LightColor(self.Definition.Name, Tick(), self.alert, self.aura, self.glow, Main.dayTime);
        Lighting.AddLight(self.Projectile.Center, r, g, b);
    }
}
