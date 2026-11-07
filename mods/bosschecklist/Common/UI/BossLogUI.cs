using System;
using System.Collections.Generic;
using System.Linq;
using BossChecklistMod.Common.Players;
using BossChecklistMod.Common.Systems;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Map;
using Terraria.UI;
using static BossChecklistMod.Common.Data;
using static BossChecklistMod.Common.ModText;
using static BossChecklistMod.Common.RecordCategory;
using static BossChecklistMod.Common.UI.Common;
using static BossChecklistMod.Common.UI.LogPages;

namespace BossChecklistMod.Common.UI;

public class PictureElement : UIElement
{
    Texture2D texture;

    public PictureElement(Texture2D texture) => this.texture = texture;

    public void SetImage(Texture2D value) => texture = value;

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        if (texture.IsNull)
            return;
        CalculatedStyle d = GetDimensions();
        D.Sprite(texture, d.X, d.Y, d.Width, d.Height, C.White);
    }
}

public class BackdropElement : UIElement
{
    readonly Action onTap;

    public BackdropElement(Action onTap)
    {
        Width.Set(0, 1);
        Height.Set(0, 1);
        this.onTap = onTap;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (evt.Target == this && Gesture.IsTap(evt))
            onTap();
    }
}

public class FilterPanelElement : LogUIElement
{
    public FilterPanelElement(Texture2D texture) : base(texture) { }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (LogUI.FilterPanelIsOpen)
            base.Draw(spriteBatch);
    }
}

public class PageTitle : UIElement
{
    readonly string text;

    public PageTitle(string text)
    {
        this.text = text;
        IgnoresMouseInteraction = true;
        Width.Set(BigMeasure(text, 0.6f * View.K), 0);
        Height.Set(P(32), 0);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        BigText(text, d.X, d.Y - P(10) * 0.6f, C.Amber, 0.6f * View.K);
    }
}

public class PageText : UIElement
{
    public readonly string text;
    readonly Color color;
    readonly float scale;

    public PageText(string text, Color color, float scale = 1)
    {
        this.text = text;
        this.color = color;
        this.scale = scale;
        IgnoresMouseInteraction = true;
        Width.Set(D.Measure(text, scale * View.K).X, 0);
        Height.Set(P(16 * scale), 0);
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetDimensions();
        float s = scale * View.K;
        D.Text(text, d.X, d.Y - 2 * s, color, s);
    }
}

public class CategoryIcon : LogUIElement, ILongPress
{
    public CategoryIcon(Texture2D texture) : base(texture) { }

    public bool LongPress()
    {
        if (!Cfg.Log.EnabledResetOptions)
            return false;
        LogUI.ResetStats();
        return true;
    }
}

public class BossLogUI : UIState
{
    static readonly (string name, string title)[] Contributors = { ("Jopojelly", "Owner"), ("SheepishShepherd", "CoOwner"), ("direwolf420", "Contributor"), ("riveren", "Sprites"), ("Orian", "EarlyTesting"), ("Panini", "EarlyTesting") };
    static readonly HashSet<int> Coins = new() { 71, 72, 73, 74 };
    static readonly string[] RecordIcons = { "Nav_Record_PreviousAttempt", "Nav_Record_FirstVictory", "Nav_Record_PersonalBest", "Nav_Record_WorldRecord" };
    static readonly Dictionary<int, int> Stations = new()
    {
        [18] = 36, [16] = 35, [134] = 525, [17] = 33, [77] = 221, [13] = 31, [101] = 354, [114] = 398, [86] = 332, [94] = 352, [96] = 345, [106] = 363, [125] = 487, [133] = 524, [220] = 1120, [228] = 1430, [243] = 1791, [247] = 2192, [283] = 2172, [300] = 2193, [301] = 2194, [302] = 2195, [303] = 2196, [304] = 2197, [305] = 2198, [306] = 2199, [307] = 2203, [308] = 2204, [412] = 3549, [15] = 34, [14] = 32, [87] = 333, [88] = 334, [89] = 335, [85] = 321,
    };
    const int HandOfCreation = 5126, Torch = 8;
    const float Swipe = 90;

    public LogPanel BookArea = null!, LeftPage = null!, RightPage = null!;
    public int SelectedSubPage = SUB_SPAWN;
    public bool FilterPanelIsOpen, ProgressBarState;
    public readonly Dictionary<string, bool> HiddenEntriesPending = new();
    public int SelectedRecordCategory = PREVIOUS, SelectedRecordComparison = NONE;
    public List<int> EventEntryNPCBannerIDList = new();
    public int SpawnItemSelected, RecipeSelected;
    public bool OtherworldMusicUnlocked;
    public int headNum = -1;
    public bool PendingToggleBossLogUI;

    BackdropElement Backdrop = null!;
    LogTab TableOfContentsTab = null!, NextBossTab = null!, NextMiniBossTab = null!, NextEventTab = null!, CreditsTab = null!;
    NavigationalButton PreviousPageButton = null!, NextPageButton = null!;
    FilterPanelElement FilterPanel = null!;
    FilterIcon[] FilterIcons = null!;
    IndicatorIcon[] Indicators = null!;
    IndicatorIcon InteractionIcon = null!;
    IndicatorPanel IndicatorTab = null!, AltInteractionsTab = null!;
    SubPageButton OpenRecordPageButton = null!, OpenSpawnPageButton = null!, OpenLootPageButton = null!;
    NavigationalButton[] RecordCategoryButtons = null!;

    int page = PAGE_TOC;
    bool hiddenListOpen, pendingPageChange, visible, waiting;
    int pageChangeValue;
    float builtK;
    List<LogList> lists = new();
    string listsKey = "";

    public BossLogUI() => Refs.log = this;

    // Whether the interface has anything to do this frame.
    public bool NeedsUpdate => visible || PendingToggleBossLogUI || pendingPageChange;

    public int PageNum
    {
        get => page;
        set
        {
            if (value == PAGE_PROMPT)
            {
                OpenProgressionModePrompt();
                return;
            }
            if (HiddenEntriesMode)
                return;
            ClearNewRecord();
            page = value;
            TableOfContentsTab.Anchor = value == PAGE_TOC ? null : -1;
            RefreshPageContent();
        }
    }

    public int PendingPageNum
    {
        get => pageChangeValue;
        set
        {
            if (HiddenEntriesMode)
                return;
            pageChangeValue = value;
            pendingPageChange = true;
        }
    }

    public EntryInfo? GetLogEntryInfo => page >= 0 && page < Tracker.sorted.Count ? Tracker.sorted[page] : null;

    public PersonalRecords? GetPlayerRecords
    {
        get
        {
            int ri = GetLogEntryInfo?.RecordIndex ?? -1;
            List<PersonalRecords>? list = State.Me?.RecordsForWorld;
            return list != null && ri >= 0 && ri < list.Count ? list[ri] : null;
        }
    }

    public WorldRecord? GetWorldRecords
    {
        get
        {
            int ri = GetLogEntryInfo?.RecordIndex ?? -1;
            return ri >= 0 && ri < State.worldRecords.Count ? State.worldRecords[ri] : null;
        }
    }

    public bool HiddenEntriesMode
    {
        get => hiddenListOpen;
        set
        {
            hiddenListOpen = value;
            EntryUI.hiddenMode = value;
            if (!value)
            {
                foreach ((string key, bool hide) in HiddenEntriesPending)
                {
                    if (World.hidden.Contains(key) != hide)
                        WorldData.SetHidden(key, hide);
                }
                Sound(HiddenEntriesPending.Count > 0 ? SoundID.ResearchComplete : SoundID.MenuClose);
                HiddenEntriesPending.Clear();
            }
            else
            {
                Sound(SoundID.MenuOpen);
            }
            Tracker.Invalidate();
        }
    }

    public bool IsHidden(EntryInfo entry)
    {
        if (hiddenListOpen && HiddenEntriesPending.TryGetValue(entry.Key, out bool pending))
            return pending;
        return World.hidden.Contains(entry.Key);
    }

    public bool BossLogVisible
    {
        get => visible;
        set
        {
            UIElement[] parts = { Backdrop, BookArea, TableOfContentsTab, FilterPanel, IndicatorTab, AltInteractionsTab,
                CreditsTab, NextBossTab, NextMiniBossTab, NextEventTab, LeftPage, RightPage };
            if (value)
            {
                foreach (UIElement part in parts)
                    Append(part);
            }
            else
            {
                foreach (UIElement part in parts.Reverse())
                    RemoveChild(part);
                Hover.Unpin();
            }
            visible = value;
        }
    }

    void ClearNewRecord()
    {
        int ri = GetLogEntryInfo?.RecordIndex ?? -1;
        BossChecklistPlayer? me = State.Me;
        if (me != null && ri >= 0 && ri < me.hasNewRecord.Length)
            me.hasNewRecord[ri] = false;
    }

    public void ToggleBossLog(bool show = true)
    {
        if (show)
        {
            if (Main.gameMenu || Util.Local.IsNull)
                return;
            Build();
            CalculatedStyle screen = Screen();
            BookArea.Left.Pixels = screen.Width / 2 - BookArea.Width.Pixels / 2;
            BookArea.Top.Pixels = screen.Height / 2 - BookArea.Height.Pixels / 2 - P(6);
            BossChecklistPlayer? me = State.Me;
            if (me != null && !me.hasOpenedTheBossLog)
            {
                me.enteredWorldReset = false;
                PageNum = Cfg.Log.PromptDisabled ? PAGE_TOC : PAGE_PROMPT;
            }
            else if (me != null && me.enteredWorldReset)
            {
                me.enteredWorldReset = false;
                PageNum = PAGE_TOC;
            }
            else
            {
                if (page >= Tracker.sorted.Count)
                    page = PAGE_TOC;
                RefreshPageContent();
            }
            CloseInventory();
            if (Refs.checklist != null && Refs.checklist.Visible)
                Refs.checklist.Visible = false;
            waiting = true;
            Sound(SoundID.MenuOpen);
        }
        else if (page >= 0)
        {
            ClearNewRecord();
        }
        else if (page == PAGE_TOC && HiddenEntriesMode)
        {
            HiddenEntriesPending.Clear();
            HiddenEntriesMode = false;
        }
        if (!show && visible)
            Sound(SoundID.MenuClose);
        BossLogVisible = show;
    }

    CalculatedStyle Screen()
    {
        CalculatedStyle d = GetDimensions();
        if (d.Width > 0 && d.Height > 0)
            return d;
        return new CalculatedStyle(0, 0, Main.screenWidth, Main.screenHeight);
    }

    public override void OnInitialize() => Build(true);

    bool Build(bool force = false)
    {
        CalculatedStyle screen = Screen();
        float K = View.Fit(screen.Width, screen.Height);
        if (!force && K == builtK)
            return false;
        builtK = K;
        bool wasVisible = visible;
        if (wasVisible)
            BossLogVisible = false;

        Backdrop = new BackdropElement(() => ToggleBossLog(false));
        BookArea = new LogPanel(Tex("LogUI_Back"));
        BookArea.asset = default;

        TableOfContentsTab = new LogTab(Tex("LogUI_Tab"), Tex("Nav_Contents")) { Id = "TableOfContents" };
        TableOfContentsTab.OnLeftClick += (evt, _) =>
        {
            if (Gesture.IsTap(evt))
                UpdateFilterTabPos(true);
        };
        NextBossTab = new LogTab(Tex("LogUI_Tab"), Tex("Nav_Boss")) { Id = "Boss" };
        NextMiniBossTab = new LogTab(Tex("LogUI_Tab"), Tex("Nav_Miniboss")) { Id = "MiniBoss" };
        NextEventTab = new LogTab(Tex("LogUI_Tab"), Tex("Nav_Event")) { Id = "Event" };
        CreditsTab = new LogTab(Tex("LogUI_Tab"), Tex("Nav_Credits")) { Id = "Credits" };
        CreditsTab.Anchor = PAGE_CREDITS;
        CreditsTab.hoverText = "Log.Tabs.Credits";

        LeftPage = new LogPanel { Id = "PageOne" };
        RightPage = new LogPanel { Id = "PageTwo" };
        Place(LeftPage, null, null, 375, 480);
        Place(RightPage, null, null, 375, 480);

        PreviousPageButton = new NavigationalButton(Tex("Nav_Prev"), true, null, 14) { Id = "Previous" };
        Place(PreviousPageButton, 8, 416);
        PreviousPageButton.OnLeftClick += PageChangerClicked;

        FilterPanel = new FilterPanelElement(Tex("LogUI_Filter"));
        (string id, string icon)[] filters = { ("Boss", "Nav_Boss"), ("MiniBoss", "Nav_Miniboss"), ("Event", "Nav_Event"), ("Hidden", "Nav_Hidden") };
        FilterIcons = new FilterIcon[filters.Length];
        for (int i = 0; i < filters.Length; i++)
        {
            var f = new FilterIcon(Tex(filters[i].icon)) { Id = filters[i].id };
            f.Top.Set(P(i * 34 + 15), 0);
            f.Left.Set(P(25) - (int)(f.Width.Pixels / 2), 0);
            FilterPanel.Append(f);
            FilterIcons[i] = f;
        }

        Indicators = new[]
        {
            new IndicatorIcon(Tex("Indicator_OnlyBosses")) { Id = "OnlyBosses" },
            new IndicatorIcon(Tex("Indicator_Manual")) { Id = "Manual" },
            new IndicatorIcon(Tex("Indicator_Progression")) { Id = "Progression" },
        };
        IndicatorTab = new IndicatorPanel(Indicators.Length) { Id = "Configurations" };
        for (int n = 0; n < Indicators.Length; n++)
        {
            Place(Indicators[n], 10 + 2 + 20 * n, 8);
            IndicatorTab.Append(Indicators[n]);
        }

        InteractionIcon = new IndicatorIcon(Tex("Indicator_Interaction")) { IgnoresMouseInteraction = true };
        AltInteractionsTab = new IndicatorPanel(1) { Id = "Interactions" };
        InteractionIcon.Left.Set((int)(AltInteractionsTab.Width.Pixels / 2 - InteractionIcon.Width.Pixels / 2), 0);
        InteractionIcon.Top.Set(P(8), 0);
        AltInteractionsTab.Append(InteractionIcon);

        NextPageButton = new NavigationalButton(Tex("Nav_Next"), true, null, 14) { Id = "Next" };
        NextPageButton.Left.Set(RightPage.Width.Pixels - NextPageButton.Width.Pixels - P(12), 0);
        NextPageButton.Top.Set(P(416), 0);
        NextPageButton.OnLeftClick += PageChangerClicked;

        Texture2D sub = Tex("Nav_SubPage_Button");
        int subW = !sub.IsNull ? sub.Width : 158, subH = !sub.IsNull ? sub.Height : 30;
        OpenRecordPageButton = new SubPageButton(sub, SUB_RECORDS);
        Place(OpenRecordPageButton, (int)(375 / 2f - subW / 2f), 5 + subH + 10);
        OpenSpawnPageButton = new SubPageButton(sub, SUB_SPAWN);
        Place(OpenSpawnPageButton, (int)(375 / 2f - subW - 8), 5);
        OpenLootPageButton = new SubPageButton(sub, SUB_LOOT);
        Place(OpenLootPageButton, (int)(375 / 2f + 8), 5);

        RecordCategoryButtons = new NavigationalButton[4];
        for (int value = 0; value < 4; value++)
        {
            RecordCategoryButtons[value] = new NavigationalButton(Tex(RecordIcons[value]), true, null, 3)
            {
                Record_Anchor = value,
                hoverText = "Log.Records.Category." + Names[value],
            };
        }

        TableOfContentsTab.Anchor = page == PAGE_TOC ? null : -1;
        UpdateIndicators();
        if (wasVisible)
        {
            BossLogVisible = true;
            RefreshPageContent();
        }
        return true;
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
        if (visible && page != PAGE_PROMPT && Gesture.pressed && !Gesture.longFired && !HiddenEntriesMode)
        {
            Vector2 m = evt.MousePosition;
            float dx = m.X - Gesture.startX, dy = m.Y - Gesture.startY;
            if (Math.Abs(dx) >= P(Swipe) && Math.Abs(dx) >= Math.Abs(dy) * 2 && BookArea.ContainsPoint(new Vector2(Gesture.startX, Gesture.startY)))
                TurnPage(dx < 0 ? 1 : -1);
        }
        Gesture.Release(evt);
    }

    public override void Update(GameTime gameTime)
    {
        UIElement? held = Gesture.Update();
        if (held != null)
            Gesture.FireLong(held);

        if (PendingToggleBossLogUI)
        {
            PendingToggleBossLogUI = false;
            ToggleBossLog(!visible);
        }
        if (visible)
        {
            Player p = Util.Local;
            bool busy = GameUIBusy();
            if (Main.gameMenu || p.IsNull || p.dead || (busy && !waiting))
                ToggleBossLog(false);
            else if (!busy)
                waiting = false;
        }
        if (visible && Build())
        {
            CalculatedStyle screen = Screen();
            BookArea.Left.Pixels = screen.Width / 2 - BookArea.Width.Pixels / 2;
            BookArea.Top.Pixels = screen.Height / 2 - BookArea.Height.Pixels / 2 - P(6);
            RefreshPageContent();
        }
        if (pendingPageChange)
        {
            pendingPageChange = false;
            PageNum = pageChangeValue;
        }
        if (visible && Gesture.touching)
            HideMouseOverInteractions();
        base.Update(gameTime);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        if (headNum == -1 || !visible || headNum >= Tracker.sorted.Count)
            return;
        EntryInfo entry = Tracker.sorted[headNum];
        bool masked = Cfg.Log.ProgressiveChecklist && !entry.IsAutoDownedOrMarked && !entry.IsUpNext;
        Color mask = MaskBoss(entry);
        float x = Gesture.x + P(15);
        List<Texture2D> heads = masked ? new List<Texture2D> { TextureAssets.NpcHead[0].Value } : entry.Heads();
        int tallest = 0;
        foreach (Texture2D head in heads)
        {
            if (!head.IsNull)
                tallest = Math.Max(tallest, head.Height);
        }
        float y = Gesture.y - P(15) - P(tallest);
        foreach (Texture2D head in heads)
        {
            if (head.IsNull)
                continue;
            D.Sprite(head, x, y, P(head.Width), P(head.Height), mask);
            x += P(head.Width + 2);
        }
    }

    public void AddShieldRects(List<CalculatedStyle> rects)
    {
        foreach (UIElement e in Elements)
        {
            if (e == Backdrop || e.IgnoresMouseInteraction)
                continue;
            CalculatedStyle d = e.GetDimensions();
            if (d.Width > 0 && d.Height > 0)
                rects.Add(d);
        }
    }

    void ResetUIPositioning()
    {
        LogPanel book = BookArea;
        float left = book.Left.Pixels, top = book.Top.Pixels;
        LeftPage.Left.Pixels = left + P(20);
        LeftPage.Top.Pixels = top + P(12);
        RightPage.Left.Pixels = left - P(15) + book.Width.Pixels - RightPage.Width.Pixels;
        RightPage.Top.Pixels = top + P(12);

        float offsetY = P(50);
        float tabH = NextBossTab.Height.Pixels;
        TableOfContentsTab.Top.Pixels = top + offsetY;
        IndicatorTab.Left.Pixels = RightPage.Left.Pixels + RightPage.Width.Pixels - P(25) - IndicatorTab.Width.Pixels;
        IndicatorTab.Top.Pixels = RightPage.Top.Pixels - IndicatorTab.Height.Pixels - P(6);
        AltInteractionsTab.Left.Pixels = IndicatorTab.Left.Pixels - AltInteractionsTab.Width.Pixels - P(2);
        AltInteractionsTab.Top.Pixels = RightPage.Top.Pixels - AltInteractionsTab.Height.Pixels - P(6);
        AltInteractionsTab.hoverText = GenerateInteractionHoverText();
        UpdateFilterTabPos(false);

        NextBossTab.Top.Pixels = top + offsetY + tabH;
        NextMiniBossTab.Top.Pixels = top + offsetY + tabH * 2;
        NextEventTab.Top.Pixels = top + offsetY + tabH * 3;
        CreditsTab.Top.Pixels = top + offsetY + tabH * 4;
        CreditsTab.Left.Pixels = left + book.Width.Pixels - P(12);
        foreach (LogTab tab in new[] { NextBossTab, NextMiniBossTab, NextEventTab })
            tab.Left.Pixels = left + (tab.OnLeftSide() ? -P(20) : book.Width.Pixels - P(12));
        foreach (LogTab tab in new[] { TableOfContentsTab, CreditsTab, NextBossTab, NextMiniBossTab, NextEventTab })
            tab.IgnoresMouseInteraction = !tab.Visible();
        IndicatorTab.IgnoresMouseInteraction = page == PAGE_PROMPT;
        AltInteractionsTab.IgnoresMouseInteraction = page == PAGE_PROMPT || AltInteractionsTab.hoverText == null;
        Recalculate();
    }

    void UpdateFilterTabPos(bool tabClicked)
    {
        if (tabClicked && page != PAGE_TOC)
            return;
        if (HiddenEntriesMode)
            return;
        if (page != PAGE_TOC)
            FilterPanelIsOpen = false;
        else if (tabClicked)
            FilterPanelIsOpen = !FilterPanelIsOpen;
        LogPanel book = BookArea;
        if (FilterPanelIsOpen)
        {
            FilterPanel.Top.Pixels = TableOfContentsTab.Top.Pixels;
            TableOfContentsTab.Left.Pixels = book.Left.Pixels - P(20) - FilterPanel.Width.Pixels;
            FilterPanel.Left.Pixels = TableOfContentsTab.Left.Pixels + TableOfContentsTab.Width.Pixels;
            foreach (FilterIcon f in FilterIcons)
                f.UpdateFilterIcon();
        }
        else
        {
            TableOfContentsTab.Left.Pixels = book.Left.Pixels - P(20);
            FilterPanel.Top.Pixels = TableOfContentsTab.Top.Pixels;
            FilterPanel.Left.Pixels = TableOfContentsTab.Left.Pixels;
        }
        FilterPanel.IgnoresMouseInteraction = !FilterPanelIsOpen;
        if (tabClicked)
            Recalculate();
    }

    public void UpdateIndicators()
    {
        IndicatorIcon only = Indicators[0], manual = Indicators[1], progression = Indicators[2];
        BossLogConfiguration log = Cfg.Log;
        only.Color = log.OnlyShowBossContent ? C.White : C.DarkGray;
        only.hoverText = log.OnlyShowBossContent ? "Log.Indicator.OnlyBossContentEnabled" : "Log.Indicator.OnlyBossContentDisabled";
        manual.Color = log.AutomaticChecklist ? C.LightGreen : C.DarkGray;
        manual.hoverText = log.AutomaticChecklist ? "Log.Indicator.AutomaticChecklist" : "Log.Indicator.ManualChecklist";
        progression.Color = log.ProgressiveChecklist ? C.Tomato : C.DarkGray;
        progression.hoverText = "Log.Indicator.ProgressionMode";
        progression.hoverTextParams = new object?[] { L("Log.Common." + (log.ProgressiveChecklist ? "Enabled" : "Disabled")).ToUpperInvariant() };
        if (NextBossTab != null)
            NextBossTab.Anchor = Tracker.FindNextEntry(BOSS);
    }

    string? GenerateInteractionHoverText()
    {
        if (!Cfg.Log.ShowInteractionTooltips)
            return null;
        bool reset = Cfg.Log.EnabledResetOptions;
        if (page == PAGE_TOC)
        {
            if (HiddenEntriesMode)
                return L("Log.HintTexts.HideEntry") + (reset ? "\n" + L("Log.HintTexts.ClearHidden") : "");
            return L("Log.HintTexts.MarkEntry") + (reset ? "\n" + L("Log.HintTexts.ClearMarked") : "");
        }
        EntryInfo? entry = GetLogEntryInfo;
        if (entry == null)
            return null;
        if (SelectedSubPage == SUB_RECORDS && entry.type == BOSS)
            return L("Log.HintTexts.Compare") + (reset ? "\n" + L("Log.HintTexts.ClearAllRecords") + "\n" + L("Log.HintTexts.ClearRecord") : "");
        if (reset && SelectedSubPage == SUB_LOOT)
            return L("Log.HintTexts.RemoveItem") + "\n" + L("Log.HintTexts.ClearItems");
        return null;
    }

    void PageChangerClicked(UIMouseEvent evt, UIElement button)
    {
        if (Gesture.IsTap(evt))
            TurnPage(((LogUIElement)button).Id == "Next" ? 1 : -1);
    }

    void TurnPage(int dir)
    {
        if (HiddenEntriesMode)
            return;
        if (dir > 0 && page == PAGE_CREDITS)
            return;
        if (dir < 0 && page == PAGE_TOC)
            return;
        List<EntryInfo> list = Tracker.sorted;
        int Step(int v) => dir > 0 ? (v < list.Count - 1 ? v + 1 : PAGE_CREDITS) : (v >= 0 ? v - 1 : list.Count - 1);
        int value = Step(page);
        while (value >= 0 && !list[value].VisibleOnPageContent())
            value = Step(value);
        Sound(SoundID.MenuTick);
        PageNum = value;
    }

    public void RefreshPageContent()
    {
        if (BookArea == null)
            return;
        if (page == PAGE_PROMPT)
        {
            OpenProgressionModePrompt();
            return;
        }
        NextBossTab.Anchor = Tracker.FindNextEntry(BOSS);
        NextMiniBossTab.Anchor = Tracker.FindNextEntry(MINIBOSS);
        NextEventTab.Anchor = Tracker.FindNextEntry(EVENT);
        float[]? scroll = SaveScroll();
        ResetUIPositioning();
        ResetBothPages();
        Util.Safe("boss log page", () =>
        {
            if (page == PAGE_TOC)
                UpdateTableofContents();
            else if (page == PAGE_CREDITS)
                UpdateCredits();
            else if (SelectedSubPage == SUB_RECORDS)
                OpenRecord();
            else if (SelectedSubPage == SUB_SPAWN)
                OpenSpawn();
            else if (SelectedSubPage == SUB_LOOT)
                OpenLoot();
        });
        RestoreScroll(scroll);
    }

    float[]? SaveScroll()
    {
        string key = page + ":" + SelectedSubPage + ":" + SelectedRecordCategory;
        float[]? views = key == listsKey ? lists.Select(l => l.ViewPosition).ToArray() : null;
        lists = new List<LogList>();
        listsKey = key;
        return views;
    }

    void RestoreScroll(float[]? views)
    {
        if (views == null)
            return;
        for (int i = 0; i < lists.Count && i < views.Length; i++)
            lists[i].ViewPosition = views[i];
    }

    LogList Track(LogList list)
    {
        lists.Add(list);
        return list;
    }

    void ResetBothPages()
    {
        LeftPage.RemoveAllChildren();
        RightPage.RemoveAllChildren();
        headNum = -1;
        if (page != PAGE_CREDITS)
            RightPage.Append(NextPageButton);
        if (page != PAGE_TOC)
            LeftPage.Append(PreviousPageButton);
        if (page < 0)
            return;
        EntryInfo? entry = GetLogEntryInfo;
        if (entry == null)
            return;
        EventEntryNPCBannerIDList = new List<int>();
        if (Cfg.Log.AccessInternalNames && entry.modSource != "Unknown")
        {
            var key = new NavigationalButton(Tex("Extra_Key"), true, null, 4)
            {
                Id = "CopyKey",
                hoverText = "Log.EntryPage.CopyKey",
                hoverTextParams = new object?[] { entry.Key },
            };
            Place(key, 5, 55);
            LeftPage.Append(key);
        }
        if (entry.modSource != "Unknown")
        {
            RightPage.Append(OpenRecordPageButton);
            RightPage.Append(OpenSpawnPageButton);
            RightPage.Append(OpenLootPageButton);
        }
        else
        {
            var broken = new UIPanel();
            Place(broken, 3, 150, 340, 160);
            RightPage.Append(broken);
            var text = new FittedTextPanel("EntryPage.LogFeaturesNotAvailable", 1, true);
            Place(text, -15, -12, 340, 200);
            broken.Append(text);
        }
    }

    static void Title(LogPanel pageElement, string key)
    {
        string title = L(key);
        var t = new PageTitle(title);
        t.Top.Set(P(18), 0);
        t.Left.Set((int)(pageElement.Width.Pixels / 2 - BigMeasure(title, 0.6f * View.K) / 2), 0);
        pageElement.Append(t);
    }

    void UpdateTableofContents()
    {
        BossChecklistPlayer? me = State.Me;
        if (me != null && !me.hasOpenedTheBossLog)
            me.hasOpenedTheBossLog = true;

        LogList pre = Track(new LogList()), hard = Track(new LogList());
        Place(pre, 4, 44, 375 - 60, 480 - 136);
        Place(hard, 19, 44, 375 - 60, 480 - 136);
        pre.PaddingTop = hard.PaddingTop = P(5);
        pre.ListPadding = hard.ListPadding = P(5);
        Title(LeftPage, "Log.TableOfContents.PreHardmode");
        Title(RightPage, "Log.TableOfContents.Hardmode");

        var preRows = new List<UIElement>();
        var hardRows = new List<UIElement>();
        BossLogConfiguration log = Cfg.Log;
        foreach (EntryInfo entry in Tracker.sorted)
        {
            if (!entry.VisibleOnChecklist())
                continue;
            string name = entry.DisplayName;
            if (log.ProgressiveChecklist && !entry.IsAutoDownedOrMarked && !entry.IsUpNext)
                name = "???";
            if (log.AutomaticChecklist && entry.MarkedAsDowned)
                name += "*";
            (bool loot, bool all) = log.LootCheckVisibility ? LootStatus(entry) : (false, false);
            bool hidden = IsHidden(entry);
            Color color = C.PapayaWhip;
            if (HiddenEntriesMode)
                color = hidden ? C.DimGray : C.DarkGray;
            else if ((!entry.Available() && !entry.IsAutoDownedOrMarked) || hidden)
                color = C.DimGray;
            else if (log.ColoredBossText)
                color = entry.IsAutoDownedOrMarked ? C.Green : C.Red;
            var row = new TableOfContents(entry.GetIndex, name, color, loot, all);
            row.PaddingTop = P(5);
            row.PaddingLeft = P(entry.progression <= WALL_OF_FLESH ? 32 : 22);
            (entry.progression <= WALL_OF_FLESH ? preRows : hardRows).Add(row);
        }
        pre.AddRange(preRows);
        hard.AddRange(hardRows);

        LeftPage.Append(pre);
        RightPage.Append(hard);

        if (log.ShowProgressBars)
        {
            NavigationalButton prev = PreviousPageButton, next = NextPageButton;
            var preBar = new ProgressBar(false);
            preBar.Left.Set((int)(prev.Left.Pixels + prev.Width.Pixels + P(10)), 0);
            preBar.Height.Set(P(14), 0);
            preBar.Top.Set((int)(prev.Top.Pixels + prev.Height.Pixels / 2 - P(14) / 2), 0);
            preBar.Width.Set((int)(LeftPage.Width.Pixels - preBar.Left.Pixels * 2), 0);
            var hardBar = new ProgressBar(true);
            hardBar.Left.Set(next.Left.Pixels - P(10) - preBar.Width.Pixels, 0);
            hardBar.Height.Set(P(14), 0);
            hardBar.Top.Set(preBar.Top.Pixels, 0);
            hardBar.Width.Set(preBar.Width.Pixels, 0);
            LeftPage.Append(preBar);
            if (!log.ProgressiveChecklist || Main.hardMode)
                RightPage.Append(hardBar);
        }
    }

    (bool loot, bool all) LootStatus(EntryInfo entry)
    {
        BossChecklistPlayer? me = State.Me;
        if (me == null)
            return (false, false);
        bool crimson = WorldGen.crimson;
        bool expert = Main.expertMode, master = Main.masterMode;
        int bag = entry.TreasureBag;
        bool ModeOk(ItemSample s) => (expert || (!s.expert && !s.expertOnly)) && (master || (!s.master && !s.masterOnly));
        bool loot = true;
        foreach (int id in entry.lootItemTypes)
        {
            if (id == bag)
                continue;
            DropInfo? drop = entry.loot.Find(d => d.itemId == id);
            if (drop != null && drop.conditions.Count > 0)
            {
                string all = string.Join(" ", drop.conditions);
                if ((crimson && all.Contains("IsCorruption")) || (!crimson && all.Contains("IsCrimson")))
                    continue;
            }
            if (ModeOk(Tracker.Sample(id)) && !me.obtained.Contains(id))
            {
                loot = false;
                break;
            }
        }
        bool allCollected = loot;
        if (entry.collectibles.Count > 0)
        {
            allCollected = true;
            List<int> drops = entry.CollectibleDrops;
            foreach (int id in entry.collectibles.Keys)
            {
                if (id <= 0 || id == bag)
                    continue;
                if (Cfg.Log.OnlyCheckDroppedLoot && !drops.Contains(id))
                    continue;
                if (!OtherworldMusicUnlocked && OTHERWORLD_MUSIC_BOXES.Contains(id))
                    continue;
                if (ModeOk(Tracker.Sample(id)) && !me.obtained.Contains(id))
                {
                    allCollected = false;
                    break;
                }
            }
        }
        return (loot, allCollected);
    }

    void UpdateCredits()
    {
        Title(LeftPage, "Log.Credits.Devs");
        Title(RightPage, "Log.Credits.Mods");
        var notice = new PageText(L("Log.Credits.Notice"), C.Salmon);
        notice.Left.Set((int)(RightPage.Width.Pixels / 2 - D.Measure(notice.text, View.K).X / 2), 0);
        notice.Top.Set(P(56), 0);
        RightPage.Append(notice);

        Texture2D devSlot = Tex("Credits_Panel_Dev");
        int dw = !devSlot.IsNull ? devSlot.Width : 320, dh = !devSlot.IsNull ? devSlot.Height : 80;
        LogList credits = Track(new LogList());
        Place(credits, (int)(375 / 2f - dw / 2f), 60, dw, dh * 4 + 20);
        credits.ListPadding = P(5);
        credits.ManualSortMethod = _ => { };
        credits.AddRange(Contributors.Select(c => (UIElement)ContributorCredit.Dev(devSlot, Tex("Credits_" + c.name), c.name, c.title)));
        LeftPage.Append(credits);

        Texture2D modSlot = Tex("Credits_Panel_Mod");
        int mw = !modSlot.IsNull ? modSlot.Width : 320, mh = !modSlot.IsNull ? modSlot.Height : 96;
        LogList mods = Track(new LogList());
        Place(mods, (int)(375 / 2f - mw / 2f), 85, mw, mh * 3 + 15);
        mods.ListPadding = P(5);
        mods.ManualSortMethod = _ => { };
        var cards = new List<UIElement>();
        foreach (RegisteredMod registered in Tracker.registeredMods.Values)
            cards.Add(ContributorCredit.Mod(modSlot, registered));
        if (cards.Count == 0)
            cards.Add(ContributorCredit.Text(Tex("Credits_Panel_NoMods"), "NoMods", "Credits.ModsEmpty"));
        cards.Add(ContributorCredit.Text(Tex("Credits_Panel_Register"), "Register", "Credits.Register", "Credits.Learn"));
        mods.AddRange(cards);
        RightPage.Append(mods);
    }

    void OpenRecord()
    {
        EntryInfo? entry = GetLogEntryInfo;
        if (entry == null)
            return;
        Texture2D slotTex = Tex("Extra_RecordSlot");
        int slotW = !slotTex.IsNull ? slotTex.Width : 320;
        if (entry.type != BOSS)
        {
            RecordDisplaySlot only = RecordDisplaySlot.ForEntry(slotTex, entry);
            Place(only, (int)(375 / 2f - slotW / 2f), 35 + 75);
            RightPage.Append(only);
            AppendRelatedIcons(entry, only);
            if (entry.type == EVENT)
            {
                foreach (int npc in entry.npcIDs)
                {
                    int banner = BannerSystem.NPCtoBanner(npc);
                    if (!EventEntryNPCBannerIDList.Contains(banner))
                        EventEntryNPCBannerIDList.Add(banner);
                }
            }
            return;
        }
        PersonalRecords? records = GetPlayerRecords;
        if (records == null)
            return;
        bool[] conditions = { true, records.UnlockedFirstVictory, records.UnlockedPersonalBest, Util.IsClient };
        int total = conditions.Count(c => c);
        int count = 0;
        SubPageButton anchor = OpenRecordPageButton;
        for (int i = 0; i < RecordCategoryButtons.Length; i++)
        {
            if (!conditions[i])
                continue;
            NavigationalButton button = RecordCategoryButtons[i];
            RightPage.Append(button);
            int xOffset = count % 2 == 0 ? (count + 1 == total ? 15 : 0) : 30;
            int yOffset = count > 1 ? 30 : total > 2 ? 0 : 15;
            button.Left.Set((int)(anchor.Left.Pixels + anchor.Width.Pixels + P(20 + xOffset)), 0);
            button.Top.Set((int)(anchor.Top.Pixels + P(yOffset)), 0);
            button.Recalculate();
            count++;
        }
        if (!conditions[SelectedRecordCategory])
            SelectedRecordCategory = PREVIOUS;
        if (SelectedRecordCategory == WORLD)
            SelectedRecordComparison = NONE;
        if (SelectedRecordComparison != NONE && !conditions[SelectedRecordComparison])
            SelectedRecordComparison = NONE;

        int sub = SelectedRecordCategory, cmp = SelectedRecordComparison;
        WorldRecord? world = GetWorldRecords;
        for (int i = 0; i < 4; i++)
        {
            RecordDisplaySlot slot = RecordDisplaySlot.ForCategory(slotTex, sub, i);
            Place(slot, (int)(375 / 2f - slotW / 2f), 35 + 75 * (i + 1));
            RightPage.Append(slot);
            if (i == 0)
            {
                var icon = new CategoryIcon(Tex(RecordIcons[sub]));
                icon.Left.Set(P(15), 0);
                icon.Top.Set((int)(slot.Height.Pixels / 2 - icon.Height.Pixels / 2), 0);
                slot.Append(icon);
                AppendRelatedIcons(entry, slot);
                continue;
            }
            if (i < 2)
                continue;
            bool duration = i == 2;
            string Conv(int v) => duration ? Records.TimeConversion(v) : Records.HitCount(v);
            (string text, Color? color) Diff(int a, int b) => duration ? Records.TimeDiff(a, b) : Records.HitDiff(a, b);
            NavigationalButton? trophy = null;
            if (sub == WORLD)
            {
                trophy = new NavigationalButton(Tracker.ItemTexture(4601), false)
                {
                    hoverText = world != null ? world.Holders(duration) : L("Log.Records.ClaimRecord"),
                };
            }
            else if (cmp != NONE)
            {
                int value = records.Stat(sub, duration);
                int comp = cmp == WORLD ? (world != null ? (duration ? world.durationWorld : world.hitsTakenWorld) : -1) : records.Stat(cmp, duration);
                (string text, Color? color) d = Diff(value, comp);
                trophy = new NavigationalButton(Tracker.ItemTexture(4600), false)
                {
                    Id = "CompareStat",
                    hoverText = "[c/F5DEB3:" + L("Log.Records.Category." + Names[cmp]) + ": " + Conv(comp) + "]" + (d.text.Length > 0 ? "\n" + d.text : ""),
                    hoverTextColor = d.color,
                };
            }
            else if (sub == BEST && (duration ? records.durationPrevBest : records.hitsTakenPrevBest) != -1)
            {
                int prev = duration ? records.durationPrevBest : records.hitsTakenPrevBest;
                (string text, Color? color) d = Diff(records.Stat(BEST, duration), prev);
                trophy = new NavigationalButton(Tracker.ItemTexture(4599), false)
                {
                    hoverText = "[c/F5DEB3:" + L("Log.Records.PreviousBest") + ": " + Conv(prev) + "]" + (d.text.Length > 0 ? "\n" + d.text : ""),
                    hoverTextColor = d.color,
                };
            }
            if (trophy == null)
                continue;
            trophy.Left.Set(slot.Width.Pixels - (trophy.Width.Pixels * 2) / 3, 0);
            trophy.Top.Set(slot.Height.Pixels / 2 - trophy.Height.Pixels / 2, 0);
            slot.Append(trophy);
            if (trophy.Id == "CompareStat")
            {
                Texture2D ct = Tex(RecordIcons[cmp]);
                var compare = new PictureElement(ct);
                Place(compare, null, null, !ct.IsNull ? ct.Width : 24, !ct.IsNull ? ct.Height : 24);
                compare.Left.Set(-(int)(compare.Width.Pixels / 3), 0);
                compare.Top.Set((int)(trophy.Height.Pixels - (compare.Height.Pixels * 2) / 3), 0);
                compare.IgnoresMouseInteraction = true;
                trophy.Append(compare);
            }
        }
    }

    static void AppendRelatedIcons(EntryInfo entry, RecordDisplaySlot slot)
    {
        float offset = 0;
        foreach (string key in entry.relatedEntries)
        {
            EntryInfo? related = Tracker.FindEntryFromKey(key);
            if (related == null)
                continue;
            Color color = related.IsAutoDownedOrMarked ? C.White : SameColor(MaskBoss(related), C.Black) ? C.Black : C.Faded;
            List<Texture2D> heads = related.Heads();
            Texture2D head = heads.Count > 0 ? heads[0] : TextureAssets.NpcHead[0].Value;
            var icon = new NavigationalButton(head, false, color, 4)
            {
                Id = entry.type == EVENT ? "eventIcon" : "bossIcon",
                Anchor = related.GetIndex,
                hoverText = related.DisplayName + "\n" + L("Log.EntryPage.ViewPage"),
            };
            float left = entry.type == EVENT ? P(15) + offset : (int)(slot.Width.Pixels - icon.Width.Pixels - P(15));
            icon.Left.Set(left, 0);
            icon.Top.Set((int)(slot.Height.Pixels / 2 - icon.Height.Pixels / 2), 0);
            slot.Append(icon);
            offset += P(10) + icon.Width.Pixels;
        }
    }

    public void ResetStats()
    {
        if (!Cfg.Log.EnabledResetOptions || SelectedSubPage != SUB_RECORDS)
            return;
        PersonalRecords? r = GetPlayerRecords;
        if (r == null)
            return;
        int flag = r.Reset(SelectedRecordCategory);
        if (flag != 0 && Util.IsClient)
        {
            int ri = GetLogEntryInfo!.RecordIndex;
            Net.ToServer(PacketKind.Reset, w =>
            {
                w.Write(ri);
                w.Write(flag);
            });
        }
        Sound(SoundID.ResearchComplete);
        RefreshPageContent();
    }

    public void ClearLoot(EntryInfo? entry, bool all)
    {
        BossChecklistPlayer? me = State.Me;
        if (me == null || entry == null)
            return;
        foreach (int id in entry.lootItemTypes)
            me.obtained.Remove(id);
        if (all)
        {
            foreach (int id in entry.collectibles.Keys)
                me.obtained.Remove(id);
        }
        Sound(SoundID.ResearchComplete);
        RefreshPageContent();
    }

    void OpenSpawn()
    {
        EntryInfo? entry = GetLogEntryInfo;
        if (entry == null || entry.modSource == "Unknown")
            return;
        var message = new UIMessageBox(entry.DisplaySpawnInfo);
        message.SetPadding(P(12));
        message.Width.Set(-P(10), 1);
        message.Height.Set(-P(370), 1);
        message.Top.Set(P(85), 0);
        message.Left.Set(P(5), 0);
        RightPage.Append(message);

        List<int> items = entry.spawnItem;
        if (SpawnItemSelected >= items.Count)
            SpawnItemSelected = 0;
        if (items.Count == 0 || items[SpawnItemSelected] <= 0)
        {
            string text = L("Log.SpawnInfo.NoSpawnItem", L("Log.Common." + Tracker.TypeName(entry.type)));
            var info = new PageText(text, C.White);
            info.Left.Set(RightPage.Width.Pixels / 2 - D.Measure(text, View.K).X / 2 - P(5), 0);
            info.Top.Set(P(300), 0);
            RightPage.Append(info);
            return;
        }

        int itemType = items[SpawnItemSelected];
        var spawnSlot = new LogItemSlot(itemType, SlotContext.EquipDye);
        if (entry.Key == "Terraria TorchGod" && itemType == Torch)
            spawnSlot.stack = 101;
        Place(spawnSlot, 48 + 56 * 2, 230);
        RightPage.Append(spawnSlot);
        if (SpawnItemSelected > 0)
        {
            var prev = new NavigationalButton(Tex("Nav_Prev"), true, null, 10) { Id = "PrevItem" };
            prev.Left.Set(spawnSlot.Left.Pixels - prev.Width.Pixels - P(6), 0);
            prev.Top.Set(spawnSlot.Top.Pixels + spawnSlot.Height.Pixels / 2 - prev.Height.Pixels / 2, 0);
            prev.OnLeftClick += ChangeSpawnItem;
            RightPage.Append(prev);
        }
        if (SpawnItemSelected < items.Count - 1)
        {
            var next = new NavigationalButton(Tex("Nav_Next"), true, null, 10) { Id = "NextItem" };
            next.Left.Set(spawnSlot.Left.Pixels + spawnSlot.Width.Pixels + P(6), 0);
            next.Top.Set(spawnSlot.Top.Pixels + spawnSlot.Height.Pixels / 2 - next.Height.Pixels / 2, 0);
            next.OnLeftClick += ChangeSpawnItem;
            RightPage.Append(next);
        }

        List<RecipeInfo> recipes = Recipes(itemType);
        if (recipes.Count > 0 && RecipeSelected >= recipes.Count)
            RecipeSelected = 0;
        if (recipes.Count == 0)
        {
            var text = new PageText(L("Log.SpawnInfo.Noncraftable"), C.White, 0.8f);
            Place(text, 10, 205);
            RightPage.Append(text);
            return;
        }
        RecipeInfo recipe = recipes[RecipeSelected];
        var from = new PageText(L("Log.SpawnInfo.RecipeFrom", "Terraria"), C.White, 0.8f);
        Place(from, 10, 205);
        RightPage.Append(from);
        if (recipes.Count > 1)
        {
            var cycle = new NavigationalButton(Tex("Extra_CycleRecipe"), true, null, 4)
            {
                Id = "CycleItem_" + recipes.Count,
                hoverText = "Log.SpawnInfo.CycleRecipe",
            };
            cycle.Left.Set(P(20 + 48 * 6 + (int)(52 * 0.85f) / 2f) - cycle.Width.Pixels / 2, 0);
            cycle.Top.Set(spawnSlot.Top.Pixels + (int)(spawnSlot.Height.Pixels / 2 - cycle.Height.Pixels / 2), 0);
            cycle.OnLeftClick += ChangeSpawnItem;
            RightPage.Append(cycle);
        }

        Player p = Util.Local;
        int row = 0, col = 0;
        foreach (IngredientInfo ing in recipe.items)
        {
            var slot = new LogItemSlot(ing.type, SlotContext.GuideItem, 0.85f)
            {
                Id = LogItemSlot.SpawnItemCraftingSlot,
                stack = ing.stack,
                nameOverride = ing.group,
                hasItem = !p.IsNull && HasItem(p, ing),
            };
            Place(slot, 20 + 48 * col, 240 + 48 * (row + 1));
            RightPage.Append(slot);
            col++;
            if (col == 7)
            {
                col = 0;
                row++;
                if (row == 2)
                    break;
            }
        }
        if (col != 0)
        {
            col = 0;
            row++;
        }
        if (recipe.tiles.Count == 0)
        {
            var hand = new LogItemSlot(HandOfCreation, SlotContext.EquipArmorVanity, 0.85f)
            {
                Id = LogItemSlot.SpawnItemCraftingSlot,
                hoverText = "Log.SpawnInfo.ByHand",
                hasItem = true,
            };
            Place(hand, 20, 240 + 48 * (row + 1));
            RightPage.Append(hand);
            return;
        }
        for (int i = 0; i < recipe.tiles.Count; i++)
        {
            int tile = recipe.tiles[i];
            var slot = new LogItemSlot(Stations.TryGetValue(tile, out int station) ? station : 0, SlotContext.EquipArmorVanity, 0.85f)
            {
                Id = LogItemSlot.SpawnItemCraftingSlot,
                tile = tile,
            };
            string tileName = TileName(tile);
            slot.hoverText = tileName.Length > 0 ? tileName : null;
            slot.hasItem = !p.IsNull && tile < p.adjTile.Length && p.adjTile[tile];
            Place(slot, 20 + 48 * i, 240 + 48 * (row + 1));
            RightPage.Append(slot);
        }
    }

    void ChangeSpawnItem(UIMouseEvent evt, UIElement button)
    {
        if (!Gesture.IsTap(evt))
            return;
        string id = ((LogUIElement)button).Id;
        if (id == "NextItem")
        {
            SpawnItemSelected++;
            RecipeSelected = 0;
        }
        else if (id == "PrevItem")
        {
            SpawnItemSelected--;
            RecipeSelected = 0;
        }
        else if (id.Contains("CycleItem"))
        {
            int total = int.Parse(id[(id.IndexOf('_') + 1)..]);
            RecipeSelected = RecipeSelected == total - 1 ? 0 : RecipeSelected + 1;
        }
        RefreshPageContent();
    }

    void OpenLoot()
    {
        EntryInfo? entry = GetLogEntryInfo;
        if (entry == null)
            return;
        LogList list = Track(new LogList());
        Place(list, 0, 125, 375 - 25, 480 - 125 - 80);
        list.ListPadding = P(5);
        var bag = new TreasureBag(entry.TreasureBag);
        bag.Left.Set(RightPage.Width.Pixels / 2 - bag.Width.Pixels / 2, 0);
        bag.Top.Set(P(88), 0);
        RightPage.Append(bag);

        BossChecklistPlayer? me = State.Me;
        int bagId = entry.TreasureBag;
        HashSet<int> hidden = entry.loot.Where(d => !d.shown).Select(d => d.itemId).ToHashSet();
        var items = new List<int>();
        void Add(int id)
        {
            if (id > 0 && !items.Contains(id))
                items.Add(id);
        }
        foreach (int type in new[] { RELIC, PET, TROPHY, MASK, MUSIC })
        {
            foreach (int id in entry.collectibles.Keys)
            {
                if (entry.collectibles.Get(id) == type)
                    Add(id);
            }
        }
        int expert = entry.ExpertItem;
        if (expert > 0 && !items.Contains(expert))
        {
            if (items.Count >= TROPHY)
                items.Insert(TROPHY, expert);
            else
                items.Add(expert);
        }
        foreach (DropInfo d in entry.loot)
        {
            if (d.itemId != bagId && !Coins.Contains(d.itemId) && d.shown)
                Add(d.itemId);
        }
        foreach (int id in entry.lootItemTypes)
        {
            if (id != bagId && !Coins.Contains(id) && !hidden.Contains(id))
                Add(id);
        }
        foreach (int id in entry.collectibles.Keys)
            Add(id);
        if (items.Any(OTHERWORLD_MUSIC_BOXES.Contains))
            OtherworldMusicUnlocked = Main.TOWMusicUnlocked;

        var row = new LootRow(0);
        int col = 0, rows = 0;
        bool journey = Util.Local.difficulty == 3;
        foreach (int id in items)
        {
            if (Cfg.Log.OnlyCheckDroppedLoot && !entry.lootItemTypes.Contains(id))
                continue;
            var slot = new LogItemSlot(id, SlotContext.TrashItem)
            {
                Id = "loot_" + id,
                hasItem = me != null && me.obtained.Contains(id),
                itemResearched = journey && BossChecklistPlayer.IsResearched(id),
            };
            slot.Left.Set(P(col * 56 + 15), 0);
            row.Append(slot);
            col++;
            if (col == 6)
            {
                col = 0;
                rows++;
                list.Add(row);
                row = new LootRow(rows);
            }
        }
        if (col != 0)
        {
            rows++;
            list.Add(row);
        }
        RightPage.Append(list);
    }

    void OpenProgressionModePrompt()
    {
        page = PAGE_PROMPT;
        ResetUIPositioning();
        LeftPage.RemoveAllChildren();
        RightPage.RemoveAllChildren();
        lists = new List<LogList>();
        listsKey = "";

        var text = new FittedTextPanel("ProgressionMode.Description", 1, true);
        Place(text, 10, 60, 375 - 30, 480 - 70);
        LeftPage.Append(text);

        Texture2D slot = Tex("Extra_PromptSlot");
        int sw = !slot.IsNull ? slot.Width : 100, sh = !slot.IsNull ? slot.Height : 100;
        LogUIElement Choice(string picture, string hover, float left, bool enabled)
        {
            var el = new LogUIElement(slot) { hoverText = hover };
            Place(el, left, 125);
            el.OnLeftClick += (evt, _) =>
            {
                if (Gesture.IsTap(evt))
                    SelectProgressionModeState(enabled);
            };
            el.OnMouseOver += (_, _) => el.assetColor = LogColor();
            el.OnMouseOut += (_, _) => el.assetColor = C.White;
            Texture2D pt = Tex(picture);
            if (!pt.IsNull)
            {
                var image = new PictureElement(pt);
                Place(image, null, null, pt.Width, pt.Height);
                image.Left.Set(el.Width.Pixels / 2 - image.Width.Pixels / 2, 0);
                image.Top.Set(el.Height.Pixels / 2 - image.Height.Pixels / 2, 0);
                image.IgnoresMouseInteraction = true;
                el.Append(image);
            }
            RightPage.Append(el);
            return el;
        }
        Choice("Extra_ProgressiveOn", "Log.ProgressionMode.SelectEnable", 375 / 2f - sw - 10, true);
        LogUIElement disable = Choice("Extra_ProgressiveOff", "Log.ProgressionMode.SelectDisable", 375 / 2f + 10, false);

        var toggle = new LogUIElement(Tex("Extra_RecordSlot"));
        Place(toggle, 25, 125 + sh + 25);
        toggle.OnMouseOver += (_, _) => disable.assetColor = LogColor();
        toggle.OnMouseOut += (_, _) => disable.assetColor = C.White;
        Texture2D bt = Tex("Checks_Box");
        var box = new PictureElement(bt);
        Place(box, 15, null, !bt.IsNull ? bt.Width : 22, !bt.IsNull ? bt.Height : 20);
        box.Top.Set(toggle.Height.Pixels / 2 - box.Height.Pixels / 2, 0);
        box.IgnoresMouseInteraction = true;
        var check = new PictureElement(Tex(Cfg.Log.PromptDisabled ? "Checks_Check" : "Checks_X"));
        Place(check, 0, 0, !bt.IsNull ? bt.Width : 22, !bt.IsNull ? bt.Height : 20);
        check.IgnoresMouseInteraction = true;
        toggle.OnLeftClick += (evt, _) =>
        {
            if (Gesture.IsTap(evt))
                DisablePromptMessage(check);
        };
        var options = new FittedTextPanel("ProgressionMode.DisablePrompt");
        options.Width.Set(toggle.Width.Pixels - (box.Left.Pixels + box.Width.Pixels + P(15)), 0);
        options.Height.Set(toggle.Height.Pixels / 2, 0);
        options.Left.Set(box.Left.Pixels + box.Width.Pixels, 0);
        options.Top.Set(P(5), 0);
        options.PaddingTop = 0;
        options.PaddingLeft = P(15);
        options.IgnoresMouseInteraction = true;
        toggle.Append(options);
        box.Append(check);
        toggle.Append(box);
        RightPage.Append(toggle);
        Recalculate();
    }

    void SelectProgressionModeState(bool enabled)
    {
        Cfg.SetLog(c =>
        {
            c.ProgressiveChecklist = enabled;
            if (enabled)
                c.DrawNextMark = true;
        });
        Tracker.Invalidate();
        Sound(SoundID.MenuTick);
        UpdateIndicators();
        page = PAGE_TOC;
        PageNum = PAGE_TOC;
    }

    static void DisablePromptMessage(PictureElement image)
    {
        Cfg.SetLog(c => c.PromptDisabled = !c.PromptDisabled);
        Sound(SoundID.MenuTick);
        image.SetImage(Tex(Cfg.Log.PromptDisabled ? "Checks_Check" : "Checks_X"));
    }

    sealed record IngredientInfo(int type, int stack, string? group);

    sealed record RecipeInfo(List<IngredientInfo> items, List<int> tiles);

    static readonly Dictionary<int, List<RecipeInfo>> recipeCache = new();

    static List<RecipeInfo> Recipes(int type)
    {
        if (recipeCache.TryGetValue(type, out List<RecipeInfo>? cached))
            return cached;
        var found = new List<RecipeInfo>();
        Util.Safe("recipes " + type, () =>
        {
            int n = Recipe.numRecipes;
            for (int i = 0; i < n; i++)
            {
                Recipe rc = Main.recipe[i];
                if (rc.IsNull || rc.createItem.type != type || rc.Disabled)
                    continue;
                var items = new List<IngredientInfo>();
                GameArray<Item> required = rc.requiredItem;
                for (int j = 0; j < required.Length; j++)
                {
                    Item it = required[j];
                    int t = it.IsNull ? 0 : it.type;
                    if (t <= 0)
                        break;
                    items.Add(new IngredientInfo(t, it.stack, GroupName(rc, t)));
                }
                var tiles = new List<int>();
                int tile = rc.requiredTile;
                if (tile >= 0)
                    tiles.Add(tile);
                found.Add(new RecipeInfo(items, tiles));
            }
        });
        recipeCache[type] = found;
        return found;
    }

    // The game's own lookup: the name of the recipe group this ingredient belongs to, if any.
    static string? GroupName(Recipe recipe, int type)
    {
        if (!recipe.ProcessGroupsForText(type, out GameString text) || text.IsNull)
            return null;
        string name = text.ToString() ?? "";
        return name.Length > 0 ? name : null;
    }

    static bool HasItem(Player p, IngredientInfo ing)
    {
        for (int i = 0; i < 58; i++)
        {
            Item it = p.inventory[i];
            if (!it.IsNull && it.type == ing.type && it.stack >= ing.stack)
                return true;
        }
        return false;
    }

    static string TileName(int tile) => Lang.GetMapObjectName(MapHelper.TileToLookup(tile, 0)) ?? "";
}
