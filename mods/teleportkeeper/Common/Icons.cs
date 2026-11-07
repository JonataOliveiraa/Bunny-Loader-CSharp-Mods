using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace TeleportKeeperMod.Common;

public sealed class IconInfo
{
    public readonly string key;
    public readonly Func<Player, bool>? zone;

    public IconInfo(string key, Func<Player, bool>? zone = null)
    {
        this.key = key;
        this.zone = zone;
    }
}

public static class Icons
{
    public static readonly IconInfo[] ICONS =
    {
        new("Default"),
        new("Forest"),
        new("Snow", static p => p.ZoneSnow),
        new("Desert", static p => p.ZoneDesert || p.ZoneUndergroundDesert),
        new("Jungle", static p => p.ZoneJungle),
        new("Corruption", static p => p.ZoneCorrupt),
        new("Crimson", static p => p.ZoneCrimson),
        new("Hallow", static p => p.ZoneHallow),
        new("Mushroom", static p => p.ZoneGlowshroom),
        new("Ocean", static p => p.ZoneBeach),
        new("Underground", static p => p.ZoneDirtLayerHeight),
        new("Cavern", static p => p.ZoneRockLayerHeight),
        new("Underworld", static p => p.ZoneUnderworldHeight),
        new("Dungeon", static p => p.ZoneDungeon),
        new("Temple", static p => p.ZoneLihzhardTemple),
        new("Shimmer", static p => p.ZoneShimmer),
        new("Sky", static p => p.ZoneSkyHeight),
        new("Space"),
        new("Graveyard", static p => p.ZoneGraveyard),
        new("Home"),
        new("Arena"),
    };

    static readonly string[] PRIORITY =
    {
        "Dungeon", "Temple", "Shimmer", "Underworld", "Sky", "Graveyard", "Mushroom", "Corruption", "Crimson", "Hallow",
        "Jungle", "Snow", "Desert", "Ocean", "Cavern", "Underground",
    };

    public static IconInfo IconOf(string? key)
    {
        foreach (IconInfo icon in ICONS)
        {
            if (icon.key == key)
                return icon;
        }
        return ICONS[0];
    }

    public static string IconName(string? key) => Util.L("Icons." + IconOf(key).key);

    public static string BiomeIcon(Player player)
    {
        if (player.IsNull)
            return "Forest";
        foreach (string key in PRIORITY)
        {
            Func<Player, bool>? zone = IconOf(key).zone;
            if (zone != null && zone(player))
                return key;
        }
        return "Forest";
    }

    static readonly Dictionary<string, Asset<Texture2D>?> textures = new();
    static readonly Dictionary<string, string> iconPaths = new();

    static Texture2D ModTexture(string path)
    {
        if (!textures.TryGetValue(path, out Asset<Texture2D>? asset))
        {
            if (!ModContent.RequestIfExists(Util.ModId + "/" + path, out asset))
                asset = null;
            textures[path] = asset;
        }
        return asset == null ? default : asset.Value;
    }

    public static Texture2D Crystal() => ModTexture("Assets/Textures/WorldTeleport");

    public static Texture2D DeathCrystal() => ModTexture("Assets/Textures/DeathTeleport");

    public static Texture2D Star(bool on = true) => ModTexture(on ? "Assets/Textures/Star" : "Assets/Textures/StarOff");

    public static Texture2D IconTexture(string? key)
    {
        IconInfo icon = IconOf(key);
        if (icon.key == "Default")
            return Crystal();
        if (!iconPaths.TryGetValue(icon.key, out string? path))
            iconPaths[icon.key] = path = "Assets/Icons/" + icon.key;
        Texture2D texture = ModTexture(path);
        return texture.IsNull ? Crystal() : texture;
    }
}
