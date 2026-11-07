using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using BestiaryDisplaysMod.Content.TileEntities;
using BestiaryDisplaysMod.Lib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static BestiaryDisplaysMod.Common.UI.Native;

namespace BestiaryDisplaysMod.Common.UI;

public sealed class Scroll
{
    static readonly long FrameTicks = Stopwatch.Frequency / 60;

    public float offset;
    float momentum;
    bool active;
    float lastY;
    long lastTime;

    public void Reset()
    {
        offset = 0;
        momentum = 0;
        active = false;
    }

    public void Update(Box area, float content)
    {
        // The original advanced the glide once per frame, tuned for 60 FPS. It follows real time here.
        long now = Stopwatch.GetTimestamp();
        float frames = lastTime == 0 ? 1f : Math.Clamp((now - lastTime) / (float)FrameTicks, 0.25f, 4f);
        lastTime = now;

        float max = Math.Max(0, content - area.h);
        if (Pointer.pressed && Pointer.In(area))
        {
            active = true;
            momentum = 0;
            lastY = Pointer.y;
        }
        if (active && Pointer.down)
        {
            if (Pointer.dragged)
            {
                float delta = lastY - Pointer.y;
                offset += delta;
                momentum = delta;
            }
            lastY = Pointer.y;
        }
        else
        {
            active = false;
            if (Math.Abs(momentum) > 0.5f)
            {
                offset += momentum * frames;
                momentum *= MathF.Pow(0.9f, frames);
            }
            else
            {
                momentum = 0;
            }
        }
        offset = Math.Clamp(offset, 0, max);
    }
}

public sealed class Stepper
{
    public readonly string key;
    public readonly Func<double> get;
    public readonly Action<double> set;
    public readonly double step;
    public readonly Func<double, string> format;
    public readonly Func<string, double?> parse;
    public readonly Func<double, double>? snap;
    public readonly TextField field = new();
    public Box? rect;

    public Stepper(string key, Func<double> get, Action<double> set, double step, Func<double, string> format, Func<string, double?> parse, Func<double, double>? snap = null)
    {
        this.key = key;
        this.get = get;
        this.set = set;
        this.step = step;
        this.format = format;
        this.parse = parse;
        this.snap = snap;
    }

    public double Stepped(double d)
    {
        double v = get() + d;
        return snap != null ? snap(v) : v;
    }
}

public static class HologramMenu
{
    const float CloseDistance = 400;
    const float W = 600, H = 384;
    const float LEFT = 14, TOP = 50, PREVIEW_W = 180, PREVIEW_H = 270;
    const float COL_X = 208, COL_W = W - COL_X - 14;
    const float ROW_H = 34, ROW_STEP = 40, GAP = 8;
    const float CELL = 52, CELL_GAP = 6;

    static readonly Color GREEN = D.Rgb(120, 255, 120), GREY = D.Rgb(170, 170, 185), GOLD = D.Rgb(255, 215, 90);

    static HologramTileEntity? entity;
    static BaseHologramProjectorTile? tile;
    static bool picker, installed;
    static readonly Scroll scroll = new();
    static readonly TextField search = new();
    static Box? searchRect;
    static List<Creature>? list;
    static Creature? hover;
    static PreviewState? preview;
    static Stepper[]? steppers;
    static readonly List<Action> late = new();
    static float k = 1, ox, oy;

    sealed class PreviewState
    {
        public readonly int netId;
        public readonly Bestiary.Dummy dummy;
        public long last = -1;

        public PreviewState(int netId)
        {
            this.netId = netId;
            dummy = new Bestiary.Dummy(netId);
        }
    }

    public static bool IsOpen => entity != null;

    static string L(string key) => Util.T("UI." + key);

    static string OnOff(bool on) => on ? "ON" : "OFF";

    static void BoxPanel(float x, float y, float w, float h, bool over) =>
        D.Panel(x, y, w, h, over ? D.ButtonHover : D.Button, over ? D.Yellow : D.ButtonBorder, 2);

    public static void Open(HologramTileEntity target, BaseHologramProjectorTile from)
    {
        bool was = IsOpen;
        entity = target;
        tile = from;
        picker = false;
        list = null;
        hover = null;
        search.text = "";
        search.Blur();
        Pointer.Disarm();
        if (!was)
            Util.PlaySound("OpenGui");
    }

    public static void Close()
    {
        entity = null;
        tile = null;
        picker = false;
        search.Blur();
        if (steppers != null)
        {
            foreach (Stepper st in steppers)
                st.field.Blur();
        }
        preview?.dummy.Release();
        preview = null;
    }

    static void Change(Action fn)
    {
        HologramTileEntity? e = entity;
        if (e == null)
            return;
        fn();
        e.Changed();
    }

    static readonly Regex FloatPrefix = new(@"^\s*[+-]?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?");
    static readonly Regex IntPrefix = new(@"^\s*[+-]?\d+");

    // parseFloat and parseInt read what a text starts with, and ignore the rest.
    static double? ParseFloat(string text)
    {
        Match m = FloatPrefix.Match(text);
        return m.Success && double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && double.IsFinite(v) ? v : null;
    }

    static double? ParseInt(string text)
    {
        Match m = IntPrefix.Match(text);
        return m.Success && double.TryParse(m.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : null;
    }

    static string Whole(double v) => ((long)v).ToString(CultureInfo.InvariantCulture);

    static Stepper[] Steppers()
    {
        return steppers ??= new[]
        {
            new Stepper("Scale", static () => entity!.Scale, static v => entity!.Scale = (float)Util.Clamp(v, tile!.MinScale, tile.MaxScale),
                0.25, static v => v.ToString("F2", CultureInfo.InvariantCulture) + "x", ParseFloat, static v => Util.Round(v / 0.25) * 0.25),
            new Stepper("OffsetX", static () => entity!.OffsetX, static v => entity!.OffsetX = (int)Util.Clamp(Util.Round(v), -tile!.MaxOffset, tile.MaxOffset), 4, Whole, ParseInt),
            new Stepper("OffsetY", static () => entity!.OffsetY, static v => entity!.OffsetY = (int)Util.Clamp(Util.Round(v), -tile!.MaxOffset, tile.MaxOffset), 4, Whole, ParseInt),
            new Stepper("Rotation", static () => entity!.Rotation, static v => entity!.Rotation = ((Util.Round(v) % 360) + 360) % 360, 15, static v => Util.Round(v) + "°", ParseFloat),
            new Stepper("AnimSpeed", static () => entity!.AnimationSpeed, static v => entity!.AnimationSpeed = (int)Util.Clamp(Util.Round(v), 1, 40), 1, Whole, ParseInt),
        };
    }

    public static void Install()
    {
        if (installed)
            return;
        installed = true;
        On_Main.DrawInterface_33_MouseText += static (orig, self) =>
        {
            orig(self);
            if (IsOpen)
                Util.Safe("projector menu", Draw);
        };
    }

    static bool StillValid()
    {
        HologramTileEntity? e = entity;
        if (e == null || Main.gameMenu || !e.StillPlaced)
            return false;
        Vector2 c = Main.LocalPlayer.Center;
        float dx = c.X - (e.Position.X + 1.5f) * 16, dy = c.Y - (e.Position.Y + 1f) * 16;
        return Math.Sqrt(dx * dx + dy * dy) <= CloseDistance;
    }

    static Box R(float x, float y, float w, float h) => new(ox + x * k, oy + y * k, w * k, h * k);

    static void Label(string text, Box r, Color color, float scale = 0.9f, bool left = false)
    {
        float size = k * scale;
        late.Add(() =>
        {
            string shown = D.Truncate(text, r.w - 10 * k, size);
            Vector2 measured = D.Measure(shown, size);
            float x = left ? r.x + 8 * k : r.x + (r.w - measured.X) / 2;
            D.Text(shown, x, r.y + (r.h - measured.Y) / 2 + 3 * size, color, size);
        });
    }

    static bool Button(string text, Box r, bool selected = false, Color? color = null, float scale = 0.9f)
    {
        ClickState state = Clicked(r);
        bool over = state.over || state.hot;
        if (!Panel(selected ? PanelStyle.CellSelected : PanelStyle.Cell, r.x, r.y, r.w, r.h, over))
            BoxPanel(r.x, r.y, r.w, r.h, over || selected);
        Label(text, r, color ?? D.White, scale);
        if (state.clicked)
        {
            Util.PlaySound("ButtonPress");
            return true;
        }
        return false;
    }

    static void Draw()
    {
        if (!StillValid())
        {
            Close();
            return;
        }
        Box screen = Screen();
        k = Math.Min(1.25f, Math.Min((screen.w - 16) / W, (screen.h - 16) / H));
        ox = Util.Round(screen.x + (screen.w - W * k) / 2);
        oy = Util.Round(screen.y + (screen.h - H * k) / 2);
        late.Clear();
        Pointer.Update();

        Box win = R(0, 0, W, H);
        Region(win);
        if (Pointer.pressed)
            BlurOutside();
        if (!Panel(PanelStyle.Window, win.x, win.y, win.w, win.h, false))
            D.Panel(win.x, win.y, win.w, win.h, D.Rgb(33, 43, 79, 240), D.PanelBorder, 2);

        Label(TitleText(), R(LEFT, 8, W - LEFT * 2 - 44, 34), GOLD, 1f, true);
        if (Button("X", R(W - 14 - 38, 8, 38, 32)))
        {
            Close();
            return;
        }

        DrawPreview();
        if (picker)
            DrawPicker();
        else
            DrawSettings();

        Flush();
        foreach (Action fn in late)
            Util.Safe("menu text", fn);
    }

    static string TitleText()
    {
        string key = "Items." + (tile != null ? tile.ItemName : "HologramProjectorItem") + ".DisplayName";
        string text = Util.T(key);
        return text.StartsWith("Mods.", StringComparison.Ordinal) ? L("SelectCreature") : text;
    }

    static void BlurOutside()
    {
        foreach (Stepper st in Steppers())
        {
            if (st.field.focused && (st.rect == null || !Pointer.In(st.rect.Value)))
                st.field.Blur();
        }
        if (search.focused && (searchRect == null || !Pointer.In(searchRect.Value)))
            search.Blur();
    }

    static void DrawPreview()
    {
        HologramTileEntity e = entity!;
        Box box = R(LEFT, TOP, PREVIEW_W, PREVIEW_H);
        if (!Panel(PanelStyle.Image, box.x, box.y, box.w, box.h, false))
            D.Panel(box.x, box.y, box.w, box.h, D.Rgb(20, 26, 50, 230), D.PanelBorder, 2);
        int? netId = picker && hover != null ? hover.id : Bestiary.NetIdOf(e.BestiaryCreditId);
        Box nameRect = R(LEFT, TOP + PREVIEW_H - 34, PREVIEW_W, 30);
        if (netId == null)
        {
            Label(L("None"), nameRect, GREY, 0.85f);
        }
        else
        {
            int id = netId.Value;
            Box area = R(LEFT + 8, TOP + 8, PREVIEW_W - 16, PREVIEW_H - 50);
            late.Add(() => DrawCreature(id, area));
            Label(picker && hover != null ? hover.name : Bestiary.DisplayName(e.BestiaryCreditId), nameRect, D.White, 0.85f);
        }
        if (e.TileInvisible)
        {
            float size = 0.7f * k;
            Box area = R(LEFT, TOP + PREVIEW_H + 6, PREVIEW_W, 60);
            late.Add(() =>
            {
                float y = area.y;
                List<string> lines = D.Wrap(L("InvisibleWarning"), area.w, size);
                for (int i = 0; i < lines.Count && i < 3; i++)
                {
                    D.Text(lines[i], area.x, y, D.Rgb(255, 210, 120), size);
                    y += 18 * k;
                }
            });
        }
    }

    static void DrawCreature(int netId, Box r)
    {
        Texture2D tex = Bestiary.Texture(netId);
        if (tex.IsNull)
            return;
        if (preview == null || preview.netId != netId)
        {
            preview?.dummy.Release();
            preview = new PreviewState(netId);
        }
        long now = Main.GameUpdateCount;
        if (now - preview.last >= 6)
        {
            preview.dummy.Advance();
            preview.last = now;
        }
        Rectangle? f = preview.dummy.Frame;
        int fh = Math.Max(1, tex.Height / Bestiary.FrameCount(netId));
        Rectangle src = f is { Width: > 0, Height: > 0 } ? f.Value : new Rectangle(0, 0, tex.Width, fh);
        float scale = Math.Min(Math.Min(r.w / src.Width, r.h / src.Height), 3 * k);
        Main.spriteBatch.Draw(tex, new Vector2(r.x + r.w / 2, r.y + r.h / 2), src, D.White, 0f, new Vector2(src.Width / 2f, src.Height / 2f), scale, SpriteEffects.None, 0f);
    }

    static float RowY(int i) => TOP + i * ROW_STEP;

    static void Toggle(string label, bool on, float x, int i, Action flip)
    {
        if (Button(label + ": " + OnOff(on), R(x, RowY(i), (COL_W - GAP) / 2, ROW_H), on, on ? GREEN : GREY, 0.85f))
            Change(flip);
    }

    static void DrawSettings()
    {
        HologramTileEntity e = entity!;
        float half = (COL_W - GAP) / 2;
        string name = e.BestiaryCreditId.Length > 0 ? Bestiary.DisplayName(e.BestiaryCreditId) : L("None");
        if (Button(L("SelectCreature") + ": " + name, R(COL_X, RowY(0), COL_W, ROW_H)))
            OpenPicker();
        Toggle(L("Animate"), e.Animate, COL_X, 1, () => e.Animate = !e.Animate);
        Toggle(L("Hologram"), e.HologramFx, COL_X + half + GAP, 1, () => e.HologramFx = !e.HologramFx);
        Toggle(L("AltRender"), e.AlternateRender, COL_X, 2, () => e.AlternateRender = !e.AlternateRender);
        Toggle(L("InvisibleTile"), e.TileInvisible, COL_X + half + GAP, 2, () => e.TileInvisible = !e.TileInvisible);
        Stepper[] all = Steppers();
        for (int i = 0; i < all.Length; i++)
            DrawStepper(all[i], RowY(3 + i));
    }

    static void DrawStepper(Stepper st, float y)
    {
        const float bw = 44;
        if (Button("-", R(COL_X, y, bw, ROW_H), scale: 1.1f))
            Change(() => st.set(st.Stepped(-st.step)));
        if (Button("+", R(COL_X + COL_W - bw, y, bw, ROW_H), scale: 1.1f))
            Change(() => st.set(st.Stepped(st.step)));
        Box r = R(COL_X + bw + 6, y, COL_W - 2 * bw - 12, ROW_H);
        st.rect = r;
        ClickState state = Clicked(r);
        if (state.clicked && !st.field.focused && st.field.Supported)
        {
            Util.PlaySound("ButtonPress");
            st.field.text = "";
            st.field.Focus();
        }
        DrawBar(r, st.field.focused || state.over);
        string label = L(st.key) + ": " + st.format(st.get());
        if (st.field.focused)
        {
            string? result = st.field.Update(r.x, r.y, r.w, r.h);
            if (result == "enter")
            {
                double? v = st.parse(st.field.text);
                if (v != null)
                    Change(() => st.set(v.Value));
            }
            string caret = Main.GameUpdateCount / 20 % 2 == 0 ? "|" : "";
            label = st.field.text + caret;
            if (label.Length == 0)
                label = "_";
        }
        Label(label, r, st.field.focused ? GOLD : D.White, 0.85f);
    }

    static void DrawBar(Box r, bool over)
    {
        if (!Panel(PanelStyle.Bar, r.x, r.y, r.w, r.h, over))
            D.Panel(r.x, r.y, r.w, r.h, over ? D.FieldActive : D.Field, over ? D.Yellow : D.PanelBorder, 2);
    }

    static void OpenPicker()
    {
        picker = true;
        scroll.Reset();
        search.text = "";
        search.Blur();
        list = null;
        hover = null;
    }

    static string shownQuery = "";
    static List<Creature>? shownList, filtered;

    static List<Creature> Filtered()
    {
        list ??= Bestiary.Creatures(tile != null && tile.IsAdvanced);
        string q = search.text.Trim().ToLowerInvariant();
        if (q.Length == 0)
            return list;
        if (filtered == null || q != shownQuery || shownList != list)
        {
            filtered = list.FindAll(c => c.name.ToLowerInvariant().Contains(q));
            shownQuery = q;
            shownList = list;
        }
        return filtered;
    }

    static void DrawPicker()
    {
        if (Button(L("Back"), R(COL_X, TOP, 110, ROW_H)))
        {
            picker = false;
            search.Blur();
            hover = null;
            return;
        }
        DrawSearch(R(COL_X + 118, TOP, COL_W - 118, ROW_H));

        List<Creature> shown = Filtered();
        Box area = R(COL_X, TOP + ROW_STEP + 4, COL_W, H - (TOP + ROW_STEP + 4) - 16);
        Region(area);
        if (shown.Count == 0)
        {
            Label(L("Empty"), area, GREY, 0.85f);
            return;
        }
        float cell = CELL * k, gap = CELL_GAP * k;
        int cols = Math.Max(1, (int)Math.Floor((area.w + gap) / (cell + gap)));
        float left = area.x + (area.w - (cols * cell + (cols - 1) * gap)) / 2;
        int rows = (int)Math.Ceiling(shown.Count / (double)cols);
        scroll.Update(area, rows * (cell + gap) - gap);
        int first = Math.Max(0, (int)Math.Floor(scroll.offset / (cell + gap)) * cols);
        string selected = entity!.BestiaryCreditId;
        for (int n = first; n < shown.Count; n++)
        {
            int row = n / cols, col = n % cols;
            float y = area.y + row * (cell + gap) - scroll.offset;
            if (y + cell > area.y + area.h)
                break;
            if (y < area.y)
                continue;
            Creature c = shown[n];
            var r = new Box(left + col * (cell + gap), y, cell, cell);
            ClickState state = Clicked(r);
            bool isSelected = c.credit == selected;
            bool over = state.over || state.hot;
            if (!Panel(isSelected ? PanelStyle.CellSelected : PanelStyle.Cell, r.x, r.y, r.w, r.h, over))
                BoxPanel(r.x, r.y, r.w, r.h, over || isSelected);
            late.Add(() => Util.Safe("icon", () => DrawIcon(c.id, r)));
            if (state.hot)
                hover = c;
            if (state.clicked)
            {
                Util.PlaySound("SelectCreature");
                hover = c;
                Change(() => entity!.BestiaryCreditId = c.credit);
            }
        }
    }

    static void DrawSearch(Box r)
    {
        TextField field = search;
        searchRect = r;
        ClickState state = Clicked(r);
        if (state.clicked && !field.focused && field.Supported)
        {
            Util.PlaySound("ButtonPress");
            field.Focus();
        }
        DrawBar(r, field.focused || state.over);
        string? result = field.Update(r.x, r.y, r.w, r.h);
        if (result == "changed" || result == "escape")
            scroll.Reset();
        string caret = field.focused && Main.GameUpdateCount / 20 % 2 == 0 ? "|" : "";
        if (field.text.Length > 0 || field.focused)
            Label(field.text + caret, r, D.White, 0.85f, true);
        else
            Label(L("SearchHint"), r, GREY, 0.85f, true);
    }

    static void DrawIcon(int netId, Box r)
    {
        Texture2D tex = Bestiary.Texture(netId);
        if (tex.IsNull)
            return;
        int fh = Math.Max(1, tex.Height / Bestiary.FrameCount(netId));
        float pad = 5 * k;
        float scale = Math.Min(Math.Min((r.w - pad * 2) / tex.Width, (r.h - pad * 2) / fh), 2 * k);
        Main.spriteBatch.Draw(tex, new Vector2(r.x + r.w / 2, r.y + r.h / 2), new Rectangle(0, 0, tex.Width, fh), D.White, 0f, new Vector2(tex.Width / 2f, fh / 2f), scale, SpriteEffects.None, 0f);
    }
}
