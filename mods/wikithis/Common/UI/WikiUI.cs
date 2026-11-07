using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace WikithisMod.Common.UI;

public class WikiUI : ModInterface
{
    const int WikiSlotContext = 6;

    public static WikiUI Instance = null!;

    // Holds its item across frames, so a handle keeps the array alive.
    public static GameArray<Item> Slot;
    static GameHandle? slotHandle;

    public static Dictionary<int, string> ItemUrl = new();

    public int PanelWidth, PanelHeight;
    public Rectangle Panel;
    public long LastClickTime;
    public int DoubleClickDelay;

    Panel_Layout layout;
    bool pressed, wasMouseLeft;
    Vector2 tempSlotPosition;

    static Asset<Texture2D>? oldBgTexture, oldBgHighlightTexture, oldTrashIcon;

    public WikiUI()
    {
        Instance = this;
    }

    public static bool CheckUrl(int type) => ItemUrl.TryGetValue(type, out string? url) && url != "";

    public static void OpenWikiPage(int type)
    {
        if (CheckUrl(type))
            GameBindings.OpenUrl(ItemUrl[type]);
    }

    public static GameCulture.CultureName CurrentCulture => Language.ActiveCulture.Name switch
    {
        "de-DE" => GameCulture.CultureName.German,
        "es-ES" => GameCulture.CultureName.Spanish,
        "fr-FR" => GameCulture.CultureName.French,
        "it-IT" => GameCulture.CultureName.Italian,
        "pl-PL" => GameCulture.CultureName.Polish,
        "pt-BR" => GameCulture.CultureName.Portuguese,
        "ru-RU" => GameCulture.CultureName.Russian,
        "zh-Hans" => GameCulture.CultureName.Chinese,
        _ => GameCulture.CultureName.English,
    };

    // String.replace in JavaScript changes only the first match, and the original relies on that.
    static string ReplaceFirst(string text, string from, string to)
    {
        int index = text.IndexOf(from, StringComparison.Ordinal);
        return index < 0 ? text : text.Substring(0, index) + to + text.Substring(index + from.Length);
    }

    public static string DefaultSearchStr(string name, string? mod = null)
    {
        name = ReplaceFirst(ReplaceFirst(name, " ", "_"), "'", "%27");
        if (mod == null)
        {
            const string terrariaWiki = "https://terraria.wiki.gg/";

            string url = $"{terrariaWiki}wiki/{name}";
            if (CurrentCulture == GameCulture.CultureName.Italian)
                url += "/it";
            else if (CurrentCulture != GameCulture.CultureName.English)
                url = terrariaWiki + LanguageManager.Instance.ActiveCulture.Name!.Substring(0, 2) + "/" + url.Substring(terrariaWiki.Length);
            return url;
        }
        return mod + name;
    }

    public static string GetReplacementsForItem(int type, string name)
    {
        if (type == ItemID.BrainScrambler)
            name += "_(item)";
        else if (type == ItemID.PrincessDress)
            name += "_(Clothier)";
        else if (type == ItemID.DontHurtCrittersBook)
            name = "Guide_to_Critter_Companionship";
        else if (type == ItemID.DontHurtCrittersBookInactive)
            name = "Guide_to_Critter_Companionship_(Inactive)";
        else if (type == ItemID.DontHurtNatureBook)
            name = "Guide_to_Environmental_Preservation";
        else if (type == ItemID.DontHurtNatureBookInactive)
            name = "Guide_to_Environmental_Preservation_(Inactive)";
        else if (type == ItemID.DontHurtComboBook)
            name = "Guide_to_Peaceful_Coexistence";
        else if (type == ItemID.DontHurtComboBookInactive)
            name = "Guide_to_Peaceful_Coexistence_(Inactive)";
        return name;
    }

    public static void InitializeItemUrl()
    {
        ItemUrl = new Dictionary<int, string>();
        InitializeVanillaItems();
    }

    // Item names always come in English: the wiki's pages are named after them.
    static void InitializeVanillaItems()
    {
        string oldLanguage = Language.ActiveCulture.Name!;
        LanguageManager.Instance.SetLanguage("en-US");
        for (int type = 1; type < ItemID.Count; type++)
        {
            if (ItemID.Sets.Deprecated[type])
                continue;
            string name = LanguageManager.Instance.GetTextValue($"ItemName.{ItemID.Search.GetName(type)}") ?? "";
            ItemUrl[type] = GetReplacementsForItem(type, DefaultSearchStr(name));
        }
        LanguageManager.Instance.SetLanguage(oldLanguage);
    }

    // Context 6 is the trash slot: for one draw, it takes a normal slot's look and cursor 2 as its icon.
    static void ChangeTextures()
    {
        oldBgTexture = TextureAssets.InventoryBack7;
        oldBgHighlightTexture = TextureAssets.InventoryBackHighlight7;
        oldTrashIcon = TextureAssets.Trash;
        TextureAssets.InventoryBack7 = TextureAssets.InventoryBack2;
        TextureAssets.InventoryBackHighlight7 = TextureAssets.InventoryBackHighlight2;
        TextureAssets.Trash = TextureAssets.Cursors[2];
    }

    static void ResetTextures()
    {
        TextureAssets.InventoryBack7 = oldBgTexture!;
        TextureAssets.InventoryBackHighlight7 = oldBgHighlightTexture!;
        TextureAssets.Trash = oldTrashIcon!;
        oldBgTexture = null;
        oldBgHighlightTexture = null;
        oldTrashIcon = null;
    }

    static Item NewAirItem()
    {
        Item item = Item.New();
        item.SetDefaults(0, default);
        return item;
    }

    void ResetState()
    {
        pressed = false;
        wasMouseLeft = false;
        LastClickTime = Environment.TickCount64;
        DoubleClickDelay = 200;
        tempSlotPosition = default;
    }

    public override void SetupContent()
    {
        Slot = GameArray<Item>.New(Item.Class, 1);
        slotHandle = new GameHandle(Slot.Pointer);
        Slot[0] = NewAirItem();

        PanelWidth = 38;
        PanelHeight = 38;
        Panel = new Rectangle(0, 0, PanelWidth, PanelHeight);
        layout = CreateLayout(PanelWidth, PanelHeight);

        ResetState();
    }

    public override void Clear()
    {
        if (!Slot[0].IsAir)
            Main.player[Main.myPlayer].GetItem(Slot[0].DeepClone(), GetItemSettings.ReturnItemFromSlot, false);

        Slot[0] = NewAirItem();

        ResetState();
    }

    static void HandleSlot()
    {
        ItemSlot.singleSlotArray[0] = Slot[0];
        ItemSlot.Handle(ItemSlot.singleSlotArray, 0, 0);
        Slot[0] = ItemSlot.singleSlotArray[0];
    }

    public override void Draw()
    {
        Vector2 position = GameBindings.MainButtonPosition();
        Panel.X = (int)(position.X + PanelWidth + 3);
        if (Main.IsJourneyMode)
            Panel.X += PanelWidth + 3;
        Panel.Y = (int)position.Y;
        Recalculate(layout, Panel);

        bool justPressed = Main.mouseLeft && !wasMouseLeft;
        bool justReleased = !Main.mouseLeft && wasMouseLeft;
        bool hovered = Panel.Contains(Main.mouseX, Main.mouseY);
        if (justPressed && hovered)
            pressed = true;
        // A drag that leaves the slot takes the item out.
        if (pressed && Main.mouseLeft && !hovered && Main.mouseItem.IsAir && !Slot[0].IsAir)
        {
            pressed = false;
            HandleSlot();
        }
        if (hovered && !Main.mouseItem.IsAir && justReleased)
            HandleSlot();
        if (justReleased)
            pressed = false;

        if (hovered && Main.mouseLeft)
        {
            if (!Slot[0].IsAir)
            {
                Mod mod = ModContent.GetInstance<Wikithis>();
                Item hoveredItem = Slot[0].DeepClone();
                string text = hoveredItem.BestiaryNotes ?? "";
                if (hoveredItem.type > 0 && !ItemID.Sets.Deprecated[hoveredItem.type] && CheckUrl(hoveredItem.type))
                    text += $"\n[i:149] {mod.GetLocalization("Wikithis.OpenWiki").Value}";
                else
                    text += $"\n{mod.GetLocalization("Wikithis.SearchDisabled").Value}";
                hoveredItem.BestiaryNotes = text;
                ItemSlot.MouseHover(ref hoveredItem, 0);
            }
            else
            {
                int x = Main.mouseX - 8, y = Main.mouseY - 70;
                Main.instance.MouseText_DrawBuffTooltip(ModContent.GetInstance<Wikithis>().GetLocalization("Wikithis.PlaceItem").Value ?? "", "", ref x, ref y);
            }
        }

        if (hovered && justPressed && !Slot[0].IsAir)
        {
            long now = Environment.TickCount64;
            if (now - LastClickTime <= DoubleClickDelay)
                OpenWikiPage(Slot[0].type);
            LastClickTime = now;
        }

        if (hovered)
        {
            ItemSlot.CursorHighlight = true;
            if (!Main.mouseItem.IsAir)
                ItemSlot.SelectHighlight = true;
        }
        tempSlotPosition.X = Panel.X;
        tempSlotPosition.Y = Panel.Y;
        ChangeTextures();
        ItemSlot.Draw(Main.spriteBatch, Slot, WikiSlotContext, 0, tempSlotPosition, Color.White);
        ResetTextures();
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, default, default, default, default, default, new GameNullable<Matrix>(Matrix.Identity), true);

        wasMouseLeft = Main.mouseLeft;
    }
}
