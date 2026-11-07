using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NPCUnlockAnnouncerMod.Common.Configs;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace NPCUnlockAnnouncerMod.Common.UI;

public static class Banner
{
    // The original counted 20 drawn frames at 60 per second; these are the same times in seconds.
    const double EnterSeconds = 20 / 60.0;
    const double ExitSeconds = 20 / 60.0;
    const float HiddenY = -100;
    const float VisibleY = 30;
    const int Width = 420;
    const int Height = 64;

    enum AnimState { Hidden, Entering, Showing, Exiting }

    readonly record struct Entry(int Type, string Title, string Subtitle);

    readonly record struct Layout(int X, int Title, int Sub, Vector3 Gold, Vector3 Grey);

    static readonly Layout LayoutModern = new(72, 10, 36, new Vector3(0, 230, 255), new Vector3(200, 220, 240));
    static readonly Layout LayoutCrimson = new(76, 14, 42, new Vector3(255, 60, 60), new Vector3(240, 180, 180));
    static readonly Layout LayoutWood = new(76, 14, 42, new Vector3(245, 190, 100), new Vector3(190, 230, 150));
    static readonly Layout LayoutJungle = new(76, 14, 42, new Vector3(255, 170, 50), new Vector3(160, 235, 120));
    static readonly Layout LayoutCorruption = new(76, 14, 42, new Vector3(190, 110, 255), new Vector3(220, 195, 245));
    static readonly Layout LayoutClassic = new(70, 8, 34, new Vector3(255, 215, 0), new Vector3(211, 211, 211));

    static readonly Queue<Entry> queue = new();
    static readonly Dictionary<BannerStyle, Asset<Texture2D>?> textures = new();

    static AnimState state = AnimState.Hidden;
    static double timer;
    static float y = HiddenY;
    static Entry? current;
    static long lastDraw;

    static float Smooth(double t)
    {
        float x = Math.Clamp((float)t, 0f, 1f);
        return x * x * (3 - 2 * x);
    }

    static float Lerp(float a, float b, float t) => a + (b - a) * t;

    static int Round(float value) => (int)MathF.Floor(value + 0.5f);

    static Color Rgb(Vector3 c, float k = 1f, float a = 255f) =>
        new(Round(c.X * k), Round(c.Y * k), Round(c.Z * k), Round(a * k));

    public static void Show(int type, string title, string subtitle)
    {
        if (Main.dedServ)
            return;
        queue.Enqueue(new Entry(type, title, subtitle));
    }

    public static void Preview()
    {
        (string title, string subtitle) = Lore.GetLore(NPCID.Guide);
        Show(NPCID.Guide, title, subtitle);
    }

    public static void Clear()
    {
        queue.Clear();
        current = null;
        state = AnimState.Hidden;
        timer = 0;
        y = HiddenY;
        lastDraw = 0;
    }

    // Measured in real time: the phone's frame rate is not fixed.
    static void Step()
    {
        long now = Stopwatch.GetTimestamp();
        double elapsed = lastDraw == 0 ? 0 : (now - lastDraw) / (double)Stopwatch.Frequency;
        lastDraw = now;
        timer += elapsed;

        double show = Math.Max(1, AnnouncerConfig.Instance.DurationSeconds);
        if (state == AnimState.Entering)
        {
            y = Lerp(HiddenY, VisibleY, Smooth(timer / EnterSeconds));
            if (timer >= EnterSeconds)
            {
                state = AnimState.Showing;
                timer = 0;
            }
        }
        else if (state == AnimState.Showing)
        {
            y = VisibleY;
            if (timer >= show)
            {
                state = AnimState.Exiting;
                timer = 0;
            }
        }
        else if (state == AnimState.Exiting)
        {
            y = Lerp(VisibleY, HiddenY, Smooth(timer / ExitSeconds));
            if (timer >= ExitSeconds)
                state = AnimState.Hidden;
        }
    }

    static void PlaySound()
    {
        AnnouncerConfig config = AnnouncerConfig.Instance;
        float volume = Math.Clamp(config.SoundVolume, 0, 100) / 100f;
        if (config.Sound == BannerSound.None || volume <= 0)
            return;
        switch (config.Sound)
        {
            case BannerSound.Crimson:
            case BannerSound.Knock:
            case BannerSound.Jungle:
            case BannerSound.Corruption:
                SoundStyle style = new SoundStyle(Util.ModId + "/Assets/Sounds/" + config.Sound) { Volume = volume };
                SoundEngine.PlaySound(style);
                break;
            case BannerSound.MagicDing:
                SoundEngine.PlaySound(SoundID.Item29, -1, -1, 0f, volume);
                break;
            case BannerSound.Coins:
                SoundEngine.PlaySound(SoundID.Coins, -1, -1, 1, volume, 0f);
                break;
            case BannerSound.MaxMana:
                SoundEngine.PlaySound(SoundID.MaxMana, -1, -1, 1, volume, 0f);
                break;
            case BannerSound.ResearchComplete:
                SoundEngine.PlaySound(SoundID.ResearchComplete, -1, -1, 1, volume, 0f);
                break;
            case BannerSound.ChestUnlock:
                SoundEngine.PlaySound(SoundID.Unlock, -1, -1, 1, volume, 0f);
                break;
        }
    }

    static Texture2D? BannerTexture(BannerStyle style)
    {
        if (style is BannerStyle.Classic or BannerStyle.Modern)
            return null;
        if (!textures.TryGetValue(style, out Asset<Texture2D>? asset))
        {
            try
            {
                asset = ModContent.GetInstance<NPCUnlockAnnouncer>().Assets.Request<Texture2D>("Assets/Banners/" + style);
            }
            catch (Exception e)
            {
                Util.LogOnce("texture" + style, "banner texture " + style + " did not load: " + e.Message);
            }
            textures[style] = asset;
        }
        return asset?.Value;
    }

    static bool Head(int type, out Texture2D head)
    {
        head = default;
        try
        {
            int index = NPC.TypeToDefaultHeadIndex(type);
            if (index < 0)
                return false;
            head = TextureAssets.NpcHead[index].Value;
            return !head.IsNull;
        }
        catch (Exception e)
        {
            // Without a head the banner still shows its text.
            Util.LogOnce("head" + type, "no head for NPC " + type + ": " + e.Message);
            return false;
        }
    }

    static float Scale()
    {
        float ui = Math.Clamp(Main.UIScale, 0.75f, 3f);
        return Math.Min(ui, Main.screenWidth * 0.94f / Width);
    }

    static void Fill(SpriteBatch sb, float x, float y, float w, float h, Color color)
    {
        if (w <= 0 || h <= 0)
            return;
        sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle(Round(x), Round(y), Round(w), Round(h)), color);
    }

    static void DrawHead(SpriteBatch sb, int type, float cx, float cy, float scale)
    {
        if (!Head(type, out Texture2D head))
            return;
        sb.Draw(head, new Vector2(cx, cy), null, new Color(255, 255, 255, 255), 0f, new Vector2(head.Width / 2f, head.Height / 2f), scale, SpriteEffects.None, 0f);
    }

    static void DrawText(SpriteBatch sb, string text, float x, float y, Color color, float scale, float room)
    {
        float width = FontAssets.MouseText.Value.MeasureString(text).X * scale;
        float fit = width > room && width > 0 ? scale * room / width : scale;
        Utils.DrawBorderString(sb, text, new Vector2(x, y), color, fit, 0f, 0f, -1);
    }

    static Layout LayoutFor(BannerStyle style) => style switch
    {
        BannerStyle.Modern => LayoutModern,
        BannerStyle.Crimson => LayoutCrimson,
        BannerStyle.Wood => LayoutWood,
        BannerStyle.Jungle => LayoutJungle,
        BannerStyle.Corruption => LayoutCorruption,
        _ => LayoutClassic,
    };

    public static void Draw()
    {
        if (Main.gameMenu)
            return;
        if (current == null && queue.Count == 0)
        {
            lastDraw = 0;
            return;
        }
        Step();
        if (state == AnimState.Hidden)
        {
            current = null;
            if (queue.Count == 0)
                return;
            current = queue.Dequeue();
            state = AnimState.Entering;
            timer = 0;
            y = HiddenY;
            Util.Safe("announcement sound", PlaySound);
        }
        if (current is not Entry entry)
            return;

        SpriteBatch sb = Main.spriteBatch;
        float s = Scale();
        BannerStyle style = AnnouncerConfig.Instance.Style;
        float top = y * s;
        Texture2D? tex = BannerTexture(style);
        float x, w, h;

        if (tex is Texture2D texture)
        {
            w = texture.Width * s;
            h = texture.Height * s;
            x = (Main.screenWidth - w) / 2;
            sb.Draw(texture, new Vector2(x, top), null, new Color(255, 255, 255, 255), 0f, Vector2.Zero, s, SpriteEffects.None, 0f);
            DrawHead(sb, entry.Type, x + 36 * s, top + h / 2, 1.15f * s);
        }
        else if (style == BannerStyle.Modern)
        {
            w = Width * s;
            h = Height * s;
            x = (Main.screenWidth - w) / 2;
            float pulse = MathF.Sin(Main.GlobalTimeWrappedHourly * 4) * 0.5f + 0.5f;
            var neon = new Vector3(Lerp(0, 220, pulse), Lerp(230, 0, pulse), 255);
            float t = Math.Max(1, Round(2 * s));
            Fill(sb, x - 4 * s, top - 4 * s, w + 8 * s, h + 8 * s, Rgb(neon, 0.12f));
            Fill(sb, x - 2 * s, top - 2 * s, w + 4 * s, h + 4 * s, Rgb(neon, 0.25f));
            Fill(sb, x, top, w, h, new Color(12, 16, 26, 220));
            Fill(sb, x, top, w, t, Rgb(neon));
            Fill(sb, x, top + h - t, w, t, Rgb(neon));
            Fill(sb, x, top, t, h, Rgb(neon));
            Fill(sb, x + w - t, top, t, h, Rgb(neon));
            if (Head(entry.Type, out _))
            {
                float bx = x + 14 * s, by = top + 12 * s, bw = 44 * s, bh = 40 * s, line = Math.Max(1, Round(s));
                Fill(sb, bx, by, bw, bh, new Color(22, 30, 44, 255));
                Fill(sb, bx, by, bw, line, Rgb(neon, 0.6f));
                Fill(sb, bx, by + bh - line, bw, line, Rgb(neon, 0.6f));
                Fill(sb, bx, by, line, bh, Rgb(neon, 0.6f));
                Fill(sb, bx + bw - line, by, line, bh, Rgb(neon, 0.6f));
                DrawHead(sb, entry.Type, x + 36 * s, top + 32 * s, 1.1f * s);
            }
        }
        else
        {
            w = Width * s;
            h = Height * s;
            x = (Main.screenWidth - w) / 2;
            float t = Math.Max(1, Round(2 * s));
            Fill(sb, x, top, w, h, new Color(20, 20, 20, 230));
            Fill(sb, x, top, w, t, Rgb(LayoutClassic.Gold));
            Fill(sb, x, top + h - t, w, t, Rgb(LayoutClassic.Gold));
            DrawHead(sb, entry.Type, x + 32 * s, top + 32 * s, 1.2f * s);
        }

        Layout layout = tex != null || style == BannerStyle.Modern ? LayoutFor(style) : LayoutClassic;
        float room = w - (layout.X + 10) * s;
        DrawText(sb, entry.Title.ToUpperInvariant(), x + layout.X * s, top + layout.Title * s, Rgb(layout.Gold), 1.1f * s, room);
        DrawText(sb, entry.Subtitle, x + layout.X * s, top + layout.Sub * s, Rgb(layout.Grey), 0.85f * s, room);
    }
}

public class BannerSystem : ModSystem
{
    public override void OnModLoad()
    {
        On_Main.DrawInterface_33_MouseText += (orig, self) =>
        {
            orig(self);
            Util.Safe("banner", Banner.Draw);
        };
    }

    public override void OnWorldUnload() => Banner.Clear();
}
