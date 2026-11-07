using System;
using System.Collections.Generic;
using BossChecklistMod.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using Terraria.UI;

namespace BossChecklistMod.Common.UI;

public static class View
{
    public static float K = 1;

    public static float Fit(float w, float h)
    {
        float fit = Math.Min(1.25f, Math.Min((w - 12) / 914f, (h - 12) / 530f));
        float factor = Math.Clamp(Cfg.Log.BookScale, 0.5f, 1f);
        K = Math.Max(0.3f, fit * factor);
        return K;
    }
}

public static class C
{
    public static readonly Color White = new(255, 255, 255), Faded = new(128, 128, 128, 128), Amber = new(255, 175, 0), Green = new(150, 255, 150),
        Red = new(255, 150, 150), PapayaWhip = new(255, 239, 213), DimGray = new(105, 105, 105), DarkGray = new(169, 169, 169),
        SkyBlue = new(135, 206, 235), Silver = new(192, 192, 192), Goldenrod = new(218, 165, 32), Gold = new(255, 215, 0),
        LightYellow = new(255, 255, 224), Salmon = new(250, 128, 114), Tan = new(210, 180, 140),
        LemonChiffon = new(255, 250, 205), Tomato = new(255, 99, 71), LightGreen = new(144, 238, 144), MediumPurple = new(147, 112, 219),
        CornflowerBlue = new(100, 149, 237), DarkGreen = new(0, 100, 0), Next = new(248, 235, 91),
        ModBlue = new(150, 150, 255), Black = new(0, 0, 0);
}

public static class Refs
{
    public static BossLogUI? log;
    public static BossChecklistUI? checklist;
    public static BossLogSystem? system;
}

public static class Common
{
    public static float P(float v) => v * View.K;

    public static void Place(UIElement el, float? left, float? top, float? width = null, float? height = null)
    {
        if (left != null)
            el.Left.Set(P(left.Value), 0);
        if (top != null)
            el.Top.Set(P(top.Value), 0);
        if (width != null)
            el.Width.Set(P(width.Value), 0);
        if (height != null)
            el.Height.Set(P(height.Value), 0);
    }

    static readonly Dictionary<string, string> resourcePaths = new();
    static readonly Dictionary<string, string> uiPaths = new();

    public static Texture2D Tex(string name)
    {
        if (!resourcePaths.TryGetValue(name, out string? path))
            resourcePaths[name] = path = "Assets/Resources/" + name;
        return Tracker.ModTexture(path);
    }

    public static Texture2D UITex(string name)
    {
        if (!uiPaths.TryGetValue(name, out string? path))
            uiPaths[name] = path = "Assets/UI/" + name;
        return Tracker.ModTexture(path);
    }

    static readonly Dictionary<string, Asset<Texture2D>?> vanilla = new();

    // Some PC images do not exist on the phone: those give nothing, and the caller draws a fallback.
    public static Texture2D VanillaTexture(string path)
    {
        if (!vanilla.TryGetValue(path, out Asset<Texture2D>? asset))
        {
            try
            {
                if (!ModContent.RequestIfExists("Terraria/" + path, out asset))
                    asset = null;
            }
            catch (Exception e)
            {
                asset = null;
                Util.LogOnce("vanilla " + path, "texture " + path + " failed: " + Util.Message(e));
            }
            vanilla[path] = asset;
        }
        if (asset == null)
            return default;
        try
        {
            return asset.Value;
        }
        catch (Exception e)
        {
            vanilla[path] = null;
            Util.LogOnce("vanilla " + path, "texture " + path + " failed: " + Util.Message(e));
            return default;
        }
    }

    public static Texture2D NpcTexture(int type) => type >= 0 && type < TextureAssets.Npc.Length ? TextureAssets.Npc[type].Value : default;

    public static Texture2D TileTexture(int type) => TextureAssets.Tile[type].Value;

    public static Color LogColor(int alpha = 255) => Cfg.LogColor(alpha);

    public static Color Disco() => new(Main.DiscoR, Main.DiscoG, Main.DiscoB);

    public static Color Alpha(Color c, int a)
    {
        float f = a / 255f;
        return new Color(D.Round(c.R * f), D.Round(c.G * f), D.Round(c.B * f), a);
    }

    public static void Sound(int id) => Util.PlaySound(id);

    public static float FitScale(string text, float maxWidth, float scale = 1)
    {
        float w = D.Measure(text).X;
        return w * scale > maxWidth ? maxWidth / w : scale;
    }

    static float deathNorm;

    static float DeathScale(float scale)
    {
        if (deathNorm == 0)
        {
            float h = FontAssets.DeathText.Value.MeasureString("Ag").Y;
            deathNorm = Math.Clamp(46f / (h > 0 ? h : 46f), 0.4f, 1.2f);
        }
        return scale * deathNorm;
    }

    public static float BigMeasure(string text, float scale) => FontAssets.DeathText.Value.MeasureString(text).X * DeathScale(scale);

    public static void BigText(string text, float x, float y, Color color, float scale) =>
        Utils.DrawBorderStringFourWay(Main.spriteBatch, FontAssets.DeathText.Value, text, x, y, color, C.Black, Vector2.Zero, DeathScale(scale));

    static Texture2D InventoryBack(int number) => number switch
    {
        3 => TextureAssets.InventoryBack3.Value,
        4 => TextureAssets.InventoryBack4.Value,
        7 => TextureAssets.InventoryBack7.Value,
        8 => TextureAssets.InventoryBack8.Value,
        11 => TextureAssets.InventoryBack11.Value,
        12 => TextureAssets.InventoryBack12.Value,
        _ => default,
    };

    public static void SlotBack(int number, float x, float y, float w, float h)
    {
        Texture2D texture = InventoryBack(number);
        if (!texture.IsNull)
            D.Sprite(texture, x, y, w, h, C.White);
        else
            D.Panel(x, y, w, h, D.Rgb(63, 82, 151, 200), D.Rgb(20, 20, 40), 2);
    }

    public static void ItemInSlot(int type, float x, float y, float w, float h, Color? color = null, int stack = 0)
    {
        if (type <= 0)
            return;
        float size = Math.Min(w, h) * (32f / 52f);
        D.ItemIcon(type, x + w / 2, y + h / 2, size, w / 52f, color ?? C.White);
        if (stack > 1)
            D.Text(stack.ToString(), x + w * 0.18f, y + h * 0.58f, C.White, (w / 52f) * 0.75f);
    }

    public static float UIScale()
    {
        float scale = Main.UIScale;
        return Math.Max(0.1f, scale != 0 ? scale : 1f);
    }

    public static bool GameUIBusy(bool allowInventory = false)
    {
        if (Main.gameMenu || Main.mapFullscreen || (!allowInventory && Main.playerInventory))
            return true;
        if (Main.ingameOptionsWindow || Main.editSign || Main.inFancyUI)
            return true;
        if (!string.IsNullOrEmpty(Main.npcChatText))
            return true;
        Player p = Main.LocalPlayer;
        if (p.IsNull || p.chest != -1 || p.talkNPC != -1)
            return true;
        GUIInstance g = GUIInstance.Active;
        return !g.IsNull && (g.GUISettingsPauseMenu.Enabled || g.GUISettings.Visible);
    }

    public static void CloseInventory()
    {
        if (!Main.playerInventory)
            return;
        GUIInstance g = GUIInstance.Active;
        if (!g.IsNull && !g.GUIPageIcons.IsNull)
            g.GUIPageIcons.Close(true);
        Main.playerInventory = false;
    }

    public static void HideMouseOverInteractions()
    {
        Main.player[Main.myPlayer].mouseInterface = true;
        Main.mouseText = true;
    }
}
