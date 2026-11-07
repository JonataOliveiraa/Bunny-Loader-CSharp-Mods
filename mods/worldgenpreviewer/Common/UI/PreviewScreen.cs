using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using WorldGenPreviewerMod.Common.Gen;
using static WorldGenPreviewerMod.Common.UI.Widgets;

namespace WorldGenPreviewerMod.Common.UI;

public static class ScreenLayout
{
    public static bool listHidden;
    public static double scrolledAt = -1e9;
}

public class DragList : UIList
{
    bool block;

    public override bool ContainsPoint(Vector2 point) => !block && base.ContainsPoint(point);

    public override void Update(GameTime gameTime)
    {
        Vector2 press = new(Pointer.pressX, Pointer.pressY);
        block = Pointer.down && !base.ContainsPoint(press);
        try
        {
            base.Update(gameTime);
        }
        finally
        {
            block = false;
        }
        if (Pointer.down && base.ContainsPoint(press))
            ScreenLayout.scrolledAt = Util.NowMs;
    }

    public int IndexOfStep(int index)
    {
        for (int k = 0; k < _items.Count; k++)
        {
            if (_items[k] is StepRow row && row.index == index)
                return k;
        }
        return -1;
    }
}

public class StepRow : UIElement
{
    public readonly int index;
    readonly float scale;
    bool pressed;

    public StepRow(int index, float height, float scale)
    {
        this.index = index;
        this.scale = scale;
        Width.Set(0, 1);
        Height.Set(height, 0);
    }

    float SkipWidth => GetDimensions().Height * 1.1f;

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        pressed = true;
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        pressed = false;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        CalculatedStyle d = GetDimensions();
        if (State.StatusOf(index) == Status.Pending && evt.MousePosition.X < d.X + SkipWidth)
            Commands.ToggleSkip(index);
        else if (Control.canWait)
            Commands.PauseAfter(index);
    }

    Color RowColor()
    {
        int s = State.StatusOf(index);
        if (s == Status.Done)
            return Colors.Done;
        if (s == Status.Skipped || s == Status.Cancelled)
            return Colors.Skipped;
        if (State.IsSkipped(index))
            return Colors.Marked;
        return Color.White;
    }

    string Tip()
    {
        int s = State.StatusOf(index);
        if (s == Status.Skipped || s == Status.Cancelled)
            return Tr("Skipped");
        if (s == Status.Done)
            return Tr("Complete");
        if (Pointer.pressX < GetDimensions().X + SkipWidth)
            return Tr(State.IsSkipped(index) ? "KeepStep" : "SkipStep");
        return Control.canWait ? Tr("AdvanceTo", PassNames.Label(index)) : PassNames.Label(index);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        int s = State.StatusOf(index);
        if (s == Status.Running)
            Fill(spriteBatch, d.X, d.Y, d.Width, d.Height, Colors.Running);
        if (pressed && !Pointer.moved)
        {
            Fill(spriteBatch, d.X, d.Y, d.Width, d.Height, Colors.Running);
            Hint.text = Tip();
        }
        float skip = SkipWidth;
        if (s == Status.Pending)
        {
            float h = Util.Round(d.Height * 0.45f), w = Util.Round(h * 15 / 14);
            Image(spriteBatch, Icon("closeButton"), d.X + (skip - w) / 2, d.Y + (d.Height - h) / 2, w, h, State.IsSkipped(index) ? Colors.Gold : Color.White);
        }
        string name = PassNames.Label(index);
        int repeats = State.RepeatsOf(index);
        if (repeats > 1)
            name += " (x" + repeats + ")";
        float textScale = Math.Min(scale, (d.Width - skip - 4) / Math.Max(1, Widgets.Width(name, 1)));
        Text(spriteBatch, name, d.X + skip, d.Y + (d.Height - 22 * textScale) / 2, RowColor(), textScale);
    }
}

public class PreviewScreen : UIState
{
    const double AUTO_SCROLL_MS = 2500;

    (int sw, int sh, bool hidden, int worldW, int worldH) key;
    (int session, int count) rowsKey = (-1, -1);
    MapView? map;
    DragList? list;
    float rowH, rowScale;

    static float Clamp(float v, float lo, float hi) => v < lo ? lo : v > hi ? hi : v;

    IconButton Button(string icon, Func<string> tip, Action tap, Func<bool> enabled, Func<bool> on) => new(icon, tip, tap, enabled, on);

    void Rebuild()
    {
        int SW = Main.screenWidth, SH = Main.screenHeight;
        bool hidden = ScreenLayout.listHidden || View.zoomed;
        int worldW = Main.maxTilesX, worldH = Main.maxTilesY;
        var newKey = (SW, SH, hidden, worldW, worldH);
        if (newKey == key)
            return;
        key = newKey;
        rowsKey = (-1, -1);
        RemoveAllChildren();

        float U = Clamp(SH / 720f, 1, 2.2f);
        float pad = Util.Round(10 * U);
        float listW = hidden ? 0 : Util.Round(Math.Min(SW * 0.3f, 320 * U));
        float contentW = SW - listW;

        Append(Place(new Box(Colors.Background), 0, 0, SW, SH));

        const int N = 7;
        float gap = Util.Round(6 * U);
        float B = Util.Round(Math.Min(Math.Max(36 * U, SH * 0.075f), (contentW - pad * 2 - (N + 2) * gap) / N));
        float statusH = Util.Round(22 * U);
        float buttonsY = pad;
        float progressH = GameProgress.ProgressHeight(U);
        float top = buttonsY + B + statusH;
        float room = SH - pad - progressH - gap - top;
        float frameW = contentW - pad * 2;
        float mapH = worldW > 0 && worldH > 0 ? Math.Min(room, Util.Round((frameW - 4) * worldH / worldW) + 4) : room;
        float y = top + Util.Round((room - mapH) / 2);
        Box frame = Place(new Box(Colors.Edge, Colors.PanelLight), pad, y, frameW, mapH);
        frame.SetPadding(2);
        frame.OverflowHidden = true;
        Append(frame);
        map ??= new MapView();
        map.Width.Set(0, 1);
        map.Height.Set(0, 1);
        frame.Append(map);
        Append(Place(new GameProgress(U), pad, y + mapH + gap, frameW, progressH));

        string previous = Tr("Previous"), resume = Tr("Resume"), pause = Tr("Pause"), next = Tr("Next");
        string noPause = Tr("NoPause"), showSteps = Tr("ShowSteps"), structures = Tr("Structures"), cancel = Tr("Cancel");
        Func<string> Timed(string tip) => () => Control.canWait ? tip : noPause;
        IconButton[] buttons =
        {
            Button("menu", () => showSteps, () => ScreenLayout.listHidden = !ScreenLayout.listHidden, () => !View.zoomed, () => !ScreenLayout.listHidden && !View.zoomed),
            Button("previous", Timed(previous), Commands.Previous, () => Control.canWait && State.lastApplied >= 0, () => false),
            Button("play", Timed(resume), Commands.Play, () => Control.canWait, () => Control.canWait && State.continueGen && !State.paused && !State.cancel),
            Button("pause", Timed(pause), Commands.Pause, () => Control.canWait, () => State.paused),
            Button("next", Timed(next), Commands.Next, () => Control.canWait, () => false),
            Button("structure", () => structures, Commands.ToggleStructures, () => true, () => State.showStructures),
            Button("cancel", () => cancel, Commands.Cancel, () => !State.cancel, () => false),
        };
        float rowWidth = N * B + (N - 1) * gap;
        float x = Util.Round((contentW - rowWidth) / 2);
        foreach (IconButton button in buttons)
        {
            Append(Place(button, x, buttonsY, B, B));
            x += B + gap;
        }
        Append(Place(new DynamicText(() => Hint.shown ?? Tr(State.statusKey, State.statusArgs), Colors.Status, 0.9f * U, 0.5f), pad, buttonsY + B, contentW - pad * 2, statusH));

        list = null;
        if (listW > 0)
        {
            Box panel = Place(new Box(Colors.Panel), contentW, 0, listW, SH);
            Append(panel);
            float titleH = Util.Round(34 * U);
            panel.Append(Place(new DynamicText(() => Tr("Steps"), Colors.Gold, 1.0f * U, 0.5f), 0, Util.Round(6 * U), listW, titleH));
            rowH = Util.Round(Math.Max(22 * U, SH * 0.05f));
            rowScale = Math.Min(rowH / 30, 0.9f * U);
            DragList steps = Place(new DragList(), Util.Round(6 * U), titleH + Util.Round(8 * U), listW - Util.Round(12 * U), SH - titleH - Util.Round(14 * U));
            steps.ListPadding = 0;
            steps.ManualSortMethod = _ => { };
            panel.Append(steps);
            list = steps;
        }
        Recalculate();
    }

    void Rows()
    {
        if (list == null)
            return;
        var newKey = (State.session, State.names.Length);
        if (newKey != rowsKey)
        {
            rowsKey = newKey;
            list.Clear();
            var rows = new List<UIElement>();
            foreach (int i in State.VisibleIndices())
                rows.Add(new StepRow(i, rowH, rowScale));
            list.AddRange(rows);
        }
        if (Util.NowMs - ScreenLayout.scrolledAt > AUTO_SCROLL_MS)
        {
            int k = list.IndexOfStep(State.index);
            if (k >= 0)
                list.ViewPosition = k * rowH - list.GetInnerDimensions().Height / 2 + rowH / 2;
        }
    }

    public override void Update(GameTime gameTime)
    {
        Rebuild();
        Rows();
        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        Hint.shown = Hint.text;
        Hint.text = null;
        base.Draw(spriteBatch);
    }
}
