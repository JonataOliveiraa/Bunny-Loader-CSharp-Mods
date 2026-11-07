// Helpers shared by the pets, buffs and items (ModUtils.cs and
// AltVanillaFunction.cs of the original).
using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace TouhouPetsMod.Common;

public static unsafe class ModUtils
{
    const string Prefix = "touhoupets/";

    static readonly Dictionary<string, Asset<Texture2D>> textures = new();
    static readonly (uint Stamp, bool Value)[] bosses = new (uint, bool)[Main.maxPlayers + 1];

    // Terraria.GameContent.Events.BirthdayParty has no C# proxy (see PartyIsUp).
    static readonly GameClass BirthdayParty = new("Terraria.GameContent.Events", "BirthdayParty");
    static readonly int[] NoParameters = { -1 };
    static MethodRef getPartyIsUp;
    static bool partyUnavailable;

    public static bool HasBuff<T>(Player player) where T : ModBuff =>
        player.FindBuffIndex(ModContent.BuffType<T>()) >= 0;

    // Keeps the pet alive while its owner has the buff (SetPetActive).
    public static void SetPetActive<T>(Projectile proj, Player owner) where T : ModBuff
    {
        if (owner.active && !owner.dead && HasBuff<T>(owner))
            proj.timeLeft = 2;
    }

    // A boss targets the player, or one is within 1280 pixels (AnyBosses).
    // Kept for the frame: some pets ask every frame, and it walks all NPCs.
    public static bool AnyBosses(Player player)
    {
        uint stamp = Main.GameUpdateCount + 1;
        ref (uint Stamp, bool Value) cached = ref bosses[player.whoAmI];
        if (cached.Stamp == stamp)
            return cached.Value;
        bool value = FindBoss(player);
        cached = (stamp, value);
        return value;
    }

    static bool FindBoss(Player player)
    {
        if (!player.active || player.dead)
            return false;
        Vector2 center = player.Center;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC n = Main.npc[i];
            if (!n.active || n.friendly || !n.boss)
                continue;
            if (n.target == player.whoAmI)
                return true;
            if (Vector2.Distance(n.Center, center) <= 1280)
                return true;
        }
        return false;
    }

    // tModLoader's ZoneForest: no biome at all, at the surface. The phone game
    // has no ZonePurity, so it is the InZonePurity list, without mod biomes.
    public static bool ZoneForest(Player p)
    {
        if (!p.ZoneOverworldHeight)
            return false;
        return !(p.ZoneBeach || p.ZoneCorrupt || p.ZoneCrimson || p.ZoneDesert || p.ZoneDungeon || p.ZoneGemCave ||
            p.ZoneGlowshroom || p.ZoneGranite || p.ZoneGraveyard || p.ZoneHallow || p.ZoneHive || p.ZoneJungle ||
            p.ZoneLihzhardTemple || p.ZoneMarble || p.ZoneMeteor || p.ZoneSnow || p.ZoneUnderworldHeight);
    }

    // BirthdayParty.PartyIsUp, by name: the class exists in the game but not in the C# API.
    // If the name ever fails, the pets just never mention a party.
    public static bool PartyIsUp
    {
        get
        {
            if (partyUnavailable)
                return false;
            try
            {
                ref readonly MethodRef method = ref GameInterop.Method(ref getPartyIsUp, BirthdayParty, "bool get_PartyIsUp()", NoParameters);
                CallFrame frame = default;
                frame.X[0] = (long)method.Info;
                frame.Ret = RetKind.Int;
                GameInterop.Call(method.Fn, &frame);
                return (frame.Out[0] & 0xFF) != 0;
            }
            catch (Exception)
            {
                partyUnavailable = true;
                return false;
            }
        }
    }

    // A texture of this mod (Assets/Textures/Extra/..., Assets/Textures/Glow/...).
    public static Texture2D Texture(string path)
    {
        if (!textures.TryGetValue(path, out Asset<Texture2D>? asset))
            textures[path] = asset = ModContent.Request<Texture2D>(Prefix + path);
        return asset.Value;
    }

    // Switches the sprite batch to additive blending and back (QuickToggleAdditiveMode).
    public static void ToggleAdditive(bool start)
    {
        SpriteBatch sb = Main.spriteBatch;
        sb.End();
        sb.Begin(start ? SpriteSortMode.Immediate : SpriteSortMode.Deferred, start ? BlendState.Additive : BlendState.AlphaBlend,
            Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, default, Main.Transform, true);
    }

    // SpriteBatch.Draw with a float scale and with a Vector2 scale (TeaNPCDraw).
    public static void Draw(Texture2D texture, Vector2 position, Rectangle? rect, Color color, float rotation, Vector2 origin, float scale, SpriteEffects effects)
    {
        Main.spriteBatch.Draw(texture, position, (GameNullable<Rectangle>)rect, color, rotation, origin, scale, effects, 0f);
    }

    public static void DrawScaled(Texture2D texture, Vector2 position, Rectangle? rect, Color color, float rotation, Vector2 origin, Vector2 scale, SpriteEffects effects)
    {
        Main.spriteBatch.Draw(texture, position, (GameNullable<Rectangle>)rect, color, rotation, origin, scale, effects, 0f);
    }
}

// The dust types the pets use (MyDustId.cs).
public static class MyDustId
{
    public const int Water = 33;
    public const int Smoke = 31;
    public const int PurpleTorch = 62;
    public const int GreenTrans = 89;
    public const int TrailingCyan = 132;
    public const int OrangeFire2 = 174;
    public const int TrailingGreen1 = 220;
    public const int ElectricCyan = 226;
}
