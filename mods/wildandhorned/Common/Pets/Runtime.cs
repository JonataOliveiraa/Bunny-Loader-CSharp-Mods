// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
// Movement and sprite layout adapted from Touhou Little Friend, MineGame223 / For One.
using System;
using System.Collections.Generic;
using System.Linq;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace WildAndHornedMod.Common.Pets;

public static class Runtime
{
    public const string PREFIX = "Mods.wildandhorned.";

    public static readonly string[] replyKeys = PETS.All.SelectMany(d => d.Companions.Select(c => d.Name + c.Line)).ToArray();

    static readonly Dictionary<string, int> companionTypes = new();

    public static bool Chance(int denominator) => Main.rand.NextBool(Math.Max(1, denominator));

    public static uint Tick() => Main.GameUpdateCount;

    public static float Clamp(float n, float low, float high) => Math.Max(low, Math.Min(high, n));

    // A pet of this name in this mod, or 0: many companions belong to mods that are not here.
    static int CompanionType(string name)
    {
        if (!companionTypes.TryGetValue(name, out int type))
        {
            type = ModContent.TryFind("wildandhorned/" + name, out ModProjectile found) ? found.Type : 0;
            companionTypes[name] = type;
        }
        return type;
    }

    public static PetBase? Companion(PetBase self, string name, string? form = null)
    {
        int type = CompanionType(name);
        if (type == 0)
            return null;
        Projectile me = self.Projectile;
        Vector2 center = me.Center;
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile p = Main.projectile[i];
            if (!p.active || p.type != type || p.owner != me.owner || Vector2.DistanceSquared(p.Center, center) >= 1200f * 1200f)
                continue;
            if (p.ModProjectile is PetBase pet && (form == null || pet.DisplayedForm == form))
                return pet;
        }
        return null;
    }

    public static void MovePet(PetBase self)
    {
        Projectile p = self.Projectile;
        Player owner = self.Owner;
        Vector2 center = owner.MountedCenter;
        float preferredX = (self.Definition.Light ? 50 : -50) * owner.direction;
        float preferredY = -30 + owner.gfxOffY;
        int side = preferredX < 0 ? -1 : 1;
        (int X, int Y)[] steps = Logic.SPACING_STEPS;
        if ((Tick() + (uint)p.whoAmI) % 20 == 0)
        {
            self.followSpot = 0;
            for (int i = 0; i < steps.Length; i++)
            {
                float x = preferredX + steps[i].X * side;
                float y = preferredY + steps[i].Y;
                if (SpotIsClear(p, center, x, y))
                {
                    self.followSpot = i;
                    break;
                }
            }
        }
        (int X, int Y) spot = steps[Math.Clamp(self.followSpot, 0, steps.Length - 1)];
        var target = new Vector2(center.X + preferredX + spot.X * side, center.Y + preferredY + spot.Y);
        float distance = Vector2.Distance(p.Center, target);
        p.tileCollide = false;
        p.rotation = p.velocity.X * 0.02f;
        if (Vector2.Distance(p.Center, owner.Center) <= 120)
            p.spriteDirection = owner.direction;
        else if (Math.Abs(p.velocity.X) > 0.25f)
            p.spriteDirection = p.velocity.X > 0 ? 1 : -1;
        if (distance > 1200)
        {
            p.Center = target;
            p.velocity = Vector2.Zero;
            if (self.IsOwner)
                p.netUpdate = true;
            return;
        }
        Vector2 delta = target - p.Center;
        p.velocity = delta * (distance < 1 ? 1 : 0.01f * self.Definition.Speed);
    }

    static bool SpotIsClear(Projectile me, Vector2 center, float x, float y)
    {
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            Projectile other = Main.projectile[i];
            if (!other.active || other.whoAmI == me.whoAmI || other.owner != me.owner || !Main.projPet[other.type])
                continue;
            Vector2 c = other.Center;
            if (Math.Abs(c.X - center.X - x) < 36 && Math.Abs(c.Y - center.Y - y) < 46)
                return false;
        }
        return true;
    }

    public static void DrawCell(PetBase self, int frame, int column, Color light)
    {
        Projectile p = self.Projectile;
        Texture2D texture = self.Sheet.Value;
        PetDefinition d = self.Definition;
        int width = texture.Width / d.Columns;
        int height = texture.Height / d.Frames;
        var rect = new Rectangle(column * width, frame * height, width, height);
        Vector2 position = p.Center - Main.screenPosition + new Vector2(0, 7 * Main.essScale);
        Color color = p.GetAlpha(light);
        Main.spriteBatch.Draw(texture, position, (GameNullable<Rectangle>)rect, color, p.rotation, new Vector2(width / 2f, height / 2f), p.scale,
            p.spriteDirection == -1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0f);
    }

    public static void DrawChat(PetBase self)
    {
        if (self.chatKey.Length == 0 || self.chatLag > 0 || self.chatLife <= 0 || Main.dedServ)
            return;
        SpriteFont font = FontAssets.MouseText.Value;
        // The wrapped text is kept while the line is up: only the key changes it.
        if (self.wrappedKey != self.chatKey)
            WrapChat(self, font);
        if (self.wrappedLines.Length == 0)
            return;
        Projectile p = self.Projectile;
        Vector2 pos = p.Center - Main.screenPosition;
        float alpha = Math.Min(1, Math.Min(self.chatLife / 20f, (self.chatDuration - self.chatLife + 1) / 10f)) * (1 - p.alpha / 255f);
        (int R, int G, int B) rgb = self.Definition.Name == "Kasen" && self.DisplayedForm == "Oni" ? (255, 70, 90) : self.Definition.Color;
        var color = new Color(rgb.R, rgb.G, rgb.B, (int)(255 * alpha));
        var border = new Color(0, 0, 0, (int)(255 * alpha));
        string[] lines = self.wrappedLines;
        for (int i = 0; i < lines.Length; i++)
        {
            Utils.DrawBorderStringFourWay(Main.spriteBatch, font, lines[i], pos.X, pos.Y - 45 - (lines.Length - 1 - i) * 22,
                color, border, self.wrappedOrigins[i], 0.75f);
        }
    }

    static void WrapChat(PetBase self, SpriteFont font)
    {
        self.wrappedKey = self.chatKey;
        self.wrappedLines = Array.Empty<string>();
        self.wrappedOrigins = Array.Empty<Vector2>();
        string? text = Translate(self.chatKey);
        if (string.IsNullOrEmpty(text) || text == PREFIX + self.chatKey)
            return;
        string[] words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        string line = "";
        foreach (string word in words)
        {
            string next = line.Length > 0 ? line + " " + word : word;
            if (line.Length > 0 && font.MeasureString(next).X > 230)
            {
                lines.Add(line);
                line = word;
            }
            else
            {
                line = next;
            }
        }
        if (line.Length > 0)
            lines.Add(line);
        self.wrappedLines = lines.ToArray();
        self.wrappedOrigins = new Vector2[lines.Count];
        for (int i = 0; i < lines.Count; i++)
        {
            Vector2 size = font.MeasureString(lines[i]);
            self.wrappedOrigins[i] = new Vector2(size.X / 2, size.Y / 2);
        }
    }

    public static string? Translate(string key) => Terraria.Localization.Language.GetTextValue(PREFIX + key);

    public static bool BossNear(PetBase self)
    {
        Vector2 ownerCenter = self.Owner.Center;
        int owner = self.Projectile.owner;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC n = Main.npc[i];
            if (n.active && n.boss && (n.target == owner || Vector2.DistanceSquared(n.Center, ownerCenter) <= 1280f * 1280f))
                return true;
        }
        return false;
    }
}
