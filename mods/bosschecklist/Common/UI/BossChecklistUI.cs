using System;
using System.Collections.Generic;
using BossChecklistMod.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;
using static BossChecklistMod.Common.ModText;
using static BossChecklistMod.Common.UI.Common;

namespace BossChecklistMod.Common.UI;

public class BossChecklistUI : UIState
{
    public static bool showCompleted = true, showMiniBoss = true, showEvent = true, showHidden;

    public UIPanel checklistPanel = null!;
    public LogList checklistList = null!;

    float builtQ;
    bool waiting;

    public static float Q => Math.Clamp(Cfg.Log.ChecklistScale != 0 ? Cfg.Log.ChecklistScale : 1f, 0.5f, 1.5f);

    public BossChecklistUI() => Refs.checklist = this;

    public bool Visible
    {
        get
        {
            UserInterface? ui = Refs.system?.bossChecklistInterface;
            return ui != null && ui.CurrentState == this;
        }
        set
        {
            UserInterface? ui = Refs.system?.bossChecklistInterface;
            if (ui == null)
                return;
            if (value)
            {
                Build();
                CloseInventory();
                if (Refs.log != null && Refs.log.BossLogVisible)
                    Refs.log.ToggleBossLog(false);
                waiting = true;
            }
            ui.SetState(value ? this : null);
            if (value)
                UpdateCheckboxes();
        }
    }

    public void Toggle()
    {
        Visible = !Visible;
        Sound(Visible ? SoundID.MenuOpen : SoundID.MenuClose);
    }

    public override void OnInitialize() => Build(true);

    void Build(bool force = false)
    {
        float q = Q;
        if (!force && q == builtQ)
            return;
        builtQ = q;
        RemoveAllChildren();
        float spacing = 8 * q;
        var panel = checklistPanel = new UIPanel();
        panel.SetPadding(10 * q);
        panel.HAlign = 1;
        panel.Top.Set(50, 0);
        panel.Width.Set(250 * q, 0);
        panel.Height.Set(-100, 1);
        panel.BackgroundColor = new Color(73, 94, 171);

        var buttons = new UIPanel { BackgroundColor = new Color(240, 248, 255) };
        buttons.SetPadding(6 * q);
        buttons.Width.Set(0, 1);
        buttons.Height.Set(36 * q, 0);
        panel.Append(buttons);

        var toggles = new (Func<Texture2D> texture, string tip, string key)[]
        {
            (() => Tracker.ItemTexture(43), "Checklist.ToggleCompletedTooltip", nameof(showCompleted)),
            (() => Tex("Nav_Miniboss"), "Checklist.ToggleMiniBossesTooltip", nameof(showMiniBoss)),
            (() => Tex("Nav_Event"), "Checklist.ToggleEventsTooltip", nameof(showEvent)),
            (() => Tex("Nav_Hidden"), "Checklist.ToggleShowHiddenBossesTooltip", nameof(showHidden)),
        };
        for (int i = 0; i < toggles.Length; i++)
        {
            var b = new ToggleButton(toggles[i].texture, L(toggles[i].tip), toggles[i].key, 26 * q);
            b.Left.Set(spacing + 32 * q * i, 0);
            b.Top.Set(-1 * q, 0);
            buttons.Append(b);
        }

        var list = checklistList = new LogList();
        list.Top.Set(42 * q + spacing, 0);
        list.Width.Set(0, 1);
        list.Height.Set(-42 * q - spacing, 1);
        list.ListPadding = 6 * q;
        panel.Append(list);
        Append(panel);
    }

    public override void LeftMouseDown(UIMouseEvent evt)
    {
        base.LeftMouseDown(evt);
        Gesture.Press(evt);
        Hover.Unpin();
    }

    public override void LeftMouseUp(UIMouseEvent evt)
    {
        base.LeftMouseUp(evt);
        Gesture.Release(evt);
    }

    public override void Update(GameTime gameTime)
    {
        UIElement? held = Gesture.Update();
        if (held != null)
            Gesture.FireLong(held);
        bool busy = GameUIBusy();
        if (Util.Local.IsNull || (busy && !waiting) || (Refs.log != null && Refs.log.BossLogVisible))
        {
            Visible = false;
            return;
        }
        if (!busy)
            waiting = false;
        Build();
        base.Update(gameTime);
        if (Gesture.On(checklistPanel))
            HideMouseOverInteractions();
    }

    public void AddShieldRects(List<CalculatedStyle> rects)
    {
        foreach (UIElement e in Elements)
        {
            if (e.IgnoresMouseInteraction)
                continue;
            CalculatedStyle d = e.GetDimensions();
            if (d.Width > 0 && d.Height > 0)
                rects.Add(d);
        }
    }

    public void UpdateCheckboxes()
    {
        if (checklistList == null)
            return;
        LogList list = checklistList;
        string? expandedKey = null;
        foreach (UIElement item in list.Items)
        {
            if (item is UIBossCheckbox { expanded: true } box)
            {
                expandedKey = box.boss.Key;
                break;
            }
        }
        float view = list.ViewPosition;
        list.Clear();
        var rows = new List<UIElement>();
        foreach (EntryInfo boss in Tracker.sorted)
        {
            if (!boss.Available() || (boss.hidden && !showHidden))
                continue;
            if (!showCompleted && boss.Downed())
                continue;
            if (boss.type == Data.EVENT && !showEvent)
                continue;
            if (boss.type == Data.MINIBOSS && !showMiniBoss)
                continue;
            var box = new UIBossCheckbox(boss);
            if (boss.Key == expandedKey)
            {
                box.expanded = true;
                box.PostExpand();
            }
            rows.Add(box);
        }
        list.AddRange(rows);
        list.ViewPosition = view;
    }
}

public class UIHoverImageButton : UIElement
{
    readonly Func<Texture2D> texture;
    readonly string hoverText;
    readonly Func<bool> isOn;

    public UIHoverImageButton(Func<Texture2D> texture, string hoverText, Func<bool> isOn, float size)
    {
        this.texture = texture;
        this.hoverText = hoverText;
        this.isOn = isOn;
        Width.Set(size, 0);
        Height.Set(size, 0);
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        Sound(SoundID.MenuTick);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        bool on = Gesture.On(this);
        Texture2D tex = texture();
        Color color = Alpha(C.White, on ? 255 : 178);
        if (!isOn())
            color = D.Rgb(110, 110, 110, 178);
        if (!tex.IsNull)
        {
            float s = Math.Min(d.Width / tex.Width, d.Height / tex.Height);
            float w = tex.Width * s, h = tex.Height * s;
            D.Sprite(tex, d.X + (d.Width - w) / 2, d.Y + (d.Height - h) / 2, w, h, color);
        }
        if (on)
            Hover.Set(hoverText);
    }
}

public class UIBossCheckbox : UIElement, ILongPress
{
    public bool expanded;
    public readonly EntryInfo boss;

    float descriptionHeight = 18;
    readonly float q;
    readonly Color textColor;
    readonly bool selected;
    readonly MoreInfoButton moreInfo;
    RichLayout? spawnLayout;
    string spawnText = "";
    float spawnWidth;

    public UIBossCheckbox(EntryInfo boss)
    {
        this.boss = boss;
        q = BossChecklistUI.Q;
        Width.Set(0, 1);
        PaddingTop = 6 * q;
        Height.Set(PaddingTop + 15 * q, 0);
        textColor = C.White;
        if (boss.type == Data.EVENT)
            textColor = C.MediumPurple;
        if (boss.type == Data.MINIBOSS)
            textColor = C.CornflowerBlue;
        if (boss.hidden)
            textColor = C.DarkGreen;
        selected = boss.Downed();
        moreInfo = new MoreInfoButton(boss, 22 * q);
        moreInfo.Left.Set(-24 * q, 1);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (evt.Target == moreInfo || !Gesture.IsTap(evt))
            return;
        foreach (UIElement item in Refs.checklist!.checklistList.Items)
        {
            if (item != this && item is UIBossCheckbox { expanded: true } other)
            {
                other.expanded = false;
                other.PostExpand();
            }
        }
        expanded = !expanded;
        Sound(SoundID.MenuTick);
        PostExpand();
    }

    public bool LongPress()
    {
        bool hide = !World.hidden.Contains(boss.Key);
        WorldData.SetHidden(boss.Key, hide);
        Sound(SoundID.Unlock);
        if (Cfg.Log.HideUnavailable && Refs.log != null)
            Refs.log.PendingPageNum = LogPages.PAGE_TOC;
        Refs.checklist!.UpdateCheckboxes();
        return true;
    }

    public void PostExpand()
    {
        AddOrRemoveChild(moreInfo, expanded);
        Height.Set(PaddingTop + 15 * q + (expanded ? descriptionHeight : 0), 0);
        Recalculate();
        if (Parent != null)
            Refs.checklist!.checklistList.Recalculate();
    }

    public override int CompareTo(object? obj) => obj is UIBossCheckbox other ? boss.progression.CompareTo(other.boss.progression) : 0;

    static void At(Texture2D texture, float x, float y, float scale)
    {
        if (!texture.IsNull)
            D.Sprite(texture, x, y, texture.Width * scale, texture.Height * scale);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetInnerDimensions();
        float x = d.X + 20 * q;
        At(UITex("checkBox"), x - 20 * q, d.Y - 5 * q, q);
        if (selected)
            At(UITex("checkMark"), x - 20 * q, d.Y - 5 * q, q);
        string text = D.Truncate(boss.DisplayName, d.Width - 20 * q - (expanded ? 26 * q : 0), q);
        D.Text(text, x, d.Y - 2 * q, textColor, q);
        if (!expanded)
            return;
        float scale = 0.85f * q;
        string info = boss.DisplaySpawnInfo;
        float width = d.Width - 7 * 2 * q;
        if (spawnLayout == null || spawnText != info || spawnWidth != width)
        {
            spawnLayout = Rich.Layout(info, width, scale);
            spawnText = info;
            spawnWidth = width;
        }
        Rich.Draw(spawnLayout, d.X + 2 * q, d.Y + (15 + 3) * q, scale, true);
        float size = spawnLayout.height + 3 * q;
        if (Math.Abs(size - descriptionHeight) > 0.5f)
        {
            descriptionHeight = size;
            PostExpand();
        }
    }
}

public class MoreInfoButton : UIHoverImageButton
{
    readonly EntryInfo boss;

    public MoreInfoButton(EntryInfo boss, float size) : base(() => UITex("info"), L("Checklist.MoreInfo"), () => true, size) => this.boss = boss;

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Gesture.IsTap(evt) || Refs.log == null)
            return;
        Refs.checklist!.Visible = false;
        Refs.log.ToggleBossLog(true);
        Refs.log.PageNum = boss.GetIndex;
    }
}

public class ToggleButton : UIHoverImageButton, ILongPress
{
    readonly string key;

    public ToggleButton(Func<Texture2D> texture, string hoverText, string key, float size) : base(texture, hoverText, () => Read(key), size) => this.key = key;

    static bool Read(string key) => key switch
    {
        nameof(BossChecklistUI.showCompleted) => BossChecklistUI.showCompleted,
        nameof(BossChecklistUI.showMiniBoss) => BossChecklistUI.showMiniBoss,
        nameof(BossChecklistUI.showEvent) => BossChecklistUI.showEvent,
        _ => BossChecklistUI.showHidden,
    };

    static void Write(string key, bool value)
    {
        switch (key)
        {
            case nameof(BossChecklistUI.showCompleted): BossChecklistUI.showCompleted = value; break;
            case nameof(BossChecklistUI.showMiniBoss): BossChecklistUI.showMiniBoss = value; break;
            case nameof(BossChecklistUI.showEvent): BossChecklistUI.showEvent = value; break;
            default: BossChecklistUI.showHidden = value; break;
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Gesture.IsTap(evt))
            return;
        bool value = !Read(key);
        Write(key, value);
        Sound(value ? SoundID.MenuOpen : SoundID.MenuClose);
        Refs.checklist!.UpdateCheckboxes();
    }

    public bool LongPress()
    {
        if (key != nameof(BossChecklistUI.showHidden))
            return false;
        WorldData.ClearHidden();
        BossChecklistUI.showHidden = false;
        Sound(SoundID.MenuClose);
        Refs.checklist!.UpdateCheckboxes();
        return true;
    }
}
