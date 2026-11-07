using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using mitanpc.Content.NPCs.TownNPCs;
using mitanpc.Content.Projectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using MitaClass = mitanpc.Content.NPCs.TownNPCs.Mitas.Mita.Mita;

namespace mitanpc.Common;

public abstract class TownNPCBase : ModNPC
{
    const string ShopName = "Shop";

    readonly struct DialogueLine
    {
        public readonly string Key;
        public readonly string Sound;

        public DialogueLine(string key, string sound)
        {
            Key = key;
            Sound = sound;
        }
    }

    static Dictionary<string, List<DialogueLine>>? dialogues;
    static readonly Dictionary<string, SoundStyle> sounds = new();

    int swingTime;
    int attackCooldown;
    int? walkFrameY;

    bool IsPlayerNPC => Name == "MiSidePlayer";

    public override void SetStaticDefaults()
    {
        bool player = IsPlayerNPC;
        Main.npcFrameCount[Type] = player ? 25 : 23;
        NPCID.Sets.ExtraFramesCount[Type] = 9;
        NPCID.Sets.AttackFrameCount[Type] = 4;
        NPCID.Sets.DangerDetectRange[Type] = Name == "Cappie" ? 30 : player ? 60 : 90;
        NPCID.Sets.AttackType[Type] = 3;
        NPCID.Sets.AttackTime[Type] = player ? 10 : 15;
        NPCID.Sets.AttackAverageChance[Type] = 1;

        NPC.Happiness
            .SetBiomeAffection<ForestBiome>(AffectionLevel.Love)
            .SetBiomeAffection<UndergroundBiome>(player ? AffectionLevel.Dislike : AffectionLevel.Like)
            .SetBiomeAffection<HallowBiome>(player ? AffectionLevel.Like : AffectionLevel.Dislike);
        if (player)
            NPC.Happiness.SetBiomeAffection<DesertBiome>(AffectionLevel.Dislike);
        else
            NPC.Happiness.SetBiomeAffection<JungleBiome>(AffectionLevel.Dislike);
        foreach (int type in new[] { NPCID.Nurse, NPCID.BestiaryGirl, NPCID.Dryad, NPCID.Stylist, NPCID.Mechanic, NPCID.PartyGirl, NPCID.Steampunker, NPCID.Princess })
            NPC.Happiness.SetNPCAffection(type, AffectionLevel.Hate);

        // Every content type has its number by now, so the JavaScript's PostSetupContent step is not needed.
        if (player)
            NPC.Happiness.SetNPCAffection<MitaClass>(AffectionLevel.Love);
        else
            NPC.Happiness.SetNPCAffection<MiSidePlayer>(AffectionLevel.Love);
    }

    public override void SetDefaults()
    {
        NPC.townNPC = true;
        NPC.friendly = true;
        NPC.aiStyle = 7;
        NPC.width = 38;
        NPC.height = 50;
        NPC.damage = 10;
        NPC.defense = 15;
        NPC.lifeMax = 250;
        NPC.HitSound = SoundID.NPCHit1;
        NPC.DeathSound = SoundID.NPCDeath1;
        NPC.knockBackResist = 0.5f;
        AnimationType = NPCID.Mechanic;
    }

    public override bool CanTownNPCSpawn(int numTownNPCs) => true;

    public override List<string> SetNPCNameList() => new() { Language.GetTextValue($"Mods.{Mod.Name}.NPCName.{Name}") ?? Name };

    public override void SetChatButtons(ref string button, ref string button2) => button = Language.GetTextValue("LegacyInterface.28") ?? "";

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        if (firstButton)
            shopName = ShopName;
    }

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement($"Mods.{Mod.Name}.Bestiary.{Name}"));
    }

    // dialogues.json has one simple shape, and the phone has no System.Text.Json.
    Dictionary<string, List<DialogueLine>> Dialogues()
    {
        if (dialogues != null)
            return dialogues;
        var result = new Dictionary<string, List<DialogueLine>>();
        string json = Encoding.UTF8.GetString(Mod.GetFileBytes("Assets/dialogues.json"));
        foreach (Match section in Regex.Matches(json, "\"(\\w+)\"\\s*:\\s*\\[(.*?)\\]", RegexOptions.Singleline))
        {
            var lines = new List<DialogueLine>();
            foreach (Match line in Regex.Matches(section.Groups[2].Value, "\"key\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"sound\"\\s*:\\s*\"([^\"]+)\""))
                lines.Add(new DialogueLine(line.Groups[1].Value, line.Groups[2].Value));
            result[section.Groups[1].Value] = lines;
        }
        return dialogues = result;
    }

    static bool HasDigit(string text)
    {
        foreach (char c in text)
        {
            if (char.IsAsciiDigit(c))
                return true;
        }
        return false;
    }

    public override string GetChat()
    {
        string name = Name;
        string playerName = Main.LocalPlayer.name ?? "";
        List<DialogueLine> lines = Dialogues()[name];
        if (name == "Mita")
        {
            bool day = Main.dayTime;
            lines = lines.FindAll(line =>
            {
                bool night = line.Key.Contains(".NightPhrase");
                return day
                    ? !night && (!line.Key.EndsWith("YouUsingDigitsInName", StringComparison.Ordinal) || HasDigit(playerName))
                    : night;
            });
        }
        DialogueLine selected = lines[Random.Shared.Next(lines.Count)];
        bool russian = Language.ActiveCulture.Name == "ru-RU";
        if ((name != "MiSidePlayer" || russian) && Main.netMode != NetmodeID.Server)
        {
            string voice = name == "MiSidePlayer" ? "MiSidePlayer/Russian/" : "Mita/" + (russian ? "Russian/" : "Japanese/");
            string path = $"{Mod.Name}/Assets/Sounds/{voice}{selected.Sound}";
            if (!sounds.TryGetValue(path, out SoundStyle sound))
                sounds[path] = sound = new SoundStyle(path);
            SoundEngine.PlaySound(sound);
        }

        // Only the first {0}, as in the JavaScript, and a name with braces is safe.
        string text = Language.GetTextValue($"Mods.{Mod.Name}.{selected.Key}") ?? "";
        int at = text.IndexOf("{0}", StringComparison.Ordinal);
        return at < 0 ? text : text[..at] + playerName + text[(at + 3)..];
    }

    // The JavaScript mod had no town melee callbacks, so it kept the original reach and cooldown
    // here and let a short-lived npcProj handle the hits. This keeps that behavior.
    public override void AI()
    {
        if (swingTime > 0)
            swingTime--;
        if (attackCooldown > 0)
        {
            attackCooldown--;
            return;
        }
        if (Main.netMode == NetmodeID.MultiplayerClient)
            return;
        float reach = NPCID.Sets.DangerDetectRange[NPC.type];
        Vector2 center = NPC.Center;
        bool found = false;
        Vector2 targetCenter = default;
        float nearest = reach;
        int count = Main.npc.Length - 1;
        for (int i = 0; i < count; i++)
        {
            NPC other = Main.npc[i];
            if (!other.active || other.friendly || other.dontTakeDamage || other.damage <= 0)
                continue;
            Vector2 at = other.Center;
            float distance = Vector2.Distance(at, center);
            if (distance < nearest && Collision.CanHit(NPC.position, NPC.width, NPC.height, other.position, other.width, other.height))
            {
                found = true;
                targetCenter = at;
                nearest = distance;
            }
        }
        if (!found)
            return;
        bool player = IsPlayerNPC;
        int direction = targetCenter.X < center.X ? -1 : 1;
        NPC.direction = direction;
        NPC.spriteDirection = direction;
        swingTime = player ? 10 : 15;
        attackCooldown = 24 + Random.Shared.Next(6);
        int damage = NPC.GetAttackDamage_ForTownNPC(30);
        int index = Projectile.NewProjectile(NPC.GetSpawnSource_ForProjectile(), center.X + direction * 20, center.Y,
            0, 0, ModContent.ProjectileType<TownMeleeStrike>(), damage, player ? 2 : 5, Main.myPlayer, 0, 0, 0);
        if (index >= 0 && index < Main.projectile.Length)
        {
            Main.projectile[index].npcProj = true;
            Main.projectile[index].noDropItem = true;
        }
    }

    public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        if (swingTime > 0)
        {
            int frames = Main.npcFrameCount[NPC.type];
            int height = TextureAssets.Npc[NPC.type].Value.Height / frames;
            int duration = NPCID.Sets.AttackTime[NPC.type];
            int attackFrame = Math.Min(3, Math.Max(0, duration - swingTime) * 4 / duration);
            walkFrameY = NPC.frame.Y;
            NPC.frame.Y = (frames - 4 + attackFrame) * height;
        }
        return true;
    }

    public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
    {
        // Keep AnimationType's walking frame and counter intact between draws.
        if (walkFrameY is int walk)
        {
            NPC.frame.Y = walk;
            walkFrameY = null;
        }
        if (swingTime <= 0 || Name == "Cappie")
            return;
        bool player = IsPlayerNPC;
        int itemType = Name == "Mila" ? ItemID.Katana : player ? ItemID.Ruler : ItemID.PsychoKnife;
        Texture2D texture = TextureAssets.Item[itemType].Value;
        int direction = NPC.spriteDirection;
        Vector2 center = NPC.Center;
        spriteBatch.Draw(texture, new Vector2(center.X - screenPos.X + direction * 3, center.Y - screenPos.Y + 11),
            null, drawColor, direction * (1.1f - swingTime * 0.15f),
            new Vector2(texture.Width / 2f, texture.Height / 2f), player ? 1f : 0.15f, direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
    }
}
