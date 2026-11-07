using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static BossChecklistMod.Common.ModText;

namespace BossChecklistMod.Common.UI;

public static class Hover
{
    const long PinMs = 4000;

    static readonly Regex VanillaKey = new(@"^[A-Za-z_]+\.[A-Za-z0-9_.]+\z");
    // The game's collector does not see C# fields: the handles keep these items alive.
    static readonly Dictionary<int, GameHandle> items = new();

    static string text = "";
    static object?[] parameters = Array.Empty<object?>();
    static Color? color;
    static int item;
    static string? itemName;
    static int pinnedType;
    static string? pinnedName;
    static long pinnedUntil;
    static bool shown;

    static Item SampleItem(int type)
    {
        if (!items.TryGetValue(type, out GameHandle? handle))
        {
            Item created = Terraria.Item.New();
            created.SetDefaults(type, default);
            items[type] = handle = new GameHandle(created.Pointer);
        }
        return new Item(handle.Target);
    }

    public static string Resolve(string value, object?[]? args = null)
    {
        args ??= Array.Empty<object?>();
        if (HasKey(value))
            return L(value, args);
        if (VanillaKey.IsMatch(value))
        {
            string found = Vanilla(value, args.Length > 0 ? args[0] : null);
            if (found != value)
                return found;
        }
        return value;
    }

    public static void Set(string? value, object?[]? args = null, Color? textColor = null)
    {
        if (string.IsNullOrEmpty(value))
            return;
        text = value;
        parameters = args ?? Array.Empty<object?>();
        color = textColor;
    }

    public static void Item(int type, string? name = null)
    {
        if (type <= 0)
            return;
        item = type;
        itemName = name;
    }

    public static void Pin(int type, string? name = null)
    {
        if (type <= 0)
            return;
        pinnedType = type;
        pinnedName = name;
        pinnedUntil = Environment.TickCount64 + PinMs;
    }

    public static void Unpin() => pinnedType = 0;

    public static bool HasWork => item != 0 || text.Length > 0 || pinnedType != 0 || shown;

    public static void Draw()
    {
        if (Gesture.touching && Gesture.pressed && item == 0)
            pinnedType = 0;
        int drawItem = item;
        string? name = itemName;
        if (drawItem == 0 && text.Length == 0 && pinnedType != 0)
        {
            if (Environment.TickCount64 < pinnedUntil)
            {
                drawItem = pinnedType;
                name = pinnedName;
            }
            else
            {
                pinnedType = 0;
            }
        }
        if (drawItem != 0)
        {
            shown = true;
            int type = drawItem;
            string? nameNow = name;
            Util.Safe("item tooltip", () => ItemTooltip(type, nameNow));
        }
        else
        {
            ClearGameHover();
            if (text.Length > 0)
            {
                string resolved = Resolve(text, parameters);
                Color? colorNow = color;
                Util.Safe("hover text", () => DrawTooltipBackground(resolved, colorNow));
            }
        }
        text = "";
        parameters = Array.Empty<object?>();
        color = null;
        item = 0;
        itemName = null;
    }

    static void ClearGameHover()
    {
        if (!shown)
            return;
        shown = false;
        Main.HoverItem = SampleItem(0);
        Main.hoverItemName = "";
    }

    static void ItemTooltip(int type, string? nameOverride)
    {
        Item it = SampleItem(type);
        Main.HoverItem = it;
        string name = nameOverride ?? it.HoverName ?? it.Name ?? "";
        Main.hoverItemName = name;
        D.Reset();
        Main.instance.MouseText(name, it.rare, 0, -1, -1, -1, -1, 0);
        D.Reset();
        Main.hoverItemName = "";
    }

    public static void DrawTooltipBackground(string value, Color? textColor)
    {
        if (string.IsNullOrEmpty(value))
            return;
        int sw = Main.screenWidth, sh = Main.screenHeight;
        RichLayout layout = Rich.Layout(value, Math.Min(420, sw - 40), 1, textColor ?? C.White);
        float w = layout.MeasuredWidth();
        const float pad = 10;
        float bw = w + pad * 2, bh = layout.height + pad * 2 - 5;
        float x = Gesture.x + 20, y = Gesture.y - bh - 24;
        if (y < sh * 0.2f)
        {
            x = Gesture.x - bw / 2;
            y = Gesture.y + 40;
        }
        x = Math.Max(0, Math.Min(sw - bw, x));
        y = Math.Max(0, Math.Min(sh - bh, y));
        var back = new Color(D.Round(23 * 0.925), D.Round(25 * 0.925), D.Round(81 * 0.925), D.Round(255 * 0.925));
        Utils.DrawInvBG(Main.spriteBatch, new Rectangle(D.Round(x), D.Round(y), D.Round(bw), D.Round(bh)), back);
        Rich.Draw(layout, x + pad, y + pad, 1);
    }
}
