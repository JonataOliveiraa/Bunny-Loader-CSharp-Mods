using System.Collections.Generic;
using ModDifficultyMod.DifficultySystem.Utils;

namespace ModDifficultyMod.DifficultySystem;

public static class DifficultyLoader
{
    public static readonly List<ModDifficulty> Items = new();

    static int count = DifficultyID.Count;
    static bool ready;

    public static void Register(ModDifficulty difficulty)
    {
        difficulty.Type = count++;
        Items.Add(difficulty);
    }

    // The original did this right after the game's Initialize_AlmostEverything. C# mods can load
    // after that, so it happens the first time the menu needs it, on the game thread.
    public static void SetupContent()
    {
        if (ready)
            return;
        ready = true;
        TransactionButton_Layout layout = MenuLayouts.Normal;
        foreach (ModDifficulty diff in Items)
        {
            diff.Layout = LayoutManager.TransactionButtonClone(layout);
            diff.KeepLayout();

            String_Layout label = String_Layout.New();
            label.Copy(layout.Label);
            diff.Layout.Label = label;

            Texture_Layout icon = Texture_Layout.New();
            icon.Copy(layout.IconTexture);
            diff.Layout.IconTexture = icon;

            Texture_Layout iconHighlight = Texture_Layout.New();
            iconHighlight.Copy(layout.IconTextureHighlight);
            diff.Layout.IconTextureHighlight = iconHighlight;

            diff.SetupContent();
        }
    }
}
