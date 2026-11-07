using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;
using TooltipIconMod.Common;

public class TooltipIcon : Mod
{
    public override void Load()
    {
        TooltipIcons.Initialize();
    }

    // A texture is an Asset<Texture2D>, a Texture2D or a "ModName/path" string.
    static object Texture(object? value, string name, string usage)
    {
        if (value == null)
            throw new ArgumentException(name + " cannot be null. " + usage);
        switch (value)
        {
            case string path:
                try
                {
                    return ModContent.Request<Texture2D>(path);
                }
                catch (Exception e) when (e is not OutOfMemoryException)
                {
                    throw new ArgumentException(name + " should be a texture, but '" + path + "' was not found. " + usage);
                }
            case Asset<Texture2D>:
            case Texture2D:
                return value;
            default:
                throw new ArgumentException(name + " should be a texture. " + usage);
        }
    }

    static object? OptionalTexture(object? value, string name, string usage) => value == null ? null : Texture(value, name, usage);

    static string Text(object? value, string name, string usage)
    {
        if (value is not string text || text.Length == 0)
            throw new ArgumentException(name + " cannot be null or empty. " + usage);
        return text;
    }

    static void Need(object?[] args, int count, string method, string usage)
    {
        if (args.Length < count)
            throw new ArgumentException(method + " requires " + count + (count > 1 ? " arguments" : " argument") + " at least. " + usage);
    }

    static object? ModName(object? value) => value is Mod mod ? mod.Name : value;

    static DamageClass DamageClassOf(object? value, string usage)
    {
        if (value is DamageClass cls)
            return cls;
        throw new ArgumentException("damageClass cannot be null. " + usage);
    }

    static void Register(string modName, string lineName, Icon icon)
    {
        if (!TooltipIcons.AddIcon(modName, lineName, icon))
            throw new ArgumentException("Specified tooltip line (modName: " + modName + ", tooltipLineName: " + lineName + ") has already been registered.");
    }

    static Func<Item, DrawableTooltipLine, bool> Condition(object? value, string usage) =>
        value as Func<Item, DrawableTooltipLine, bool> ?? throw new ArgumentException("condition should be a function. " + usage);

    public override object? Call(params object?[] allArgs)
    {
        if (allArgs.Length == 0 || allArgs[0] is not string method)
            throw new ArgumentException("1st argument should be string, this is method name.");
        object?[] args = allArgs[1..];
        object? At(int index) => index < args.Length ? args[index] : null;

        TooltipIcons.Initialize();
        switch (method)
        {
            case "AddNormalIcon":
            {
                string usage = method + "(string modName, string tooltipLineName, Asset<Texture2D> texture)";
                Need(args, 3, method, usage);
                string mod = Text(ModName(args[0]), "modName", usage), line = Text(args[1], "tooltipLineName", usage);
                Register(mod, line, new NormalIcon(Texture(args[2], "texture", usage)));
                return null;
            }
            case "AddPrefixIcon":
            {
                string usage = method + "(string modName, string tooltipLineName, Asset<Texture2D> texture, bool doesSmallerValueBenefitPositiveEffect = false)";
                Need(args, 3, method, usage);
                string mod = Text(ModName(args[0]), "modName", usage), line = Text(args[1], "tooltipLineName", usage);
                Register(mod, line, new PrefixIcon(Texture(args[2], "texture", usage), At(3) is true));
                return null;
            }
            case "AddFramedIcon":
            {
                string usage = method + "(string modName, string tooltipLineName, Asset<Texture2D> texture, Rectangle? frame, Vector2 offset)";
                Need(args, 3, method, usage);
                string mod = Text(ModName(args[0]), "modName", usage), line = Text(args[1], "tooltipLineName", usage);
                Register(mod, line, new FramedIcon(Texture(args[2], "texture", usage), At(3) as Rectangle?, At(4) as Vector2?));
                return null;
            }
            case "AddCustomIcon":
            {
                string usage = method + "(string modName, string tooltipLineName, Action<Item, DrawableTooltipLine, SpriteBatch, Vector2, Color> draw)";
                Need(args, 3, method, usage);
                string mod = Text(ModName(args[0]), "modName", usage), line = Text(args[1], "tooltipLineName", usage);
                Action<Item, DrawableTooltipLine, SpriteBatch, Vector2, Color, float> draw = args[2] switch
                {
                    Action<Item, DrawableTooltipLine, SpriteBatch, Vector2, Color, float> scaled => scaled,
                    Action<Item, DrawableTooltipLine, SpriteBatch, Vector2, Color> plain => (item, l, sb, position, color, _) => plain(item, l, sb, position, color),
                    _ => throw new ArgumentException("draw should be a function. " + usage),
                };
                Register(mod, line, new CustomIcon(draw));
                return null;
            }
            case "AddDamageClassIcon":
            {
                string usage = method + "(DamageClass damageClass, Asset<Texture2D>? damageTexture, Asset<Texture2D>? speedTexture, Asset<Texture2D>? knockbackTexture = null, Asset<Texture2D>? critChanceTexture = null)";
                Need(args, 3, method, usage);
                TooltipIcons.AddDamageClassIcons(DamageClassOf(args[0], usage), OptionalTexture(args[1], "damageTexture", usage),
                    OptionalTexture(args[2], "speedTexture", usage), OptionalTexture(At(3), "knockbackTexture", usage),
                    OptionalTexture(At(4), "critChanceTexture", usage));
                return null;
            }
            case "AddConsumableIcon":
            case "AddEquipableIcon":
            case "AddBuffTimeIcon":
            {
                string usage = method + "(Func<Item, DrawableTooltipLine, bool> condition, Asset<Texture2D> texture)";
                Need(args, 2, method, usage);
                Func<Item, DrawableTooltipLine, bool> condition = Condition(args[0], usage);
                object texture = Texture(args[1], "texture", usage);
                if (method == "AddConsumableIcon")
                    TooltipIcons.AddConsumableIcon(condition, texture);
                else if (method == "AddEquipableIcon")
                    TooltipIcons.AddEquipableIcon(condition, texture);
                else
                    TooltipIcons.AddBuffTimeIcon(condition, texture);
                return null;
            }
            case "AddBossSummoningItem":
            {
                string usage = method + "(int itemType)";
                Need(args, 1, method, usage);
                // ItemID values are shorts, so any whole number is taken.
                int type = args[0] is sbyte or byte or short or ushort or int ? Convert.ToInt32(args[0]) : -1;
                if (type < 0)
                    throw new ArgumentException("Specified item (#" + args[0] + ") does not exist. " + usage);
                Icons.BossSummoningItems.Add(type);
                return null;
            }
        }
        throw new ArgumentException("Unknown method name \"" + method + "\".");
    }
}
