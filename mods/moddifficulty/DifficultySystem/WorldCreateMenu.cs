using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ModDifficultyMod.DifficultySystem.Utils;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;

namespace ModDifficultyMod.DifficultySystem;

public static class WorldCreateMenu
{
    static List<int>? order;

    public static void Initialize()
    {
        // While the difficulty list is open, the rest of the menu must not take touches.
        On_GUIWorldCreateMenu.DrawName += (orig, self) => { bool on = Exclusive(self); orig(self); EndExclusive(on); };
        On_GUIWorldCreateMenu.DrawWorldSize += (orig, self) => { bool on = Exclusive(self); orig(self); EndExclusive(on); };
        On_GUIWorldCreateMenu.DrawWorldEvilType += (orig, self) => { bool on = Exclusive(self); orig(self); EndExclusive(on); };
        On_GUIWorldCreateMenu.DrawCategoryButtons += (orig, self) => { bool on = Exclusive(self); orig(self); EndExclusive(on); };
        On_GUIWorldCreateMenu.DrawMainButtons += (orig, self) => { bool on = Exclusive(self); orig(self); EndExclusive(on); };
        On_GUIWorldCreateMenu.DrawRandomiseButtons += (orig, self) => { bool on = Exclusive(self); orig(self); EndExclusive(on); };

        On_GUIWorldCreateMenu.DrawImage += HandlePreviewDifficultyImage;
        On_GUIWorldCreateMenu.DrawWorldDifficulty += (orig, self) => DrawWorldDifficulty(self);

        On_GUIWorldSelectMenu.WorldDraw += HandleDifficultyDisplayName;
        On_GUIWorldSelectMenu.SortEntry += (orig, self, a, b) => SortEntry(self, a, b);
        On_GUIWorldSelectMenu.CanWorldBePlayed += CanWorldBePlayed;
    }

    static bool Exclusive(GUIWorldCreateMenu self)
    {
        bool on = self.displayDifficultyOptions;
        if (on)
            GUIInputRegionManager.Instance.DisableAllPicking();
        return on;
    }

    static void EndExclusive(bool on)
    {
        if (on)
            GUIInputRegionManager.Instance.RenablePicking();
    }

    static List<int> Order()
    {
        if (order != null)
            return order;
        order = new List<int> { DifficultyID.Journey, DifficultyID.Normal, DifficultyID.Expert, DifficultyID.Master };
        foreach (ModDifficulty diff in DifficultyLoader.Items)
            diff.SetupDifficultyList(order);
        return order;
    }

    static int SortEntry(GUIWorldSelectMenu self, WorldFileData a, WorldFileData b)
    {
        if (a.IsNull && b.IsNull)
            return 0;
        if (a.IsNull || a.Name == null)
            return 1;
        if (b.IsNull || b.Name == null)
            return -1;
        bool flag = self.CanWorldBePlayed(a);
        if (flag != self.CanWorldBePlayed(b))
            return flag ? -1 : 1;
        if (a._isFavorite != b._isFavorite)
            return a._isFavorite ? -1 : 1;
        return self.CompareNatural(a.Name, b.Name);
    }

    static bool CanWorldBePlayed(On_GUIWorldSelectMenu.orig_CanWorldBePlayed orig, GUIWorldSelectMenu self, WorldFileData fileData)
    {
        ModDifficulty? diff = fileData.GameMode >= DifficultyID.Count ? ModDifficulty.GetModDifficulty(fileData.GameMode) : null;
        return diff != null ? diff.CanBeSelected() : orig(self, fileData);
    }

    static void HandleDifficultyDisplayName(On_GUIWorldSelectMenu.orig_WorldDraw orig, GUIWorldSelectMenu self, ItemGrid_Layout grid, int index, Vector2 pos, float scale)
    {
        ModDifficulty? diff = null;
        var worlds = new GameList(self.SortedWorldData);
        if (!worlds.IsNull && index >= 0 && index < worlds.Count)
        {
            WorldFileData fileData = worlds.Get<WorldFileData>(index);
            if (!fileData.IsNull && fileData.GameMode >= DifficultyID.Count)
                diff = ModDifficulty.GetModDifficulty(fileData.GameMode);
        }

        String_Layout layout = MenuLayouts.ItemMode;
        float oldScale = layout.Scale;
        // Scale 0 hides the name the game would write for this world.
        if (diff != null)
            layout.Scale = 0;
        orig(self, grid, index, pos, scale);
        layout.Scale = oldScale;
        if (diff != null)
        {
            layout.Color = diff.GetColor();
            GUIString.Draw(layout, diff.DisplayName);
        }
    }

    static void HandlePreviewDifficultyImage(On_GUIWorldCreateMenu.orig_DrawImage orig, GUIWorldCreateMenu self)
    {
        DifficultyLoader.SetupContent();
        ModDifficulty? diff = self.difficulty >= DifficultyID.Count ? ModDifficulty.GetModDifficulty(self.difficulty) : null;
        if (diff == null)
        {
            orig(self);
            return;
        }
        Texture2D oldBackground = GUIWorldCreateMenu.previewDifficultyNormalBackground;
        Texture2D oldBunny = GUIWorldCreateMenu.previewDifficultyNormalBunny;
        if (diff.DifficultyBackground is Texture2D background)
            GUIWorldCreateMenu.previewDifficultyNormalBackground = background;
        if (diff.DifficultyBunny is Texture2D bunny)
            GUIWorldCreateMenu.previewDifficultyNormalBunny = bunny;
        orig(self);
        GUIWorldCreateMenu.previewDifficultyNormalBackground = oldBackground;
        GUIWorldCreateMenu.previewDifficultyNormalBunny = oldBunny;
    }

    static string Text(string key) => Language.GetTextValue(key) ?? key;

    static void Tick() => SoundEngine.PlaySound(SoundID.MenuTick, -1, -1, 1, 1f, 0f);

    static void SetDifficulty(GUIWorldCreateMenu self, int value)
    {
        string? description;
        if (value == DifficultyID.Normal)
            description = self.WorldDescriptionNormal;
        else if (value == DifficultyID.Expert)
            description = self.WorldDescriptionExpert;
        else if (value == DifficultyID.Master)
            description = self.WorldDescriptionMaster;
        else if (value == DifficultyID.Journey)
            description = self.WorldDescriptionCreative;
        else
            description = ModDifficulty.GetModDifficulty(value)?.Description;
        self.description = description ?? self.WorldDescriptionDefault;
        self.difficulty = value;
        self.displayDifficultyOptions = false;
        Tick();
    }

    static GUITransactionButton.InputState Button(TransactionButton_Layout layout, string label, bool disabled, ref float scale, bool forcePressed) =>
        GUITransactionButton.Draw(layout, default(Texture2D), label, disabled, ref scale, default(GameNullable<Color>), forcePressed, false, false, false);

    static bool Clicked(TransactionButton_Layout layout, string label, bool disabled, bool forcePressed)
    {
        float scale = 1f;
        return Button(layout, label, disabled, ref scale, forcePressed) == GUITransactionButton.InputState.Clicked;
    }

    static void DrawWorldDifficulty(GUIWorldCreateMenu self)
    {
        DifficultyLoader.SetupContent();

        int difficulty = self.difficulty;
        bool isModDiff = ModDifficulty.IsModDifficulty(difficulty);
        ModDifficulty? modDiff = isModDiff ? ModDifficulty.GetModDifficulty(difficulty) : null;

        bool showOptions = self.displayDifficultyOptions;
        Panel_Layout backing = MenuLayouts.DifficultyOptionsBacking;
        bool isOverOptions = showOptions && GUIPanel.Region(backing).Contains(Main.mouseX, Main.mouseY);

        if (showOptions)
        {
            Panel_Layout greyout = MenuLayouts.Greyout;
            GUIPanel.RegisterPickingRegion(greyout);
            GUIPanel.Draw(greyout, false, default, default, default);
        }

        bool mousePressed = Main.mouseLeft && Main.mouseLeftRelease;
        if (showOptions && !isOverOptions && mousePressed)
        {
            self.displayDifficultyOptions = false;
            Tick();
        }

        TransactionButton_Layout normal = MenuLayouts.Normal, expert = MenuLayouts.Expert, master = MenuLayouts.Master, journey = MenuLayouts.Journey;
        string normalText = Text("UI.Normal"), expertText = Text("UI.Expert");
        string masterText = Text("UI.Master"), journeyText = Text("UI.Creative");
        string vanillaText = difficulty == 0 ? normalText : difficulty == 1 ? expertText : difficulty == 2 ? masterText : journeyText;
        Color vanillaColor = difficulty == 0 ? normal.Label.Color : difficulty == 1 ? expert.Label.Color : difficulty == 2 ? master.Label.Color : journey.Label.Color;

        TransactionButton_Layout value = MenuLayouts.DifficultyValue;
        Color current = !isModDiff ? vanillaColor : modDiff != null ? modDiff.GetColor() : Color.White;
        value.Label.Color = current;
        value.DisabledLabelColour = current;
        value.PressedLabelColour = current;

        if (showOptions)
            GUIInputRegionManager.Instance.DisableAllPicking();
        float valueScale = 0f;
        GUITransactionButton.InputState state = Button(value, !isModDiff ? vanillaText : modDiff != null ? modDiff.DisplayName : "Undefined", false, ref valueScale, false);
        GUIString.Draw(MenuLayouts.DifficultyTitle, Text("Mobile.Difficulty"));
        if (state == GUITransactionButton.InputState.Clicked)
        {
            self.displayDifficultyOptions = true;
            Tick();
        }
        if (showOptions)
            GUIInputRegionManager.Instance.RenablePicking();

        if (!showOptions)
            return;

        const float buttonH = 32, rowH = 38;
        float centerY = value.Location.Y;

        List<int> list = Order();
        int selectedIndex = list.IndexOf(difficulty);
        for (int i = 0; i < list.Count; i++)
        {
            int type = list[i];
            TransactionButton_Layout layout = type switch
            {
                DifficultyID.Normal => normal,
                DifficultyID.Expert => expert,
                DifficultyID.Master => master,
                DifficultyID.Journey => journey,
                _ => ModDifficulty.GetModDifficulty(type)!.Layout,
            };
            layout.Location.Y = centerY + (i - selectedIndex) * rowH + 2;
        }

        backing.Anchor = LayoutCalculator.AnchorType.TopRight;
        backing.Size.Y = list.Count * rowH + 16;
        float firstY = centerY - selectedIndex * rowH + 2;
        float lastY = centerY + (list.Count - 1 - selectedIndex) * rowH + 2;
        backing.Location.Y = (firstY + lastY) / 2 + buttonH / 2 + 1;

        GUIPanel.Draw(backing, false, default, default, default);

        bool isCreative = Main.LocalPlayer.difficulty == DifficultyID.Journey;

        if (Clicked(normal, normalText, isCreative, difficulty == DifficultyID.Normal))
            SetDifficulty(self, DifficultyID.Normal);
        if (Clicked(expert, expertText, isCreative, difficulty == DifficultyID.Expert))
            SetDifficulty(self, DifficultyID.Expert);
        if (Clicked(master, masterText, isCreative, difficulty == DifficultyID.Master))
            SetDifficulty(self, DifficultyID.Master);
        if (Clicked(journey, journeyText, !isCreative, isCreative))
            SetDifficulty(self, DifficultyID.Journey);

        foreach (ModDifficulty diff in DifficultyLoader.Items)
        {
            if (diff.DifficultyIcon is Texture2D icon)
            {
                diff.Layout.IconTexture._texture = icon;
                diff.Layout.IconTextureHighlight._texture = icon;
            }
            Color color = diff.GetColor();
            diff.Layout.Label.Color = color;
            diff.Layout.DisabledLabelColour = color;
            diff.Layout.PressedLabelColour = color;
            if (Clicked(diff.Layout, diff.DisplayName, !diff.CanBeSelected(), difficulty == diff.Type))
            {
                SetDifficulty(self, diff.Type);
                diff.OnSelected(difficulty);
            }
        }
    }
}
