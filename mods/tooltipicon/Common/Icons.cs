using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace TooltipIconMod.Common;

public abstract class Icon
{
    public abstract void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale);
}

public static class Icons
{
    // Assets, not textures: the game keeps an asset alive, but nothing roots a texture held only by C#.
    static readonly Dictionary<string, Asset<Texture2D>?> textures = new();

    public static Asset<Texture2D>? OwnTexture(string path)
    {
        if (!textures.TryGetValue(path, out Asset<Texture2D>? asset))
        {
            Mod mod = ModContent.GetInstance<TooltipIcon>();
            try
            {
                asset = mod.Assets.Request<Texture2D>("Assets/Textures/" + path);
            }
            catch (Exception e)
            {
                mod.Logger.Warn("texture " + path + " did not load: " + e.Message);
            }
            textures[path] = asset;
        }
        return asset;
    }

    // A source is one of the mod's own paths, an Asset<Texture2D> or a Texture2D.
    public static bool TextureOf(object? source, out Texture2D texture)
    {
        switch (source)
        {
            case string path:
                if (OwnTexture(path) is Asset<Texture2D> own)
                {
                    texture = own.Value;
                    return true;
                }
                break;
            case Asset<Texture2D> asset:
                texture = asset.Value;
                return true;
            case Texture2D direct:
                texture = direct;
                return true;
        }
        texture = default;
        return false;
    }

    public static void Draw(SpriteBatch sb, object? source, Vector2 position, Color color, Rectangle? frame = null, Vector2? offset = null, float scale = 1f)
    {
        if (TextureOf(source, out Texture2D tex))
            DrawTexture(sb, tex, position, color, frame, offset, scale);
    }

    public static void DrawTexture(SpriteBatch sb, Texture2D tex, Vector2 position, Color color, Rectangle? frame = null, Vector2? offset = null, float scale = 1f)
    {
        Vector2 shift = offset ?? Vector2.Zero;
        var at = new Vector2(position.X + shift.X * scale, position.Y + shift.Y * scale);
        if (frame is Rectangle rect)
            sb.Draw(tex, at, rect, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        else
            sb.Draw(tex, at, null, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    public const string PrefixPositive = "Prefixes/PrefixPositive";
    public const string PrefixNegative = "Prefixes/PrefixNegative";
    public const string PrefixPositiveReversed = "Prefixes/PrefixPositiveReversed";
    public const string PrefixNegativeReversed = "Prefixes/PrefixNegativeReversed";

    public static readonly FitIcon BossIcon = new("Misc/BossSummon");

    public static readonly HashSet<int> BossSummoningItems = new() { 560, 43, 70, 1331, 1307, 1133, 5120, 267, 4988, 544, 556, 557, 1293, 2673, 3601, 4271, 361, 3828, 1315, 1844, 1958, 2767, 602, 5334 };
}

public class FitIcon : Icon
{
    readonly string path;
    readonly float box;

    public FitIcon(string path, float box = 24f)
    {
        this.path = path;
        this.box = box;
    }

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale)
    {
        if (!Icons.TextureOf(path, out Texture2D tex))
            return;
        float size = Math.Max(tex.Width, tex.Height);
        float fit = size > box ? box / size : 1f;
        var offset = new Vector2((22 - tex.Width * fit) / 2, (22 - tex.Height * fit) / 2);
        Icons.DrawTexture(sb, tex, position, color, null, offset, scale * fit);
    }
}

sealed class ConditionalTexture
{
    readonly List<(Func<Item, DrawableTooltipLine, bool> Condition, object Texture)> list = new();

    public void Add(Func<Item, DrawableTooltipLine, bool> condition, object texture)
    {
        int found = list.FindIndex(e => e.Condition == condition);
        if (found >= 0)
            list[found] = (condition, texture);
        else
            list.Add((condition, texture));
    }

    public object? Get(Item item, DrawableTooltipLine line)
    {
        foreach ((Func<Item, DrawableTooltipLine, bool> condition, object texture) in list)
        {
            bool ok;
            try
            {
                ok = condition(item, line);
            }
            catch (Exception)
            {
                ok = false;
            }
            if (ok)
                return texture;
        }
        return null;
    }
}

public class NormalIcon : Icon
{
    readonly object texture;

    public NormalIcon(object texture) => this.texture = texture;

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale) =>
        Icons.Draw(sb, texture, position, color, null, null, scale);
}

public class FramedIcon : Icon
{
    readonly object texture;
    readonly Rectangle? frame;
    readonly Vector2? offset;

    public FramedIcon(object texture, Rectangle? frame = null, Vector2? offset = null)
    {
        this.texture = texture;
        this.frame = frame;
        this.offset = offset;
    }

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale) =>
        Icons.Draw(sb, texture, position, color, frame, offset, scale);
}

public class ItemIcon : Icon
{
    readonly int itemType;
    readonly Vector2? offset;

    public ItemIcon(int itemType, Vector2? offset = null)
    {
        this.itemType = itemType;
        this.offset = offset;
    }

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale)
    {
        Main.instance.LoadItem(itemType);
        Icons.DrawTexture(sb, TextureAssets.Item[itemType].Value, position, color, null, offset, scale);
    }
}

public class CustomIcon : Icon
{
    readonly Action<Item, DrawableTooltipLine, SpriteBatch, Vector2, Color, float> draw;

    public CustomIcon(Action<Item, DrawableTooltipLine, SpriteBatch, Vector2, Color, float> draw) => this.draw = draw;

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale) =>
        draw(item, line, sb, position, color, scale);
}

public class PrefixIcon : Icon
{
    readonly object texture;
    readonly string positive, negative;

    public PrefixIcon(object texture, bool isReversed = false)
    {
        this.texture = texture;
        positive = isReversed ? Icons.PrefixPositiveReversed : Icons.PrefixPositive;
        negative = isReversed ? Icons.PrefixNegativeReversed : Icons.PrefixNegative;
    }

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale)
    {
        Icons.Draw(sb, texture, position, color, null, null, scale);
        Icons.Draw(sb, line.IsModifierBad ? negative : positive, position, color, null, null, scale);
    }
}

public class TileBoostIcon : Icon
{
    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale)
    {
        Icons.Draw(sb, "Placeables/Block", position, color, null, null, scale);
        Icons.Draw(sb, item.tileBoost >= 0 ? Icons.PrefixPositive : Icons.PrefixNegative, position, color, null, null, scale);
    }
}

public class PlaceableIcon : Icon
{
    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale) =>
        Icons.Draw(sb, item.createWall > 0 ? "Placeables/Wall" : "Placeables/Block", position, color, null, null, scale);
}

public class WeaponIcon : Icon
{
    readonly string common;
    readonly Dictionary<DamageClass, object> textures = new();

    static string? W(string? name) => name != null ? "Weapons/" + name : null;

    public WeaponIcon(string common, string? melee, string? ranged, string? magic, string? summon)
    {
        this.common = W(common)!;
        if (W(melee) is string meleePath)
        {
            textures[DamageClass.Melee] = meleePath;
            textures[DamageClass.MeleeNoSpeed] = meleePath;
        }
        if (W(ranged) is string rangedPath)
            textures[DamageClass.Ranged] = rangedPath;
        if (W(magic) is string magicPath)
        {
            textures[DamageClass.Magic] = magicPath;
            textures[DamageClass.MagicSummonHybrid] = magicPath;
        }
        if (W(summon) is string summonPath)
        {
            textures[DamageClass.Summon] = summonPath;
            textures[DamageClass.SummonMeleeSpeed] = summonPath;
        }
    }

    object TextureFor(Item item) => textures.TryGetValue(item.DamageType, out object? texture) ? texture : common;

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale) =>
        Icons.Draw(sb, TextureFor(item), position, color, null, null, scale);

    public bool AddModDamageClassTexture(DamageClass cls, object? texture)
    {
        if (textures.ContainsKey(cls))
            return false;
        if (texture != null)
            textures[cls] = texture;
        return true;
    }
}

public class WeaponPrefixIcon : WeaponIcon
{
    readonly string positive, negative;

    public WeaponPrefixIcon(string common, string? melee, string? ranged, string? magic, string? summon, bool isReversed = false)
        : base(common, melee, ranged, magic, summon)
    {
        positive = isReversed ? Icons.PrefixPositiveReversed : Icons.PrefixPositive;
        negative = isReversed ? Icons.PrefixNegativeReversed : Icons.PrefixNegative;
    }

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale)
    {
        base.Draw(item, line, sb, position, color, scale);
        Icons.Draw(sb, line.IsModifierBad ? negative : positive, position, color, null, null, scale);
    }
}

public class ConsumableIcon : Icon
{
    readonly ConditionalTexture mod = new();

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale)
    {
        if (mod.Get(item, line) is object own)
        {
            Icons.Draw(sb, own, position, color, null, null, scale);
            return;
        }
        if (Icons.BossSummoningItems.Contains(item.type))
        {
            Icons.BossIcon.Draw(item, line, sb, position, color, scale);
            return;
        }
        Icons.Draw(sb, Pick(item), position, color, null, null, scale);
    }

    static string Pick(Item item)
    {
        if (item.makeNPC > 0)
            return "Consumables/ReleaseNPC";
        if (item.healLife > 0)
            return "Consumables/LifePotion";
        if (item.healMana > 0)
            return "Consumables/ManaPotion";
        if (ItemID.Sets.IsFood[item.type])
            return "Consumables/Food";
        if (item.buffType > 0)
            return "Consumables/BuffPotion";
        switch (item.type)
        {
            case 29:
            case 1291:
                return "Consumables/MaxLifeUp";
            case 109:
                return "Consumables/MaxManaUp";
            case 2350:
            case 2351:
            case 2997:
            case 4870:
                return "Consumables/TeleportPotion";
            case 678:
                return "Consumables/BuffPotion";
            case 4829:
            case 4830:
            case 4910:
                return "Consumables/ReleaseNPC";
        }
        return "Consumables/Consumable";
    }

    public void AddModIcon(Func<Item, DrawableTooltipLine, bool> condition, object texture) => mod.Add(condition, texture);
}

public class EquipableIcon : Icon
{
    readonly ConditionalTexture mod = new();

    static string Pick(Item item)
    {
        if (item.shoot > 0 && Main.projHook[item.shoot])
            return "Equipables/Hook";
        if (item.mountType != -1)
            return MountID.Sets.Cart[item.mountType] ? "Equipables/Minecart" : "Equipables/Mount";
        if (item.buffType > 0 && Main.lightPet[item.buffType])
            return "Equipables/LightPet";
        if (item.buffType > 0 && Main.vanityPet[item.buffType])
            return "Equipables/Pet";
        return item.accessory ? "Equipables/Accessory" : "Equipables/Armor";
    }

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale) =>
        Icons.Draw(sb, mod.Get(item, line) ?? Pick(item), position, color, null, null, scale);

    public void AddModIcon(Func<Item, DrawableTooltipLine, bool> condition, object texture) => mod.Add(condition, texture);
}

public class BuffTimeIcon : Icon
{
    readonly ConditionalTexture mod = new();

    public override void Draw(Item item, DrawableTooltipLine line, SpriteBatch sb, Vector2 position, Color color, float scale)
    {
        object texture = mod.Get(item, line) ?? (ItemID.Sets.IsFood[item.type] ? "Consumables/FoodDuration" : "Consumables/BuffDuration");
        Icons.Draw(sb, texture, position, color, null, null, scale);
    }

    public void AddModIcon(Func<Item, DrawableTooltipLine, bool> condition, object texture) => mod.Add(condition, texture);
}
