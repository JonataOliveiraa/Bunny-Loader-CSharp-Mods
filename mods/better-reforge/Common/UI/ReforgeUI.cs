using System;
using System.Collections.Generic;
using System.Linq;
using BetterReforgeMod.Common.Utilities;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace BetterReforgeMod.Common.UI;

public class PrefixButton : ModButton
{
    readonly ReforgeUI parent;
    public readonly int Index;

    public PrefixButton(ReforgeUI parent, int index)
    {
        this.parent = parent;
        Index = index;
    }

    Color GetColor(bool isSelectedPrefix) =>
        isSelectedPrefix ? parent.SelectedButtonColor : Hovered ? parent.PanelColorHighlight : parent.PanelColor;

    public void Draw(Rectangle rect, bool isSelectedPrefix) => DrawUtils.DrawInvBG(rect, GetColor(isSelectedPrefix));

    public override void OnClick() => parent.SelectPrefix(parent.AvailablePrefixes![Index]);
}

public class ReforgeButton : ModButton
{
    static Texture2D idle, pressed;

    readonly ReforgeUI parent;

    public ReforgeButton(ReforgeUI parent)
    {
        this.parent = parent;
    }

    static Texture2D Load(string name) => ModContent.Request<Texture2D>("better-reforge/Assets/Textures/" + name).Value;

    public override Color GetColor() => Clicked ? Color.Gray : Color.White;

    public override Texture2D GetTexture()
    {
        if (idle.IsNull)
        {
            idle = Load("Reforge_0");
            pressed = Load("Reforge_1");
        }
        return Hovered && Main.mouseLeft ? pressed : idle;
    }

    public override void OnClick() => parent.ReforgeItem();
}

public class ReforgeUI : ModInterface
{
    const int ReforgeSlotContext = 5;
    const int GoblinTinkerer = 107;

    readonly struct PendingText
    {
        public readonly string Value;
        public readonly float X, Y, Scale;
        public readonly Color Color;

        public PendingText(string text, float x, float y, Color color, float scale = 1f)
        {
            Value = text;
            X = x;
            Y = y;
            Color = color;
            Scale = scale;
        }
    }

    public static ReforgeUI Instance = null!;

    // Both arrays hold their item across frames, so a handle keeps each one alive.
    public static GameArray<Item> Slot;
    static GameArray<Item> preview;
    static GameHandle? slotHandle, previewHandle;

    public int PanelWidth, PanelHeight;
    public Rectangle Panel, PrefixPanel, ScrollRect, ScrollThumb;
    public Color PanelColor, PanelColorHighlight, PrefixPanelColor, SelectedButtonColor, ScrollThumbColor, DividerColor;
    public int[]? AvailablePrefixes;
    public int OriginalPrefix, SelectedPrefix, ReforgeCost, ViewPosition;
    public PrefixButton[] PrefixButtons = Array.Empty<PrefixButton>();
    public ReforgeButton ReforgeActionButton = null!;

    Panel_Layout layout;
    int visibleCount;
    bool pressedSlot, wasMouseLeft, scrollPressed;
    int scrollGrabY, oldItemType, oldItemPrefix;
    Rectangle tempRectangle, tempSlotRectangle;
    Vector2 tempPosition, tempSlotPosition;
    readonly List<PendingText> pendingTexts = new();

    public ReforgeUI()
    {
        Instance = this;
    }

    static Item NewAirItem()
    {
        Item item = Item.New();
        item.SetDefaults(0, default);
        return item;
    }

    public static string GetPrefixName(int prefix)
    {
        string text = prefix >= 0 && prefix < Lang.prefix.Length ? Lang.prefix[prefix].Value ?? "" : $"Prefix {prefix}";
        return text.StartsWith('(') && text.EndsWith(')') ? text.Substring(1, text.Length - 2) : text;
    }

    // Tries every prefix on a copy of the item and sorts them by the value they give it.
    static int[]? GetPrefixesForItem(Item item)
    {
        GameArray<int> rollablePrefixes = item.GetRollablePrefixes();
        if (rollablePrefixes.IsNull || rollablePrefixes.Length == 0)
            return null;

        var prefixes = new List<(int Id, int Value)>();
        for (int i = 0; i < rollablePrefixes.Length; i++)
        {
            int pre = rollablePrefixes[i];
            item.ResetPrefix();
            if (item.Prefix(pre) && item.prefix == pre)
                prefixes.Add((pre, item.value));
        }
        return prefixes.OrderByDescending(p => p.Value).Select(p => p.Id).ToArray();
    }

    void InitializeReforgeItem()
    {
        Item item = Slot[0];

        oldItemType = item.type;
        oldItemPrefix = item.prefix;

        AvailablePrefixes = GetPrefixesForItem(item.Clone());
        preview[0] = item.Clone();
        OriginalPrefix = item.prefix;
        SelectedPrefix = item.prefix;
        ReforgeCost = 0;
        int numPrefixes = AvailablePrefixes?.Length ?? 0;
        PrefixButtons = new PrefixButton[numPrefixes];
        for (int i = 0; i < numPrefixes; i++)
            PrefixButtons[i] = new PrefixButton(this, i);
        ViewPosition = 0;
    }

    public void ReforgeItem()
    {
        Player player = Main.player[Main.myPlayer];
        long price = ReforgeCost;
        if (player.CanAffordItem(price, -1))
        {
            player.BuyItem(price, -1);
            Slot[0].ResetPrefix();
            Slot[0].Prefix(SelectedPrefix);
            SoundEngine.PlaySound(SoundID.Item37, -1, -1, 0f, 1f);
        }
    }

    public void SelectPrefix(int prefix)
    {
        Item item = preview[0];
        item.ResetPrefix();
        item.Prefix(prefix);
        SelectedPrefix = prefix;
        ReforgeCost = OriginalPrefix == prefix ? 0 : GUIInstance.Active.GUIReforge.ItemReforgeCost(item);
    }

    void ResetState()
    {
        pressedSlot = false;
        wasMouseLeft = false;
        tempPosition = default;
        tempRectangle = default;
        tempSlotPosition = default;
        tempSlotRectangle = new Rectangle(0, 0, 52, 52);

        oldItemType = -1;
        oldItemPrefix = -1;

        visibleCount = 5;
        ViewPosition = 0;
        ScrollRect = default;
        ScrollThumb = default;
        scrollPressed = false;
        scrollGrabY = 0;

        pendingTexts.Clear();
    }

    public override void SetupContent()
    {
        Slot = GameArray<Item>.New(Item.Class, 1);
        slotHandle = new GameHandle(Slot.Pointer);
        Slot[0] = NewAirItem();
        preview = GameArray<Item>.New(Item.Class, 1);
        previewHandle = new GameHandle(preview.Pointer);

        PanelWidth = 296;
        PanelHeight = 256;
        Panel = new Rectangle(0, 0, PanelWidth, PanelHeight);
        PanelColor = new Color(44, 46, 107, 180);
        PanelColorHighlight = new Color(69, 71, 132, 180);
        layout = CreateLayout(PanelWidth, PanelHeight);

        PrefixPanel = default;
        PrefixPanelColor = new Color(77, 110, 110, 180);
        SelectedButtonColor = new Color(180, 162, 49, 180);
        ScrollThumbColor = new Color(150, 155, 220, 180);
        DividerColor = new Color(13, 13, 13, 180);

        ReforgeActionButton = new ReforgeButton(this);

        ResetState();
    }

    public override void Clear()
    {
        if (!Slot[0].IsAir)
            Main.player[Main.myPlayer].GetItem(Slot[0].DeepClone(), GetItemSettings.ReturnItemFromSlot, false);

        Slot[0] = NewAirItem();
        InitializeReforgeItem();

        ResetState();
    }

    static bool CheckCanDraw()
    {
        Player player = Main.player[Main.myPlayer];
        if (player.talkNPC < 0 || player.talkNPC >= 200)
            return false;
        NPC npc = Main.npc[player.talkNPC];
        return npc.active && npc.type == GoblinTinkerer;
    }

    static string WrapString(string text, SpriteFont font, int maxWidth, int maxLines)
    {
        GameArray<GameString> wrapped = Utils.WordwrapString(text, font, maxWidth, maxLines, out _);
        var lines = new string[wrapped.Length];
        for (int i = 0; i < lines.Length; i++)
            lines[i] = wrapped[i].IsNull ? "" : wrapped[i].ToString() ?? "";
        return string.Join("\n", lines);
    }

    public override void Draw()
    {
        if (!CheckCanDraw())
        {
            GUIInstance.Active.GUIPageIcons.Close(true);
            return;
        }

        Main.inventoryScale = 0.75f;

        DrawPanelBG();
        DrawReforgeSlot();
        DrawDivider();

        if (Slot[0].type != oldItemType || Slot[0].prefix != oldItemPrefix)
            InitializeReforgeItem();

        SpriteFont font = FontAssets.MouseText.Value;
        if (Slot[0].IsAir)
        {
            string text = WrapString(BetterReforgeText.PlaceItem, font, PanelWidth - 58, 2);
            Vector2 size = font.MeasureString(text);
            pendingTexts.Add(new PendingText(text, Panel.X + 54, Panel.Y + 30 - (size.Y * 0.825f) * 0.5f, Color.Gray, 0.825f));
        }
        else
        {
            DrawPrefixButtons();
            if (OriginalPrefix != SelectedPrefix)
                DrawReforgeButton();
            DrawCosts(font);
        }

        DrawPendingTexts(font);
    }

    void DrawDivider()
    {
        tempRectangle.X = Panel.X;
        tempRectangle.Y = Panel.Y + 54;
        tempRectangle.Width = PanelWidth;
        tempRectangle.Height = 2;
        DrawUtils.Rect(TextureAssets.MagicPixel.Value, tempRectangle, DividerColor);
    }

    void DrawPanelBG()
    {
        bool isRightPage = ReforgeLayout.RightPageController;
        Panel.X = isRightPage ? Main.screenWidth - PanelWidth - 42 : 42;
        Panel.Y = isRightPage ? 1 : PanelHeight + 22;
        Recalculate(layout, Panel);
        DrawUtils.DrawInvBG(Panel, PanelColor);

        PrefixPanel.X = Panel.X + 8;
        PrefixPanel.Y = Panel.Y + 62;
        PrefixPanel.Width = PanelWidth - 16;
        PrefixPanel.Height = PanelHeight - 70;
        DrawUtils.DrawInvBG(PrefixPanel, PrefixPanelColor);
    }

    static Item HandleSlot(Item item)
    {
        ItemSlot.singleSlotArray[0] = item;
        ItemSlot.Handle(ItemSlot.singleSlotArray, 0, 0);
        return ItemSlot.singleSlotArray[0];
    }

    void DrawReforgeSlot()
    {
        tempSlotRectangle.X = Panel.X + 8;
        tempSlotRectangle.Y = Panel.Y + 8;

        bool justPressed = Main.mouseLeft && !wasMouseLeft;
        bool justReleased = !Main.mouseLeft && wasMouseLeft;
        bool hovered = tempSlotRectangle.Contains(Main.mouseX, Main.mouseY);
        if (justPressed && hovered)
            pressedSlot = true;
        // A drag that leaves the slot takes the item out.
        if (pressedSlot && Main.mouseLeft && !hovered && Main.mouseItem.IsAir && !Slot[0].IsAir)
        {
            pressedSlot = false;
            Slot[0] = HandleSlot(Slot[0]);
        }
        // Releasing a reforgeable item over the slot puts one of it in.
        if (hovered && !Main.mouseItem.IsAir && justReleased)
        {
            if (Main.mouseItem.CanHavePrefixes())
            {
                Slot[0] = HandleSlot(Slot[0]);
                if (Slot[0].stack > 1)
                {
                    Slot[0].stack -= 1;
                    Main.mouseItem = Slot[0].Clone();
                    Slot[0].stack = 1;
                }
            }
        }
        if (justReleased)
            pressedSlot = false;

        if (hovered && Main.mouseLeft && !Slot[0].IsAir)
        {
            Item item = preview[0];
            ItemSlot.MouseHover(ref item, 0);
        }

        if (hovered)
        {
            ItemSlot.CursorHighlight = true;
            if (!Main.mouseItem.IsAir)
                ItemSlot.SelectHighlight = true;
        }
        tempSlotPosition.X = tempSlotRectangle.X;
        tempSlotPosition.Y = tempSlotRectangle.Y;
        ItemSlot.Draw(Main.spriteBatch, Slot, ReforgeSlotContext, 0, tempSlotPosition, Color.White);
        DrawUtils.NewSpriteBatchForUI();

        wasMouseLeft = Main.mouseLeft;
    }

    void DrawPrefixButtons()
    {
        const int padding = 4, spacing = 2, scrollWidth = 32;
        int visible = visibleCount;
        int maxIndex = Math.Max(0, PrefixButtons.Length - visible);

        int x = PrefixPanel.X + padding;
        int y = PrefixPanel.Y + padding;
        int width = PrefixPanel.Width - scrollWidth - padding * 2;
        int height = (PrefixPanel.Height - padding * 2 - spacing * (visible - 1)) / visible;
        ScrollRect.X = PrefixPanel.Right - scrollWidth;
        ScrollRect.Y = PrefixPanel.Y + padding;
        ScrollRect.Width = scrollWidth - padding;
        ScrollRect.Height = PrefixPanel.Height - padding * 2;
        DrawUtils.DrawInvBG(ScrollRect, PanelColor);

        int thumbHeight = Math.Max(24, (int)Math.Floor(ScrollRect.Height * ((double)visible / Math.Max(PrefixButtons.Length, visible))));
        double t = maxIndex == 0 ? 0 : (double)ViewPosition / maxIndex;
        ScrollThumb.Width = ScrollRect.Width - 4;
        ScrollThumb.Height = thumbHeight;
        ScrollThumb.X = (int)((ScrollRect.X + ScrollRect.Width * 0.5) - ScrollThumb.Width * 0.5);
        ScrollThumb.Y = (int)(ScrollRect.Y + (ScrollRect.Height - thumbHeight) * t);

        bool hoveredThumb = ScrollThumb.Contains(Main.mouseX, Main.mouseY);
        if (hoveredThumb && Main.mouseLeft && !scrollPressed)
        {
            scrollPressed = true;
            scrollGrabY = Main.mouseY - ScrollThumb.Y;
        }
        if (!Main.mouseLeft)
            scrollPressed = false;

        if (scrollPressed && maxIndex > 0)
        {
            Main.blockMouse = true;
            int minY = ScrollRect.Y;
            int maxY = ScrollRect.Bottom - thumbHeight;
            ScrollThumb.Y = Math.Max(minY, Math.Min(Main.mouseY - scrollGrabY, maxY));
            double nt = (double)(ScrollThumb.Y - minY) / (maxY - minY);
            // Math.round in JavaScript: halves go up.
            ViewPosition = (int)Math.Floor(nt * maxIndex + 0.5);
        }
        DrawUtils.DrawInvBG(ScrollThumb, ScrollThumbColor);

        int startIndex = ViewPosition;
        for (int i = 0; i < visible; i++)
        {
            int index = startIndex + i;
            if (index >= PrefixButtons.Length)
                break;
            PrefixButton button = PrefixButtons[index];
            button.Area = new Rectangle(x, y + i * (height + spacing), width, height);
            ModButton.UpdateButton(button, button.Area);

            int prefix = AvailablePrefixes![button.Index];
            pendingTexts.Add(new PendingText(GetPrefixName(prefix), button.Area.X + 4, button.Area.Y + 8, OriginalPrefix == prefix ? Color.LightGreen : Color.White, 0.825f));

            button.Draw(button.Area, SelectedPrefix == prefix);
        }
    }

    void DrawReforgeButton()
    {
        ReforgeButton button = ReforgeActionButton;
        Texture2D texture = button.GetTexture();
        button.Area = new Rectangle(
            tempSlotRectangle.X + tempSlotRectangle.Width + 2,
            (int)((tempSlotRectangle.Y + tempSlotRectangle.Height * 0.5) - 6 - texture.Height * 0.5),
            texture.Width,
            texture.Height);
        ModButton.UpdateButton(button, button.Area);
        if (button.Hovered)
            pendingTexts.Add(new PendingText(Language.GetText("LegacyInterface.19").Value ?? "", Main.mouseX, Panel.Y - 8, Color.White));
        button.Draw(button.Area);
    }

    void DrawCosts(SpriteFont font)
    {
        int startX = Panel.X - 70;
        int startY = Panel.Y - 36;
        int centerX = startX + PanelWidth - 48;
        int centerY = startY + 16;

        ItemSlot.DrawCost(Main.spriteBatch, centerX, centerY, ReforgeCost, Main.spriteBatch2);

        string text = WrapString(Language.GetText("Mobile.ReforgePrice").Value ?? "", font, 164, 3);
        Vector2 size = font.MeasureString(text);
        pendingTexts.Add(new PendingText(text, centerX + 32 - size.X * 0.8f, Panel.Y + 2 + (size.Y * 0.8f) * 0.5f, Color.White, 0.8f));
    }

    void DrawPendingTexts(SpriteFont font)
    {
        foreach (PendingText text in pendingTexts)
        {
            tempPosition.X = text.X;
            tempPosition.Y = text.Y;
            DrawUtils.BorderString(font, text.Value, tempPosition, text.Color, text.Scale);
        }
        pendingTexts.Clear();
    }
}

static class BetterReforgeText
{
    public static string PlaceItem => ModContent.GetInstance<BetterReforge>().GetLocalization("ReforgeUI.PlaceItem").Value ?? "";
}
