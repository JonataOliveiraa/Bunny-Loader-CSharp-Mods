using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BossChecklistMod.Common.Players;
using BossChecklistMod.Common.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static BossChecklistMod.Common.ModText;
using static BossChecklistMod.Common.UI.Common;

namespace BossChecklistMod.Common.UI;

public static class LogPages
{
    public const int PAGE_TOC = -1, PAGE_CREDITS = -2, PAGE_PROMPT = -3;
    public const int SUB_RECORDS = 0, SUB_SPAWN = 1, SUB_LOOT = 2;
    public static readonly string[] SUB_NAMES = { "Records", "SpawnInfo", "LootAndCollectibles" };
    public const int WALL_OF_FLESH = 7;

    public static void Sprite(Texture2D texture, CalculatedStyle d, Color? color = null, Rectangle? src = null, bool flip = false)
    {
        if (!texture.IsNull)
            D.Sprite(texture, d.X, d.Y, d.Width, d.Height, color ?? C.White, src, flip);
    }

    public static void At(Texture2D texture, float x, float y, Color? color = null, float? scale = null)
    {
        if (texture.IsNull)
            return;
        float s = scale ?? View.K;
        D.Sprite(texture, x, y, texture.Width * s, texture.Height * s, color ?? C.White);
    }

    public static Color MaskBoss(EntryInfo entry)
    {
        if (!Cfg.Log.ProgressiveChecklist)
            return C.White;
        return entry.IsUpNext ? C.Black : C.White;
    }

    public static bool SameColor(Color a, Color b) => a.R == b.R && a.G == b.G && a.B == b.B && a.A == b.A;
}

public class LogUIElement : UIElement
{
    public string Id = "";
    public string? hoverText;
    public object?[] hoverTextParams = Array.Empty<object?>();
    public Color? hoverTextColor;
    public Texture2D asset;
    public Color assetColor = C.White;

    public LogUIElement() { }

    public LogUIElement(Texture2D texture)
    {
        if (texture.IsNull)
            return;
        asset = texture;
        Width.Set(P(texture.Width), 0);
        Height.Set(P(texture.Height), 0);
    }

    protected static BossLogUI LogUI => Refs.log!;

    public override void Draw(SpriteBatch spriteBatch)
    {
        bool on = Gesture.On(this);
        if (on)
            HideMouseOverInteractions();
        LogPages.Sprite(asset, GetInnerDimensions(), assetColor);
        base.Draw(spriteBatch);
        if (on && hoverText != null)
            Hover.Set(hoverText, hoverTextParams, hoverTextColor);
    }
}

public class LogList : UIList
{
    bool blockDrag;

    public List<UIElement> Items => _items;

    public override bool ContainsPoint(Vector2 point) => !blockDrag && base.ContainsPoint(point);

    public override void Update(GameTime gameTime)
    {
        bool startedHere = Gesture.pressed && base.ContainsPoint(new Vector2(Gesture.startX, Gesture.startY));
        blockDrag = Main.mouseLeft && !startedHere;
        try
        {
            base.Update(gameTime);
        }
        finally
        {
            blockDrag = false;
        }
    }
}

public class NavigationalButton : LogUIElement, ILongPress
{
    public int? Anchor;
    public int? Record_Anchor;

    readonly Texture2D texture;
    readonly Color iconColor;
    readonly bool hoverButton;
    readonly int pad;

    public NavigationalButton(Texture2D texture, bool hoverButton, Color? color = null, int pad = 0)
    {
        this.texture = texture;
        Width.Set(P(!texture.IsNull ? texture.Width : 18), 0);
        Height.Set(P(!texture.IsNull ? texture.Height : 20), 0);
        iconColor = hoverButton || color == null ? C.White : color.Value;
        this.hoverButton = hoverButton;
        this.pad = pad;
    }

    public override bool ContainsPoint(Vector2 point)
    {
        CalculatedStyle d = GetDimensions();
        float p = P(pad);
        return point.X >= d.X - p && point.X < d.X + d.Width + p && point.Y >= d.Y - p && point.Y < d.Y + d.Height + p;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Gesture.IsTap(evt))
            return;
        BossLogUI log = LogUI;
        if (Anchor != null)
            log.PendingPageNum = Anchor.Value;
        if (Record_Anchor != null)
        {
            if (log.SelectedRecordCategory != Record_Anchor.Value)
                Sound(SoundID.MenuTick);
            log.SelectedRecordCategory = Record_Anchor.Value;
            if (Record_Anchor.Value == log.SelectedRecordComparison)
                log.SelectedRecordComparison = RecordCategory.NONE;
            log.RefreshPageContent();
        }
        if (Id == "CopyKey")
        {
            Util.Say(log.GetLogEntryInfo?.Key, C.White);
            Sound(SoundID.Unlock);
        }
    }

    public bool LongPress()
    {
        BossLogUI log = LogUI;
        if (Record_Anchor == null || Record_Anchor.Value == log.SelectedRecordCategory)
            return false;
        log.SelectedRecordComparison = log.SelectedRecordComparison == Record_Anchor.Value ? RecordCategory.NONE : Record_Anchor.Value;
        Sound(SoundID.MenuTick);
        log.RefreshPageContent();
        return true;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        base.MouseOver(evt);
        if (hoverButton)
            Sound(SoundID.MenuTick);
    }

    Color HoverColor => Gesture.On(this) ? C.White : C.Faded;

    public override void Draw(SpriteBatch spriteBatch)
    {
        Color color;
        if (Record_Anchor != null)
        {
            BossLogUI log = LogUI;
            if (log.SelectedRecordCategory == Record_Anchor.Value)
                color = iconColor;
            else if (log.SelectedRecordComparison == Record_Anchor.Value)
                color = C.Gold;
            else
                color = HoverColor;
        }
        else
        {
            color = hoverButton ? HoverColor : iconColor;
        }
        LogPages.Sprite(texture, GetInnerDimensions(), color);
        base.Draw(spriteBatch);
    }
}

public class IndicatorPanel : LogUIElement, ILongPress
{
    public IndicatorPanel(int iconCount)
    {
        Width.Set(P(10 * 2 + (18 + 2) * iconCount - 2), 0);
        Height.Set(P(26), 0);
    }

    public bool LongPress()
    {
        if (Id != "Interactions" || !Cfg.Log.EnabledResetOptions)
            return false;
        BossLogUI log = LogUI;
        if (log.PageNum == LogPages.PAGE_TOC)
        {
            if (log.HiddenEntriesMode)
            {
                log.HiddenEntriesPending.Clear();
                WorldData.ClearHidden();
            }
            else
            {
                WorldData.ClearMarked();
            }
            Sound(SoundID.ResearchComplete);
            log.RefreshPageContent();
            return true;
        }
        if (log.PageNum < 0)
            return false;
        EntryInfo? entry = log.GetLogEntryInfo;
        if (log.SelectedSubPage == LogPages.SUB_RECORDS && entry?.type == Data.BOSS)
            log.ResetStats();
        else if (log.SelectedSubPage == LogPages.SUB_LOOT)
            log.ClearLoot(entry, true);
        return true;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        BossLogUI log = LogUI;
        if (log.PageNum == LogPages.PAGE_PROMPT)
            return;
        if (Id == "Interactions" && hoverText == null)
            return;
        CalculatedStyle d = GetInnerDimensions();
        Texture2D end = Tex("LogUI_IndicatorEnd"), section = Tex("LogUI_IndicatorSection");
        float ew = P(!end.IsNull ? end.Width : 10), eh = P(!end.IsNull ? end.Height : 26);
        LogPages.Sprite(end, new CalculatedStyle(d.X, d.Y, ew, eh));
        LogPages.Sprite(section, new CalculatedStyle(d.X + ew, d.Y, d.Width - ew * 2, d.Height));
        LogPages.Sprite(end, new CalculatedStyle(d.X + d.Width - ew, d.Y, ew, eh), C.White, null, true);
        if (Id == "Configurations")
        {
            Texture2D back = Tex("Indicator_Back");
            int bw = !back.IsNull ? back.Width : 18;
            for (int i = 0; i < Elements.Count; i++)
                LogPages.At(back, d.X + ew + P((bw + 2) * i), d.Y + P(6));
        }
        base.Draw(spriteBatch);
    }
}

public class IndicatorIcon : LogUIElement
{
    public Color Color = C.White;

    readonly Texture2D texture;

    public IndicatorIcon(Texture2D texture)
    {
        this.texture = texture;
        Width.Set(P(!texture.IsNull ? texture.Width : 14), 0);
        Height.Set(P(!texture.IsNull ? texture.Height : 14), 0);
    }

    public override bool ContainsPoint(Vector2 point)
    {
        CalculatedStyle d = GetDimensions();
        float p = P(3);
        return point.X >= d.X - p && point.X < d.X + d.Width + p && point.Y >= d.Y - p && point.Y < d.Y + d.Height + p;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Gesture.IsTap(evt))
            return;
        switch (Id)
        {
            case "Progression":
                Cfg.SetLog(c => c.ProgressiveChecklist = !c.ProgressiveChecklist);
                break;
            case "Manual":
                Cfg.SetLog(c => c.AutomaticChecklist = !c.AutomaticChecklist);
                break;
            case "OnlyBosses":
                Cfg.SetLog(c => c.OnlyShowBossContent = !c.OnlyShowBossContent);
                break;
            default:
                return;
        }
        Tracker.Invalidate();
        Sound(SoundID.MenuTick);
        LogUI.UpdateIndicators();
        LogUI.RefreshPageContent();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        LogPages.Sprite(texture, GetInnerDimensions(), Color);
        base.Draw(spriteBatch);
    }
}

public class FilterIcon : LogUIElement
{
    readonly Texture2D icon;
    Texture2D check;

    public FilterIcon(Texture2D icon)
    {
        this.icon = icon;
        Width.Set(P(!icon.IsNull ? icon.Width : 24), 0);
        Height.Set(P(!icon.IsNull ? icon.Height : 24), 0);
    }

    string ConfigValue() => Id switch
    {
        "Boss" => Cfg.Log.FilterBosses.ToString(),
        "MiniBoss" => Cfg.Log.FilterMiniBosses.ToString(),
        "Event" => Cfg.Log.FilterEvents.ToString(),
        _ => "",
    };

    static string CheckFor(string value) => value switch
    {
        "HideWhenCompleted" => "Checks_Next",
        "Hide" => "Checks_X",
        _ => "Checks_Check",
    };

    public void UpdateFilterIcon()
    {
        bool only = Cfg.Log.OnlyShowBossContent;
        bool blocked = only && (Id == "MiniBoss" || Id == "Event");
        check = Id == "Hidden" ? default : Tex(blocked ? "Checks_X" : CheckFor(ConfigValue()));
        if (blocked)
        {
            hoverText = "Log.TableOfContents.Filter.Disabled";
            hoverTextParams = Array.Empty<object?>();
        }
        else if (Id == "Hidden")
        {
            hoverText = "Log.TableOfContents.Filter.ToggleHidden" + (LogUI.HiddenEntriesMode ? "Close" : "Open");
        }
        else
        {
            string enumName = Id == "Boss" ? nameof(BossFilterMode) : nameof(EntryFilterMode);
            hoverText = L("Log.Common." + Id + "Plural") + ": " + L("Configs." + enumName + "." + ConfigValue() + ".Label");
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Gesture.IsTap(evt) || Id.Length == 0)
            return;
        BossLogUI log = LogUI;
        bool only = Cfg.Log.OnlyShowBossContent;
        if (Id == "Hidden")
        {
            log.HiddenEntriesMode = !log.HiddenEntriesMode;
        }
        else if (Id == "Boss" || !only)
        {
            if (Id == "Boss")
            {
                Cfg.SetLog(c => c.FilterBosses = c.FilterBosses == BossFilterMode.Show ? BossFilterMode.HideWhenCompleted : BossFilterMode.Show);
            }
            else if (Id == "MiniBoss")
            {
                Cfg.SetLog(c => c.FilterMiniBosses = Cycle(c.FilterMiniBosses));
            }
            else
            {
                Cfg.SetLog(c => c.FilterEvents = Cycle(c.FilterEvents));
            }
            Tracker.Invalidate();
            Sound(SoundID.MenuTick);
        }
        UpdateFilterIcon();
        log.RefreshPageContent();
    }

    static EntryFilterMode Cycle(EntryFilterMode value) => value switch
    {
        EntryFilterMode.Show => EntryFilterMode.HideWhenCompleted,
        EntryFilterMode.HideWhenCompleted => EntryFilterMode.Hide,
        _ => EntryFilterMode.Show,
    };

    public override void Draw(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetInnerDimensions();
        if (Id == "Hidden")
        {
            bool on = LogUI.HiddenEntriesMode;
            float scale = View.K * (on ? Main.cursorScale : 1f);
            float w = (!icon.IsNull ? icon.Width : 24) * scale, h = (!icon.IsNull ? icon.Height : 24) * scale;
            if (!icon.IsNull)
                D.Sprite(icon, d.X + d.Width / 2 - w / 2, d.Y + d.Height / 2 - h / 2, w, h, on ? C.White : C.DimGray);
        }
        else
        {
            LogPages.Sprite(icon, d);
        }
        if (!check.IsNull)
            LogPages.At(check, d.X + d.Width - P(10), d.Y + d.Height - P(15));
        base.Draw(spriteBatch);
    }
}

public class SubPageButton : LogUIElement
{
    readonly int subPageType;
    readonly string buttonKey;

    public SubPageButton(Texture2D texture, int type) : base(texture)
    {
        subPageType = type;
        buttonKey = "Log.Tabs." + LogPages.SUB_NAMES[type];
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Gesture.IsTap(evt))
            return;
        BossLogUI log = LogUI;
        if (log.SelectedSubPage != subPageType)
            Sound(SoundID.MenuTick);
        log.SelectedSubPage = subPageType;
        log.RefreshPageContent();
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        CalculatedStyle d = GetInnerDimensions();
        BossLogUI log = LogUI;
        if (subPageType == log.SelectedSubPage)
            LogPages.Sprite(Tex("Nav_SubPage_Border"), d);
        EntryInfo? entry = log.GetLogEntryInfo;
        bool useKillCount = subPageType == LogPages.SUB_RECORDS && entry != null && entry.type != Data.BOSS;
        string text = useKillCount ? Hover.Resolve("LegacyInterface.101") : L(buttonKey);
        float scale = FitScale(text, d.Width - P(20), 0.9f * View.K);
        D.Text(text, d.X + (d.Width - D.Measure(text, scale).X) / 2, d.Y + P(5), C.Gold, scale);
    }
}

public class TreasureBag : LogUIElement, ILongPress
{
    readonly int itemType;
    readonly Texture2D bagTexture;

    public TreasureBag(int bag)
    {
        itemType = bag > 0 ? bag : 0;
        bagTexture = itemType != 0 ? Tracker.ItemTexture(itemType) : Tex("Extra_TreasureBag");
        int size = !bagTexture.IsNull ? bagTexture.Height : 32;
        Width.Set(P(size), 0);
        Height.Set(P(size), 0);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (itemType != 0 && Gesture.IsTap(evt))
            Hover.Pin(itemType);
    }

    public bool LongPress()
    {
        if (!Cfg.Log.EnabledResetOptions || LogUI.SelectedSubPage != LogPages.SUB_LOOT)
            return false;
        LogUI.ClearLoot(LogUI.GetLogEntryInfo, false);
        return true;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetInnerDimensions();
        if (itemType != 0)
        {
            if (D.ItemFrame(itemType, out Texture2D texture, out Rectangle frame))
            {
                float s = Math.Min(d.Width / frame.Width, d.Height / frame.Height);
                float w = frame.Width * s, h = frame.Height * s;
                D.Sprite(texture, d.X + (d.Width - w) / 2, d.Y + (d.Height - h) / 2, w, h, C.White, frame);
            }
            if (Gesture.On(this))
                Hover.Item(itemType);
        }
        else
        {
            LogPages.Sprite(bagTexture, d);
        }
        base.Draw(spriteBatch);
    }
}

public static class SlotContext
{
    // The number of the game's InventoryBack texture.
    public const int EquipDye = 12, GuideItem = 4, TrashItem = 7, EquipArmorVanity = 8;
}

public class LogItemSlot : LogUIElement, ILongPress
{
    public const string SpawnItemCraftingSlot = "SpawnItemCraftingSlot";

    public bool hasItem, itemResearched;
    public string? nameOverride;
    public int tile = -1, stack;

    readonly int item, context;

    struct SlotRestrictions
    {
        public bool master, expert, music, restricted, progress;
    }

    public LogItemSlot(int item, int context = SlotContext.TrashItem, float scale = 1)
    {
        this.item = item;
        this.context = context;
        Width.Set(P(52 * scale), 0);
        Height.Set(P(52 * scale), 0);
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (Gesture.IsTap(evt) && CanShowTooltip())
            Hover.Pin(item, nameOverride);
    }

    public bool LongPress()
    {
        if (Id == SpawnItemCraftingSlot || !Cfg.Log.EnabledResetOptions)
            return false;
        BossLogUI log = LogUI;
        if (log.SelectedSubPage != LogPages.SUB_LOOT)
            return false;
        BossChecklistPlayer? me = State.Me;
        if (me != null && me.obtained.Remove(item))
            Sound(SoundID.Unlock);
        log.RefreshPageContent();
        return true;
    }

    bool IsLootSlot => Id.Contains("loot_");

    SlotRestrictions Restrictions()
    {
        EntryInfo entry = LogUI.GetLogEntryInfo!;
        bool isMasterPet = entry.collectibles.Get(item) == Data.PET;
        bool master = (item == entry.Relic || isMasterPet) && !Main.masterMode;
        bool expert = item == entry.ExpertItem && !Main.expertMode;
        bool music = Data.OTHERWORLD_MUSIC_BOXES.Contains(item) && !LogUI.OtherworldMusicUnlocked;
        bool progress = !hasItem && IsLootSlot && Cfg.Log.ProgressiveChecklist && !entry.IsAutoDownedOrMarked;
        return new SlotRestrictions { master = master, expert = expert, music = music, restricted = master || expert || music, progress = progress };
    }

    bool CanShowTooltip()
    {
        if (item <= 0)
            return false;
        if (Id == SpawnItemCraftingSlot || !IsLootSlot)
            return hoverText == null;
        SlotRestrictions r = Restrictions();
        return hasItem || (!r.restricted && !r.progress);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        CalculatedStyle d = GetInnerDimensions();
        bool on = Gesture.On(this);
        if (on)
            HideMouseOverInteractions();
        if (item <= 0 && hoverText == null)
            return;
        float cx = d.X + d.Width / 2, cy = d.Y + d.Height / 2;

        if (Id == SpawnItemCraftingSlot || !IsLootSlot)
        {
            SlotBack(context, d.X, d.Y, d.Width, d.Height);
            if (tile == 26)
                DrawAltar(d);
            else
                ItemInSlot(item, d.X, d.Y, d.Width, d.Height, C.White, stack);
            if (Id == SpawnItemCraftingSlot && Cfg.Log.SpawnItemCraftingChecklist && hasItem)
                LogPages.At(Tex("Checks_Check"), cx, cy);
            if (on)
            {
                if (hoverText != null)
                    Hover.Set(hoverText, hoverTextParams, hoverTextColor);
                else
                    Hover.Item(item, nameOverride);
            }
            return;
        }

        EntryInfo entry = LogUI.GetLogEntryInfo!;
        SlotRestrictions r = Restrictions();
        SlotBack(hasItem ? 3 : r.restricted ? 11 : 7, d.X, d.Y, d.Width, d.Height);
        ItemInSlot(item, d.X, d.Y, d.Width, d.Height, r.progress ? C.Black : C.White);
        if (entry.collectibles.Has(item))
            LogPages.At(Tex("Extra_HighlightedCollectible"), d.X, d.Y, C.White, d.Width / 52);

        if (itemResearched && (hasItem || !r.restricted))
            LogPages.At(Tex("Checks_Researched"), cx, cy);
        if (!hasItem)
        {
            if (r.master)
            {
                Badge("Images/UI/WorldCreation/IconDifficultyMaster", 4929, cx, cy);
                if (on)
                    Hover.Set("Log.LootAndCollection.ItemIsMasterOnly", null, D.Rgb(255, D.Round(Main.masterColor * 200), 0));
            }
            else if (r.expert)
            {
                Badge("Images/UI/WorldCreation/IconDifficultyExpert", 3318, cx, cy);
                if (on)
                    Hover.Set("Log.LootAndCollection.ItemIsExpertOnly", null, Disco());
            }
            else if (r.music)
            {
                Badge("Images/UI/WorldCreation/IconRandomSeed", 5014, cx, cy);
                if (on)
                    Hover.Set("Log.LootAndCollection.ItemIsLocked", null, C.Goldenrod);
            }
            else if (r.progress)
            {
                if (on)
                    Hover.Set("???", null, C.White);
            }
            else if (on)
            {
                Hover.Item(item);
            }
        }
        else
        {
            LogPages.At(Tex("Checks_Check"), cx, cy);
            if (on)
                Hover.Item(item);
        }

        if (Cfg.Log.ShowCollectionType && entry.collectibles.Has(item))
        {
            int type = entry.collectibles.Get(item);
            string badge = type switch
            {
                Data.PET => "Checks_Pet",
                Data.MASK => "Checks_Mask",
                Data.MUSIC => "Checks_Music",
                Data.GENERIC => "Checks_Generic",
                _ => "Checks_Trophy",
            };
            LogPages.At(Tex(badge), d.X - P(4), d.Y + d.Height - P(15));
            if (on && type >= 0 && type < Data.COLLECTIBLE_NAMES.Length)
                D.Text(Data.COLLECTIBLE_NAMES[type], d.X, d.Y, C.Amber, 0.8f * View.K);
        }
    }

    static void DrawAltar(CalculatedStyle d)
    {
        Texture2D texture = TileTexture(26);
        if (texture.IsNull)
            return;
        bool crimson = WorldGen.crimson;
        float s = 0.64f * d.Width / (52 * 0.85f);
        float cx = d.X + d.Width / 2, cy = d.Y + d.Height / 2;
        for (int i = 0; i < 6; i++)
        {
            int ox = i % 3, oy = i / 3;
            int h = 16 + oy * 2;
            var src = new Rectangle(((crimson ? 3 : 0) + ox) * 18, oy * 18, 16, h);
            float x = cx - (16 * s) / 2 + 16 * s * (ox - 1);
            float y = cy - (h * s) / 2 + ((h * s) / 2) * (oy == 0 ? -1 : 1);
            D.Sprite(texture, x, y, 16 * s, h * s, C.White, src);
        }
    }

    static void Badge(string path, int fallbackItem, float cx, float cy)
    {
        Texture2D texture = VanillaTexture(path);
        if (!texture.IsNull)
            LogPages.At(texture, cx, cy);
        else
            ItemInSlot(fallbackItem, cx, cy, P(26), P(26));
    }
}

public class LootRow : LogUIElement
{
    readonly int order;

    public LootRow(int order)
    {
        this.order = order;
        Height.Set(P(50), 0);
        Width.Set(0, 1);
    }

    public override int CompareTo(object? obj) => obj is LootRow other ? order - other.order : 0;
}

public class LogPanel : LogUIElement
{
    public LogPanel() { }

    public LogPanel(Texture2D texture) : base(texture) { }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        CalculatedStyle page = GetInnerDimensions();
        BossLogUI log = LogUI;
        if (Id.Length == 0)
        {
            LogPages.Sprite(Tex("LogUI_Back"), page, LogColor());
            LogPages.Sprite(Tex("LogUI_Paper"), page);
        }
        int selected = log.PageNum;
        float K = View.K;
        if (selected == LogPages.PAGE_PROMPT)
        {
            if (Id == "PageOne")
            {
                D.Text(L("Log.ProgressionMode.BeforeYouBegin"), page.X + P(10), page.Y + P(15), C.White, 0.8f * K);
                string ask = L("Log.ProgressionMode.AskEnable");
                float s = FitScale(ask, page.Width - P(30), K);
                D.Text(ask, page.X + page.Width / 2 - D.Measure(ask, s).X / 2, page.Y + P(40), C.Amber, s);
            }
            else if (Id == "PageTwo")
            {
                string msg = L("Log.ProgressionMode.SelectAnOption");
                float s = FitScale(msg, page.Width - P(30), K);
                D.Text(msg, page.X + page.Width / 2 - D.Measure(msg, s).X / 2, page.Y + P(40), C.Amber, s);
            }
            return;
        }
        if (selected < 0)
            return;
        EntryInfo? entry = log.GetLogEntryInfo;
        if (entry == null)
            return;
        if (Id == "PageOne")
            DrawEntryLeft(entry, page);
        else if (Id == "PageTwo" && entry.modSource != "Unknown" && log.SelectedSubPage == LogPages.SUB_RECORDS && entry.type == Data.EVENT)
            DrawBanners(entry, page);
    }

    static void DrawEntryLeft(EntryInfo entry, CalculatedStyle page)
    {
        float K = View.K;
        Color mask = LogPages.MaskBoss(entry);
        bool custom = false;
        if (entry.customDrawing != null)
        {
            try
            {
                entry.customDrawing(Main.spriteBatch, new Rectangle(D.Round(page.X), D.Round(page.Y), D.Round(page.Width), D.Round(page.Height)), new Color(mask.R, mask.G, mask.B, 255));
                custom = true;
            }
            catch (Exception e)
            {
                Util.LogOnce("portrait " + entry.Key, "custom portrait of " + entry.Key + " failed: " + Util.Message(e));
            }
        }
        if (!custom)
        {
            Texture2D texture = entry.Portrait();
            Rectangle? src = null;
            if (texture.IsNull && entry.npcIDs.Count > 0)
            {
                int type = entry.npcIDs[0];
                texture = NpcTexture(type);
                if (!texture.IsNull)
                {
                    int frames = Math.Max(1, type < Main.npcFrameCount.Length ? Main.npcFrameCount[type] : 1);
                    src = new Rectangle(0, 0, texture.Width, texture.Height / frames);
                }
            }
            if (!texture.IsNull)
            {
                int w = src?.Width ?? texture.Width, h = src?.Height ?? texture.Height;
                float xs = 375f / w, ys = (480f - 150f) / h;
                float s = (xs < 1 || ys < 1 ? Math.Min(xs, ys) : 1) * K;
                D.Sprite(texture, page.X + page.Width / 2 - (w * s) / 2, page.Y + page.Height / 2 - (h * s) / 2, w * s, h * s, mask, src);
            }
        }

        bool anyHead = false;
        CalculatedStyle first = default;
        float lastX = 0, total = 0;
        int offset = 0;
        List<Texture2D> heads = entry.Heads();
        for (int i = heads.Count - 1; i >= 0; i--)
        {
            Texture2D head = heads[i];
            if (head.IsNull)
                continue;
            var src = new Rectangle(0, 0, head.Width, head.Height);
            if (entry.Key == "Terraria Deerclops")
                src = new Rectangle(2, 0, 48, 40);
            float x = page.X + page.Width - P(src.Width) - P(10) - P(src.Width + 2) * offset;
            var r = new CalculatedStyle(x, page.Y + P(5), P(src.Width), P(src.Height));
            if (!anyHead)
            {
                anyHead = true;
                first = r;
            }
            D.Sprite(head, r.X, r.Y, r.Width, r.Height, mask, src);
            offset++;
            total += r.Width;
            lastX = x;
        }
        if (anyHead)
        {
            Texture2D check = Tex(entry.IsAutoDownedOrMarked ? "Checks_Check" : "Checks_X");
            LogPages.At(check, first.X + first.Width / 2, first.Y + first.Height - P(!check.IsNull ? check.Height / 2f : 10));
            if (Gesture.Inside(lastX, first.Y, total + P(2) * (offset - 1), first.Height))
            {
                bool done = entry.IsAutoDownedOrMarked;
                Hover.Set(done ? "Log.EntryPage.Defeated" : "Log.EntryPage.Undefeated",
                    new object?[] { Main.worldName ?? "", entry.MarkedAsDowned ? "*" : "" }, done ? C.Green : C.Red);
            }
        }

        string progression = Cfg.Log.ShowProgressionValue ? "[" + entry.progression.ToString(CultureInfo.InvariantCulture) + "f] " : "";
        D.Text(D.Truncate(progression + entry.DisplayName, page.Width - P(10) - (anyHead ? page.X + page.Width - lastX : 0), K), page.X + P(5), page.Y + P(5), C.Goldenrod, K);
        D.Text(entry.ModDisplayName, page.X + P(5), page.Y + P(30), C.ModBlue, K);
    }

    static void DrawBanners(EntryInfo entry, CalculatedStyle page)
    {
        bool masked = LogPages.SameColor(LogPages.MaskBoss(entry), C.Black);
        int n = 0;
        foreach (int bannerID in LogUI.EventEntryNPCBannerIDList)
        {
            if (bannerID <= 0)
                continue;
            int npcID = BannerSystem.BannerToNPC(bannerID);
            int bannerItem = BannerSystem.BannerToItem(bannerID);
            if (bannerItem <= 0)
                continue;
            if (npcID >= 0 && npcID < NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally.Length && NPCID.Sets.PositiveNPCTypesExcludedFromDeathTally[npcID])
                continue;
            int kills = BannerSystem.GetKillCount(bannerID);
            int need = ItemID.Sets.KillsToBanner[bannerItem];
            if (need == 0)
                need = 50;
            bool reached = kills >= need;
            Color color = reached ? C.White : masked ? C.Black : C.Faded;
            int col = n % 12, row = n / 12;
            float rx = page.X + P(40 + 25 * col), ry = page.Y + P(185 + 64 * row), rw = P(16), rh = P(48);
            ItemInSlot(bannerItem, rx - P(18), ry, P(52), P(48), color);
            if (Gesture.Inside(rx, ry, rw, rh))
            {
                string name = masked ? "???" : NPCName(npcID);
                Hover.Set(name + "\n" + kills + (reached ? "" : " / " + need));
            }
            n++;
            if (n >= 36)
                break;
        }
    }
}

public class RecordDisplaySlot : LogUIElement
{
    int slotID = -1;
    string title = "", value = "", tooltip = "";
    (int x, int y) ach = (-1, -1);

    static readonly Dictionary<(int, int), int> AchFallback = new()
    {
        [(0, 9)] = 321, [(0, 3)] = 1291, [(7, 10)] = 17, [(4, 10)] = 4600, [(4, 8)] = 4600, [(4, 9)] = 3099, [(2, 12)] = 3099, [(3, 0)] = 901, [(0, 7)] = 901,
    };

    RecordDisplaySlot(Texture2D texture) : base(texture) { }

    public static RecordDisplaySlot ForEntry(Texture2D texture, EntryInfo entry)
    {
        var slot = new RecordDisplaySlot(texture);
        if (entry.type == Data.MINIBOSS)
        {
            BossChecklistPlayer? me = State.Me;
            int kills = 0;
            me?.miniKills.TryGetValue(entry.Key, out kills);
            slot.title = L("Log.Records.TotalKills");
            slot.value = kills.ToString();
        }
        return slot;
    }

    public static RecordDisplaySlot ForCategory(Texture2D texture, int sub, int slotID)
    {
        var slot = new RecordDisplaySlot(texture) { slotID = slotID };
        slot.title = Titles(sub)[slotID];
        slot.value = Values(sub)[slotID];
        slot.tooltip = new[] { "", "Log.Records.Tooltip." + RecordCategory.Names[sub], "Log.Records.Tooltip.Duration", "Log.Records.Tooltip.HitsTaken" }[slotID];
        slot.ach = AchCoords(sub)[slotID];
        return slot;
    }

    static string[] Titles(int sub)
    {
        string world = sub == RecordCategory.WORLD ? "World" : "";
        return new[]
        {
            L("Log.Records.Category." + RecordCategory.Names[sub]),
            L("Log.Records.Title." + RecordCategory.Names[sub]),
            L("Log.Records.Title.Duration" + world),
            L("Log.Records.Title.HitsTaken" + world),
        };
    }

    static string[] Values(int sub)
    {
        PersonalRecords p = LogUI.GetPlayerRecords!;
        WorldRecord? w = LogUI.GetWorldRecords;
        string unique = p.attempts == 0 ? L("Log.Records.Unchallenged") : "#" + p.attempts;
        string duration = Records.TimeConversion(p.durationPrev), hits = Records.HitCount(p.hitsTakenPrev);
        if (sub == RecordCategory.BEST)
        {
            unique = p.KDR();
            duration = Records.TimeConversion(p.durationBest);
            hits = Records.HitCount(p.hitsTakenBest);
        }
        else if (sub == RecordCategory.FIRST)
        {
            unique = p.PlayTime();
            duration = Records.TimeConversion(p.durationFirst);
            hits = Records.HitCount(p.hitsTakenFirst);
        }
        else if (sub == RecordCategory.WORLD)
        {
            unique = w != null ? w.KDR() : L("Log.Records.Unchallenged");
            duration = Records.TimeConversion(w?.durationWorld ?? -1);
            hits = Records.HitCount(w?.hitsTakenWorld ?? -1);
        }
        string? name = sub == RecordCategory.WORLD ? Main.worldName : Util.Local.name;
        return new[] { name ?? "", unique, duration, hits };
    }

    static (int, int)[] AchCoords(int sub)
    {
        (int, int) unique = (0, 9);
        if (sub == RecordCategory.BEST)
        {
            unique = (0, 3);
        }
        else if (sub == RecordCategory.FIRST)
        {
            unique = (7, 10);
        }
        else if (sub == RecordCategory.WORLD)
        {
            WorldRecord? w = LogUI.GetWorldRecords;
            unique = w != null && w.totalKills >= w.totalDeaths ? (4, 10) : (4, 8);
        }
        return new[] { (-1, -1), unique, sub == RecordCategory.WORLD ? (2, 12) : (4, 9), sub == RecordCategory.WORLD ? (0, 7) : (3, 0) };
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        CalculatedStyle d = GetInnerDimensions();
        float K = View.K;
        if (ach.x >= 0 && ach.y >= 0)
        {
            Texture2D sheet = VanillaTexture("Images/UI/Achievements");
            if (!sheet.IsNull)
                D.Sprite(sheet, d.X, d.Y, P(64), P(64), C.White, new Rectangle(66 * ach.x, 66 * ach.y, 64, 64));
            else
                ItemInSlot(AchFallback.TryGetValue(ach, out int fallback) ? fallback : 321, d.X, d.Y, P(64), P(64));
            if (Gesture.Inside(d.X, d.Y, P(64), P(64)))
                Hover.Set(tooltip);
        }
        float max = d.Width - P(64 * 2) - P(15);
        if (title.Length > 0)
        {
            float s = FitScale(title, max, K);
            Vector2 m = D.Measure(title, s);
            D.Text(title, d.X + d.Width / 2 - m.X / 2 + P(2), d.Y + m.Y / 3, slotID == 0 ? C.Goldenrod : C.Gold, s);
        }
        if (value.Length > 0)
        {
            float s = FitScale(value, max, K);
            Vector2 m = D.Measure(value, s);
            D.Text(value, d.X + d.Width / 2 - m.X / 2 + P(2), d.Y + d.Height - m.Y, slotID == 0 ? C.LightYellow : C.White, s);
        }
    }
}

public class ContributorCredit : LogUIElement
{
    static readonly Dictionary<string, (float max0, float max1, float x0, float x1)> Layouts = new()
    {
        ["Dev"] = (224, 224, 80, 85),
        ["Mod"] = (208, 0, 95, 0),
        ["NoMods"] = (260, 0, 40, 0),
        ["Register"] = (280, 275, 45, 25),
    };

    Texture2D icon;
    string header = "", subheader = "";
    int[]? entryCounts;
    bool noIcon;

    ContributorCredit(Texture2D texture) : base(texture) { }

    public static ContributorCredit Dev(Texture2D texture, Texture2D character, string devName, string devTitle) => new(texture)
    {
        Id = "Dev",
        icon = character,
        header = devName,
        subheader = L("Log.Credits.Titles." + devTitle),
    };

    public static ContributorCredit Mod(Texture2D texture, RegisteredMod registered)
    {
        var c = new ContributorCredit(texture)
        {
            Id = "Mod",
            header = StripTags(registered.display),
            entryCounts = registered.counts,
            icon = ModIcon(registered),
        };
        if (c.icon.IsNull)
        {
            c.icon = Tex("Credits_NoIcon");
            c.noIcon = true;
        }
        return c;
    }

    public static ContributorCredit Text(Texture2D texture, string id, string titleKey, string descriptionKey = "")
    {
        var c = new ContributorCredit(texture) { Id = id, header = L("Log." + titleKey) };
        if (descriptionKey.Length > 0)
            c.subheader = L("Log." + descriptionKey);
        return c;
    }

    static Texture2D ModIcon(RegisteredMod registered)
    {
        if (!registered.iconSearched)
        {
            registered.iconSearched = true;
            if (registered.mod != null)
            {
                foreach (string name in new[] { "icon", "icon_workshop" })
                {
                    string path = registered.mod.Name + "/" + name;
                    if (ModContent.HasAsset(path))
                    {
                        registered.iconFile = path;
                        break;
                    }
                }
            }
        }
        return registered.iconFile != null ? Tracker.ModTexture(registered.iconFile) : default;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        CalculatedStyle d = GetInnerDimensions();
        float K = View.K;
        Layouts.TryGetValue(Id, out (float max0, float max1, float x0, float x1) layout);
        float modOffset = subheader.Length > 0 ? 0 : P(8);
        if (!icon.IsNull)
        {
            var r = new CalculatedStyle(d.X + modOffset, d.Y + modOffset, P(80), P(80));
            LogPages.Sprite(icon, r);
            if (noIcon && Gesture.Inside(r.X, r.Y, r.Width, r.Height))
                Hover.Set("Log.Credits.NoIcon");
        }
        D.Text(header, d.X + P(layout.x0), d.Y + P(11), C.White, FitScale(header, P(layout.max0), K));
        if (subheader.Length > 0)
        {
            D.Text(subheader, d.X + P(layout.x1), d.Y + P(45), C.LemonChiffon, FitScale(subheader, P(layout.max1), K));
        }
        else if (entryCounts != null)
        {
            float x = 94 + (70 * 2) / 3f;
            foreach (int n in entryCounts)
            {
                string t = n.ToString();
                D.Text(t, d.X + P(x) - D.Measure(t, K).X / 2, d.Y + P(54), C.LemonChiffon, K);
                x += 74;
            }
        }
    }
}

public class LogTab : LogUIElement
{
    readonly Texture2D texture, icon;
    int? anchor;

    public LogTab(Texture2D texture, Texture2D icon)
    {
        this.texture = texture;
        this.icon = icon;
        Width.Set(P(!texture.IsNull ? texture.Width : 32), 0);
        Height.Set(P(!texture.IsNull ? texture.Height : 76), 0);
    }

    public int? Anchor
    {
        get => anchor;
        set
        {
            anchor = value;
            if (Id == "TableOfContents")
            {
                hoverText = "Log.Tabs." + (value != null ? "TableOfContents" : "ToggleFilters");
            }
            else if (Id != "Credits" && value != null && value >= 0 && value < Tracker.sorted.Count)
            {
                EntryInfo entry = Tracker.sorted[value.Value];
                string type = L("Log.Common." + (Cfg.Log.ProgressiveChecklist ? "Entry" : Tracker.TypeName(entry.type)));
                hoverText = "Log.Tabs.NextEntry";
                hoverTextParams = new object?[] { type, entry.DisplayName };
            }
        }
    }

    public bool Visible()
    {
        int page = LogUI.PageNum;
        if (page == LogPages.PAGE_PROMPT)
            return false;
        if ((Cfg.Log.ProgressiveChecklist || Cfg.Log.OnlyShowBossContent) && (Id == "MiniBoss" || Id == "Event"))
            return false;
        if (Id == "TableOfContents")
            return true;
        if (Id == "Credits")
            return page != LogPages.PAGE_CREDITS;
        return anchor != null && anchor >= 0 && page != anchor;
    }

    public bool OnLeftSide()
    {
        int page = LogUI.PageNum;
        if (Id == "TableOfContents")
            return true;
        if (Id == "Credits")
            return false;
        return (anchor != null && page > anchor) || page == LogPages.PAGE_CREDITS;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (Gesture.IsTap(evt) && anchor != null)
        {
            Sound(SoundID.MenuTick);
            LogUI.PendingPageNum = anchor.Value;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (!Visible())
            return;
        base.Draw(spriteBatch);
        CalculatedStyle d = GetInnerDimensions();
        bool left = OnLeftSide();
        LogPages.Sprite(texture, d, C.Tan, null, !left);
        Texture2D shown = Id == "TableOfContents" && LogUI.PageNum == LogPages.PAGE_TOC ? Tex("Nav_Filter") : icon;
        if (!shown.IsNull)
        {
            float dx = d.X < Main.screenWidth / 2f ? P(2) : -P(2);
            LogPages.At(shown, d.X + d.Width / 2 - P(shown.Width) / 2 + dx, d.Y + d.Height / 2 - P(shown.Height) / 2);
        }
    }
}

public class TableOfContents : LogUIElement, ILongPress
{
    readonly EntryInfo entry;
    readonly string displayName;
    readonly float textScale;
    readonly bool markAsNext, allLoot, allCollectibles;
    string text;
    Color TextColor, defaultColor;

    public TableOfContents(int index, string displayName, Color entryColor, bool loot, bool collect, float textScale = 1)
    {
        BossLogUI log = LogUI;
        entry = Tracker.sorted[index];
        this.displayName = displayName;
        text = displayName;
        this.textScale = textScale;
        markAsNext = Cfg.Log.DrawNextMark && entry.IsUpNext && !log.IsHidden(entry) && !log.HiddenEntriesMode;
        allLoot = loot;
        allCollectibles = collect;
        TextColor = defaultColor = markAsNext && Cfg.Log.ColoredBossText ? C.Next : entryColor;
        Width.Set(0, 1);
    }

    float Scale => textScale * View.K;

    public override void Recalculate()
    {
        MinWidth.Set(D.Measure(text, Scale).X + PaddingLeft + PaddingRight, 0);
        MinHeight.Set(16 * Scale + PaddingTop + PaddingBottom, 0);
        base.Recalculate();
    }

    void ToggleHidden()
    {
        BossLogUI log = LogUI;
        bool hidden = !log.IsHidden(entry);
        log.HiddenEntriesPending[entry.Key] = hidden;
        Sound(SoundID.Unlock);
        TextColor = defaultColor = hidden ? C.DimGray : C.DarkGray;
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Gesture.IsTap(evt))
            return;
        BossLogUI log = LogUI;
        if (log.HiddenEntriesMode)
        {
            ToggleHidden();
            return;
        }
        if (Cfg.Log.ProgressiveChecklist && !entry.IsAutoDownedOrMarked && !entry.IsUpNext)
            return;
        Sound(SoundID.MenuTick);
        log.PendingPageNum = entry.GetIndex;
    }

    public bool LongPress()
    {
        BossLogUI log = LogUI;
        if (log.HiddenEntriesMode)
        {
            ToggleHidden();
            return true;
        }
        WorldData.SetMarked(entry.Key, !entry.MarkedAsDowned);
        Sound(SoundID.Unlock);
        log.RefreshPageContent();
        return true;
    }

    public override void MouseOver(UIMouseEvent evt)
    {
        LogUI.headNum = entry.GetIndex;
        if (Cfg.Log.ShowProgressionValue)
            text = "[" + entry.progression.ToString(CultureInfo.InvariantCulture) + "f] " + displayName;
        TextColor = Cfg.Log.ColoredBossText ? C.SkyBlue : C.Silver;
        base.MouseOver(evt);
    }

    public override void MouseOut(UIMouseEvent evt)
    {
        if (LogUI.headNum == entry.GetIndex)
            LogUI.headNum = -1;
        text = displayName;
        TextColor = defaultColor;
        base.MouseOut(evt);
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        BossLogUI log = LogUI;
        CalculatedStyle d = GetInnerDimensions();
        BossChecklistPlayer? me = State.Me;
        int ri = entry.RecordIndex;
        if (Cfg.Feature.NewRecordLogGlow && me != null && ri >= 0 && ri < me.hasNewRecord.Length && me.hasNewRecord[ri])
            TextColor = Disco();
        base.Draw(spriteBatch);
        float s = Scale;
        D.Text(text, d.X, d.Y - 2 * s, TextColor, s);

        CalculatedStyle parent = Parent!.GetInnerDimensions();
        float hmOffset = entry.progression > LogPages.WALL_OF_FLESH ? P(10) : 0;
        if (!log.HiddenEntriesMode && allLoot)
        {
            bool gold = !Cfg.Log.OnlyCheckDroppedLoot && allCollectibles;
            Texture2D chest = Tex(gold ? "Checks_Chest_Gold" : "Checks_Chest");
            if (!chest.IsNull)
            {
                var r = new CalculatedStyle(parent.X + parent.Width - P(chest.Width) - hmOffset, d.Y - P(2), P(chest.Width), P(chest.Height));
                LogPages.Sprite(chest, r);
                if (Gesture.Inside(r.X, r.Y, r.Width, r.Height))
                {
                    string looted = L("Log.TableOfContents.AllLoot");
                    Hover.Set(gold ? looted + "\n" + L("Log.TableOfContents.AllCollectibles") : looted);
                }
            }
        }

        float x = d.X - P(20);
        float y = d.Y - P(5);
        CheckmarkType checkType = Cfg.Log.SelectedCheckmarkType;
        bool hidden = log.IsHidden(entry);
        Texture2D grid = Tex("Checks_Box");
        if (log.HiddenEntriesMode)
        {
            grid = hidden ? TextureAssets.InventoryTickOff.Value : TextureAssets.InventoryTickOn.Value;
            y += P(7);
        }
        else if (entry.IsAutoDownedOrMarked)
        {
            if (checkType == CheckmarkType.X_Empty)
                grid = Tex("Checks_X");
            else if (checkType != CheckmarkType.StrikeThrough)
                grid = Tex("Checks_Check");
            else
                DrawStrike(d, s);
        }
        else
        {
            grid = Tex(checkType == CheckmarkType.Check_X ? "Checks_X" : "Checks_Box");
            if (markAsNext)
                grid = Tex("Checks_Next");
        }
        if (log.HiddenEntriesMode)
        {
            LogPages.At(grid, x, y);
        }
        else if (!hidden && checkType != CheckmarkType.StrikeThrough)
        {
            LogPages.At(Tex("Checks_Box"), x, y);
            LogPages.At(grid, x, y);
        }
        else if (!hidden && checkType == CheckmarkType.StrikeThrough && markAsNext && !entry.IsAutoDownedOrMarked)
        {
            LogPages.At(Tex("Checks_Next"), x, y);
        }
    }

    void DrawStrike(CalculatedStyle d, float s)
    {
        Texture2D strike = Tex("Checks_Strike");
        if (strike.IsNull)
            return;
        int w = strike.Width / 3, h = strike.Height;
        Vector2 size = D.Measure(displayName, s);
        bool hover = Gesture.On(this);
        Color cap = hover ? C.Faded : C.White;
        float y = d.Y + size.Y / 3 - P(h) / 2;
        D.Sprite(strike, d.X - P(w), y, P(w), P(h), cap, new Rectangle(0, 0, w, h));
        if (!hover)
            D.Sprite(strike, d.X, y, size.X, P(h), C.White, new Rectangle(w, 0, w, h));
        D.Sprite(strike, d.X + size.X, y, P(w), P(h), cap, new Rectangle(w * 2, 0, w, h));
    }

    public override int CompareTo(object? obj) => obj is TableOfContents other ? entry.progression.CompareTo(other.entry.progression) : 0;
}

public class ProgressBar : LogUIElement
{
    struct Counts
    {
        public int downed, total;
        public float pct;
    }

    struct Section
    {
        public float X, Y, Width, Height;
        public string text;
    }

    bool initializeDividers = true;
    bool barState;
    readonly float percentageTotal;
    readonly Counts countsTotal;
    readonly List<(string name, Counts counts)> countsByType = new();
    readonly List<(string name, Counts counts)> countsByMod = new();
    readonly List<Section> sections = new();

    public ProgressBar(bool hardMode)
    {
        barState = LogUI.ProgressBarState;
        List<EntryInfo> all = Tracker.sorted;
        countsTotal = Calculate(all, hardMode);
        percentageTotal = countsTotal.pct;
        foreach (int type in new[] { Data.BOSS, Data.MINIBOSS, Data.EVENT })
            countsByType.Add((L("Log.Common." + Tracker.TypeName(type) + "Plural"), Calculate(all.Where(e => e.type == type), hardMode)));
        Counts terraria = Calculate(all.Where(e => e.modSource == "Terraria"), hardMode);
        if (terraria.total != 0)
            countsByMod.Add(("Terraria", terraria));
        foreach ((string name, RegisteredMod registered) in Tracker.registeredMods)
        {
            Counts c = Calculate(all.Where(e => e.modSource == name), hardMode);
            if (c.total != 0)
                countsByMod.Add((StripTags(registered.display), c));
        }
        Counts unknown = Calculate(all.Where(e => e.modSource == "Unknown"), hardMode);
        if (unknown.total != 0)
            countsByMod.Add(("Unknown", unknown));
    }

    static Counts Calculate(IEnumerable<EntryInfo> entries, bool hardMode)
    {
        int total = 0, downed = 0;
        foreach (EntryInfo e in entries)
        {
            if (!e.VisibleOnChecklist() || (hardMode && e.progression <= LogPages.WALL_OF_FLESH) || (!hardMode && e.progression > LogPages.WALL_OF_FLESH))
                continue;
            total++;
            if (e.IsAutoDownedOrMarked)
                downed++;
        }
        return new Counts { downed = downed, total = total, pct = total == 0 ? 1 : downed / (float)total };
    }

    void GenerateDividers()
    {
        sections.Clear();
        CalculatedStyle d = GetInnerDimensions();
        float barFull = d.Width - P(8);
        float remainder = barFull * percentageTotal;
        float meterX = d.X + P(4);
        List<(string name, Counts counts)> counts = barState ? countsByMod : countsByType;
        int finalIndex = -1;
        for (int i = 0; i < counts.Count; i++)
        {
            if (counts[i].counts.downed != 0)
                finalIndex = i;
        }
        int all = countsTotal.total != 0 ? countsTotal.total : 1;
        for (int i = 0; i < counts.Count; i++)
        {
            (string name, Counts c) = counts[i];
            if (c.downed == 0)
                continue;
            float length = barFull * (c.downed / (float)all);
            string text = name + ": " + c.downed + "/" + c.total + " (" + (c.downed / (double)all * 100).ToString("F1", CultureInfo.InvariantCulture) + "%)";
            if (i == finalIndex)
                length = remainder;
            sections.Add(new Section { X = meterX, Y = d.Y, Width = Math.Max(0, length - 1), Height = d.Height, text = text });
            meterX += length;
            remainder -= length;
        }
    }

    public override void LeftClick(UIMouseEvent evt)
    {
        base.LeftClick(evt);
        if (!Gesture.IsTap(evt))
            return;
        LogUI.ProgressBarState = !LogUI.ProgressBarState;
        Sound(SoundID.MenuTick);
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (initializeDividers || barState != LogUI.ProgressBarState)
        {
            barState = LogUI.ProgressBarState;
            GenerateDividers();
            initializeDividers = false;
        }
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        if (LogUI.HiddenEntriesMode)
            return;
        base.Draw(spriteBatch);
        CalculatedStyle d = GetInnerDimensions();
        float K = View.K;
        string percent = (percentageTotal * 100).ToString("F1", CultureInfo.InvariantCulture) + "%";
        float ts = 0.85f * K;
        Vector2 m = D.Measure(percent, ts);
        D.Text(percent, d.X + d.Width / 2 - m.X / 2, d.Y - m.Y, C.Amber, ts);

        Texture2D bar = Tex("Extra_ProgressBar");
        if (!bar.IsNull)
        {
            int wCut = bar.Width / 3, h = bar.Height;
            float k = d.Height / h;
            D.Sprite(bar, d.X, d.Y, wCut * k, d.Height, C.White, new Rectangle(0, 0, wCut, h));
            D.Sprite(bar, d.X + wCut * k, d.Y, d.Width - wCut * k * 2, d.Height, C.White, new Rectangle(wCut, 0, wCut, h));
            D.Sprite(bar, d.X + d.Width - wCut * k, d.Y, wCut * k, d.Height, C.White, new Rectangle(2 * wCut, 0, wCut, h));
        }
        float barWidth = d.Width - P(8);
        float mx = d.X + P(4), my = d.Y + P(4), mw = barWidth * percentageTotal, mh = d.Height - P(8);
        D.Fill(mx, my, mw, mh, C.White);
        D.Fill(mx, my, mw, mh, LogColor(180));
        Color color = LogColor();
        for (int i = 0; i < sections.Count; i++)
        {
            Section s = sections[i];
            if (Gesture.Inside(s.X, s.Y, s.Width, s.Height))
            {
                Hover.Set(s.text);
                D.Fill(s.X, s.Y + P(4), s.Width, s.Height - P(8), color);
            }
            if (i != sections.Count - 1)
                D.Fill(s.X + s.Width - 1, s.Y + P(4), 2, s.Height - P(8), color);
        }
    }
}

public class FittedTextPanel : LogUIElement
{
    readonly string key;
    readonly float textScale;
    readonly bool fit;
    RichLayout? layout;
    float layoutScale;
    string layoutKey = "";

    public FittedTextPanel(string localizationKey, float textScale = 1, bool fit = false)
    {
        key = localizationKey;
        this.textScale = textScale;
        this.fit = fit;
    }

    RichLayout Layout(CalculatedStyle d)
    {
        string text = L("Log." + key);
        float w = d.Width - P(7 * 2);
        string current = text + "|" + w + "|" + d.Height;
        if (layout != null && layoutKey == current)
            return layout;
        float scale = textScale * View.K;
        RichLayout built = Rich.Layout(text, w, scale);
        while (fit && built.height + P(18) > d.Height && scale > 0.5f * View.K)
        {
            scale *= 0.95f;
            built = Rich.Layout(text, w, scale);
        }
        layout = built;
        layoutScale = scale;
        layoutKey = current;
        return built;
    }

    public override void Draw(SpriteBatch spriteBatch)
    {
        base.Draw(spriteBatch);
        CalculatedStyle d = GetInnerDimensions();
        RichLayout current = Layout(d);
        Rich.Draw(current, d.X + P(2), d.Y + P(15 + 3), layoutScale, true);
    }
}
