using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TeleportKeeperMod.Common.Systems;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using static TeleportKeeperMod.Common.UI.Widgets;
using static TeleportKeeperMod.Common.Util;

namespace TeleportKeeperMod.Common.UI;

public abstract class TeleportState : UIState
{
    public readonly TeleportKeeperSystem owner;
    public UIPanel? panel;

    protected TeleportState(TeleportKeeperSystem owner) => this.owner = owner;

    protected abstract TextBox? Box { get; }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        Press.x = evt.MousePosition.X;
        Press.y = evt.MousePosition.Y;
        Press.target = evt.Target;
        TextBox? box = Box;
        if (box == null)
            return;
        bool inside = false;
        for (UIElement? e = evt.Target; e != null; e = e.Parent)
        {
            if (e == box)
                inside = true;
        }
        if (!inside)
            box.Blur();
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (Main.mouseLeft && panel != null && panel.ContainsPoint(new Vector2(Main.mouseX, Main.mouseY)))
            Screen.HideMouseOverInteractions();
    }

    protected UIPanel Window(float width, float height, string title)
    {
        RemoveAllChildren();
        Append(new Backdrop(owner.Close));
        var p = panel = new UIPanel();
        p.BackgroundColor = Colors.Panel;
        p.BorderColor = Colors.Black;
        p.SetPadding(12);
        p.HAlign = 0.5f;
        p.VAlign = 0.5f;
        Place(p, 0, 0, width, height);
        UIText t = Label(title, 1.1f, Colors.Gold);
        t.HAlign = 0.5f;
        p.Append(t);
        Append(p);
        return p;
    }
}

public class EditorUI : TeleportState
{
    TeleportPoint? point;
    string icon = "Default";
    string color = "White";
    bool deleteOnUse;
    UIState? back;

    public TextBox? nameBox;
    UIText? iconLabel, colorLabel;

    string? shownIcon, shownColor;
    int shownVersion = -1;

    public EditorUI(TeleportKeeperSystem owner) : base(owner) { }

    protected override TextBox? Box => nameBox;

    public void Begin(TeleportPoint? point, string icon, UIState? back = null)
    {
        this.point = point;
        this.back = back;
        this.icon = point != null ? point.icon : icon;
        deleteOnUse = point != null && point.deleteOnUse;
        color = point != null ? point.color : "White";
        shownIcon = shownColor = null;
        Build();
        nameBox!.Text = point != null ? point.name : "";
    }

    void Build()
    {
        bool editing = point != null;
        UIPanel p = Window(440, 392, L(editing ? "UI.EditTitle" : "UI.CreateTitle"));
        p.Append(Place(Label(L("UI.Name"), 0.9f), 0, 32));
        nameBox = Place(new TextBox(""), 0, 54, null, 40);
        nameBox.Width.Set(0, 1);
        p.Append(nameBox);

        iconLabel = Place(Label("", 0.9f), 0, 102);
        p.Append(iconLabel);
        const float size = 34, gap = 4;
        const int perRow = 11;
        for (int i = 0; i < Icons.ICONS.Length; i++)
        {
            var b = new IconButton(Icons.ICONS[i].key, size, k => k == icon, k => icon = k);
            Place(b, i % perRow * (size + gap), 126 + i / perRow * (size + gap));
            p.Append(b);
        }

        colorLabel = Place(Label("", 0.9f), 0, 204);
        p.Append(colorLabel);
        for (int i = 0; i < NameColors.NAME_COLORS.Length; i++)
        {
            string key = NameColors.NAME_COLORS[i].key;
            var b = new ColorButton(key, NameColors.NameColor(key), 34, k => k == color, k => color = k);
            Place(b, i * (34 + 4), 228);
            p.Append(b);
        }

        CheckBox check = Place(new CheckBox(L("UI.DeleteOnUse"), () => deleteOnUse, v => deleteOnUse = v), 0, 272, null, 30);
        check.Width.Set(0, 1);
        p.Append(check);

        var buttons = new List<(string key, Action<Button> fn, bool danger)> { ("UI.Save", _ => Save(), false) };
        if (editing)
            buttons.Add(("UI.Delete", Delete, true));
        buttons.Add(("UI.Cancel", _ => Cancel(), false));
        float w = (416f - (buttons.Count - 1) * 8) / buttons.Count;
        for (int i = 0; i < buttons.Count; i++)
            p.Append(Place(new Button(L(buttons[i].key), buttons[i].fn, buttons[i].danger), i * (w + 8), 320, w, 40));
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (nameBox == null)
            return;
        // The texts only change with the choices or the teleports, so they are not asked for every frame.
        if (icon != shownIcon || Data.version != shownVersion)
            nameBox.placeholder = point != null ? point.name : Teleports.DefaultName(icon);
        if (icon != shownIcon)
            iconLabel!.SetText(L("UI.Icon") + ": " + Icons.IconName(icon));
        if (color != shownColor)
        {
            colorLabel!.SetText(L("UI.Color") + ": " + NameColors.NameColorName(color));
            nameBox.textColor = NameColors.NameColor(color);
        }
        shownIcon = icon;
        shownColor = color;
        shownVersion = Data.version;
    }

    void Save()
    {
        nameBox!.Blur();
        string name = nameBox.Text.Trim();
        if (point != null)
        {
            Teleports.Edit(point.id, name.Length > 0 ? name : point.name, icon, deleteOnUse, color);
            Say("TpUpdated", 50, 255, 130);
        }
        else
        {
            Player p = Local;
            if (p.IsNull)
                return;
            Teleports.Add(name.Length > 0 ? name : Teleports.DefaultName(icon), icon, deleteOnUse, p.position.X / 16f, p.position.Y / 16f, color);
            Say("NewTpAdded", 50, 255, 130);
        }
        Leave();
    }

    void Delete(Button button)
    {
        if (!button.confirm)
        {
            button.confirm = true;
            button.SetText(L("UI.Confirm"));
            return;
        }
        Teleports.Delete(point!.id);
        Say("RemovedTp", 255, 100, 100);
        Leave();
    }

    void Cancel()
    {
        nameBox!.Blur();
        Leave();
    }

    void Leave()
    {
        if (back != null)
            owner.OpenList(false);
        else
            owner.Close();
    }
}

public class Row : UIPanel
{
    readonly ListUI list;
    readonly TeleportPoint? point;
    readonly DeathPoint? deathPoint;

    public Row(ListUI list, TeleportPoint? point, DeathPoint? deathPoint)
    {
        this.list = list;
        this.point = point;
        this.deathPoint = deathPoint;
        SetPadding(6);
        Width.Set(0, 1);
        Height.Set(52, 0);
        bool death = deathPoint != null;
        float left = death ? 48 : 84;
        string title = death ? L("UI.DeathOf", deathPoint!.owner) : point!.name;
        Color titleColor = death ? new Color(255, 120, 120, 255) : NameColors.NameColor(point!.color);
        Append(Place(Label(title, 1f, titleColor), left, 2));
        string hint = death ? L("UI.DeathHint") : Icons.IconName(point!.icon) + (point.deleteOnUse ? " · " + L("UI.DeleteOnUseShort") : "");
        Append(Place(Label(hint, 0.75f, Colors.Gray), left, 24));
        var del = new Button(L("UI.Delete"), Delete, true, 0.8f);
        Place(del, null, 0, 78, 40);
        del.HAlign = 1;
        Append(del);
        if (!death)
        {
            var edit = new Button(L("UI.Edit"), _ => list.owner.OpenEdit(point!), false, 0.8f);
            Place(edit, -84, 0, 70, 40);
            edit.HAlign = 1;
            Append(edit);
        }
        var go = new Button(L("UI.Go"), _ => Go(), false, 0.8f);
        Place(go, death ? -84 : -160, 0, 58, 40);
        go.HAlign = 1;
        Append(go);
        if (!death)
        {
            var star = new Chip(() => Icons.Star(point!.favorite), 32, 32, static () => false, () => Teleports.Favorite(point!.id, !point.favorite));
            Place(star, 44, 0);
            star.VAlign = 0.5f;
            Append(star);
        }
    }

    void Go()
    {
        list.owner.Close();
        if (deathPoint != null)
            Teleports.UseDeath(deathPoint);
        else
            Teleports.UsePoint(point!);
    }

    void Delete(Button button)
    {
        if (!button.confirm)
        {
            button.confirm = true;
            button.SetText(L("UI.Confirm"));
            return;
        }
        if (deathPoint != null)
        {
            Teleports.DeleteDeath(deathPoint.owner);
            Say("DeathTpRemoved", 255, 0, 0);
        }
        else
        {
            Teleports.Delete(point!.id);
            Say("RemovedTp", 255, 100, 100);
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        BackgroundColor = Colors.Inner;
        BorderColor = Colors.Black;
        base.DrawSelf(spriteBatch);
        CalculatedStyle d = GetInnerDimensions();
        Texture2D tex = deathPoint != null ? Icons.DeathCrystal() : Icons.IconTexture(point!.icon);
        DrawFitted(tex, d.X, d.Y, 40, d.Height, 34);
    }
}

public class ListUI : TeleportState
{
    const string All = "all", Fav = "fav";

    public TextBox? search;
    public DragList? list;

    string filter = All;
    UIElement? filters;
    UIText? empty;

    string? chips, shownText, shownFilter;
    int shownVersion = -1;

    public ListUI(TeleportKeeperSystem owner) : base(owner) { }

    protected override TextBox? Box => search;

    public void Begin()
    {
        UIPanel p = Window(500, 420, L("UI.ListTitle"));
        Button close = Place(new Button("X", _ => owner.Close(), true, 0.9f), null, -4, 36, 32);
        close.HAlign = 1;
        p.Append(close);
        search = Place(new TextBox(L("UI.Search"), (_, _) => Refresh(), 30), 0, 34, null, 40);
        search.Width.Set(-110, 1);
        p.Append(search);
        Button add = Place(new Button(L("UI.New"), _ => owner.OpenCreate(true), false, 0.9f), null, 34, 100, 40);
        add.HAlign = 1;
        p.Append(add);
        filters = Place(new UIElement(), 0, 80, null, 32);
        filters.Width.Set(0, 1);
        p.Append(filters);
        empty = Place(Label("", 0.9f, Colors.Gray), 0, 126);
        p.Append(empty);
        list = Place(new DragList(), 0, 120);
        list.Width.Set(0, 1);
        list.Height.Set(-120, 1);
        list.ListPadding = 6;
        list.ManualSortMethod = static _ => { };
        p.Append(list);
        chips = shownText = shownFilter = null;
        shownVersion = -1;
        Refresh();
    }

    void SetFilter(string value)
    {
        filter = filter == value ? All : value;
        Refresh();
    }

    void BuildFilters(List<string> icons)
    {
        string key = string.Join(",", icons);
        if (key == chips)
            return;
        chips = key;
        UIElement bar = filters!;
        bar.RemoveAllChildren();
        const float size = 32, gap = 4, width = 476;
        var row = new List<Chip>
        {
            new(L("UI.All"), 70, size, () => filter == All, () =>
            {
                filter = All;
                Refresh();
            }),
            new(static () => Icons.Star(true), size, size, () => filter == Fav, () => SetFilter(Fav)),
        };
        foreach (string k in icons)
            row.Add(new Chip(() => Icons.IconTexture(k), size, size, () => filter == k, () => SetFilter(k)));
        float x = 0, y = 0;
        foreach (Chip chip in row)
        {
            float w = chip.Width.Pixels;
            if (x + w > width)
            {
                x = 0;
                y += size + gap;
            }
            bar.Append(Place(chip, x, y));
            x += w + gap;
        }
        float top = 80 + y + size + 8;
        bar.Height.Set(y + size, 0);
        list!.Top.Set(top, 0);
        list.Height.Set(-top, 1);
        empty!.Top.Set(top + 6, 0);
        Recalculate();
    }

    static bool HasIcon(string key)
    {
        foreach (TeleportPoint p in Data.points)
        {
            if (p.icon == key)
                return true;
        }
        return false;
    }

    void Refresh()
    {
        if (list == null)
            return;
        // Only these can change what the screen shows, so nothing is rebuilt until one of them does.
        string text = search?.Text ?? "";
        if (Data.version == shownVersion && text == shownText && filter == shownFilter)
            return;

        var icons = new List<string>();
        foreach (IconInfo i in Icons.ICONS)
        {
            if (HasIcon(i.key))
                icons.Add(i.key);
        }
        if (filter != All && filter != Fav && !icons.Contains(filter))
            filter = All;
        BuildFilters(icons);

        string query = text.Trim().ToLowerInvariant();
        bool Match(string value) => query.Length == 0 || value.ToLowerInvariant().Contains(query);

        var deaths = new List<DeathPoint>();
        if (filter == All)
        {
            foreach (DeathPoint d in Data.deaths)
            {
                if (Match(L("UI.DeathOf", d.owner)))
                    deaths.Add(d);
            }
        }
        var favorites = new List<TeleportPoint>();
        var others = new List<TeleportPoint>();
        foreach (TeleportPoint p in Data.points)
        {
            bool passes = filter == All || (filter == Fav ? p.favorite : p.icon == filter);
            if (!passes || !(Match(p.name) || Match(Icons.IconName(p.icon))))
                continue;
            (p.favorite ? favorites : others).Add(p);
        }

        shownVersion = Data.version;
        shownText = text;
        shownFilter = filter;

        float view = list.ViewPosition;
        list.Clear();
        var rows = new List<UIElement>(favorites.Count + deaths.Count + others.Count);
        foreach (TeleportPoint p in favorites)
            rows.Add(new Row(this, p, null));
        foreach (DeathPoint d in deaths)
            rows.Add(new Row(this, null, d));
        foreach (TeleportPoint p in others)
            rows.Add(new Row(this, p, null));
        list.AddRange(rows);
        list.ViewPosition = view;
        string none = Data.points.Count == 0 && Data.deaths.Count == 0 ? L("UI.Empty") : rows.Count == 0 ? L("UI.NoResults") : "";
        empty!.SetText(none);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        Refresh();
    }
}
