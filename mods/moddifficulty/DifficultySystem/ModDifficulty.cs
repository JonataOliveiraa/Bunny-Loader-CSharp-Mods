using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ModDifficultyMod.DifficultySystem;

public abstract class ModDifficulty
{
    public int Type = -1;
    public string Texture = "";
    public TransactionButton_Layout Layout;

    // A layout made here is only known to C#, so the game's garbage collector has to be told.
    GameHandle? layoutHandle;
    Asset<Texture2D>? difficultyIcon, difficultyBackground, difficultyBunny;

    // Whether the difficulty is active in the current world.
    public bool IsActive
    {
        get
        {
            WorldFileData data = Main.ActiveWorldFileData;
            return !data.IsNull && data.GameMode == Type;
        }
    }

    static string T(string section, string key, string fallback)
    {
        string full = "Mods.moddifficulty." + section + "." + key;
        string? text = Language.GetTextValue(full);
        return string.IsNullOrEmpty(text) || text == full ? fallback : text;
    }

    // The name and description shown in the world creation and selection menus.
    public virtual string DisplayName => T(GetType().Name, "Name", GetType().Name);

    public virtual string Description => T(GetType().Name, "Description", "");

    // The textures used in the world creation menu. Null where the file does not exist.
    public Texture2D? DifficultyIcon => difficultyIcon?.Value;

    public Texture2D? DifficultyBackground => difficultyBackground?.Value;

    public Texture2D? DifficultyBunny => difficultyBunny?.Value;

    // How the difficulty behaves in the game:
    // Normal (0), no changes. Expert (1), bosses drop treasure bags. Master (2), bosses drop relics.
    // Journey (3), creative mode and item duplication.
    public virtual int VanillaEquivalent => DifficultyID.Normal;

    public void KeepLayout() => layoutHandle = new GameHandle(Layout.Pointer);

    static Asset<Texture2D>? Find(string path)
    {
        string name = "moddifficulty/" + path;
        return ModContent.HasAsset(name) ? ModContent.Request<Texture2D>(name) : null;
    }

    // The difficulty's static data, such as its textures. This is done automatically.
    public virtual void SetupContent()
    {
        difficultyIcon = Find(Texture);
        difficultyBackground = Find(Texture + "_Preview");
        difficultyBunny = Find(Texture + "_Bunny");
    }

    // Adds or changes entries of the difficulty list in the world creation menu.
    public virtual void SetupDifficultyList(List<int> list) => list.Add(Type);

    // The color shown in the world creation and selection menus.
    public virtual Color GetColor() => Color.White;

    // For example the Journey difficulty can only be selected by a Journey character.
    public virtual bool CanBeSelected()
    {
        bool isCreative = Main.LocalPlayer.difficulty == DifficultyID.Journey;
        return VanillaEquivalent == DifficultyID.Journey ? isCreative : !isCreative;
    }

    // Called when this difficulty is selected in the world creation menu.
    public virtual void OnSelected(int oldDifficulty) { }

    // The level used for scaling NPCs and projectiles. See Terraria.DataStructures.GameDifficultyLevel.
    public virtual float GetDifficultyLevel(float currentLevel) => currentLevel;

    public static void Register(ModDifficulty difficulty) => DifficultyLoader.Register(difficulty);

    public static bool IsModDifficulty(int type) => type >= DifficultyID.Count;

    public static ModDifficulty? GetModDifficulty(int type)
    {
        List<ModDifficulty> items = DifficultyLoader.Items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].Type == type)
                return items[i];
        }
        return null;
    }
}
