using System;
using System.Collections.Generic;
using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Terraria;
using Terraria.UI;

namespace DevToolMod.Content.UI.Layers;

public class MainLayer : ModInterfaceLayer
{
    ModInterface ToolBarUI = null!;
    ModInterface ToolBarUI2 = null!;
    ModInterface CameraUI = null!;
    ModInterface EntityDespawnerUI = null!;
    ModInterface WeatherUI = null!;

    public override void SetupLayerList(List<GameInterfaceLayer> list) =>
        list.Insert(Math.Min(27, list.Count), Value);

    public override void SetupContent()
    {
        ToolBarUI = ModInterface.GetByName("ToolBarUI");
        ToolBarUI2 = ModInterface.GetByName("ToolBarUI_2");
        CameraUI = ModInterface.GetByName("CameraUI");
        EntityDespawnerUI = ModInterface.GetByName("EntityDespawnerUI");
        WeatherUI = ModInterface.GetByName("WeatherUI");
    }

    static bool CanDrawLayer()
    {
        if (Main.gameMenu || Main.gameInactive) return false;
        GUIInstance gui = GUIInstance.Active;
        return gui.IsNull || gui.GUISettings.IsNull || !gui.GUISettings.Visible;
    }

    public override void Draw()
    {
        if (!CanDrawLayer()) return;
        DrawPopups();
        DrawHelper.NewSpriteBatchForUI();
        // No batch restart between the two toolbars: one sits at the bottom of the screen and the
        // other right above it, so they do not cover each other. ToolBarUI's tooltip comes from
        // BuffTooltip (the game's own), which manages its own batch.
        ToolBarUI2.Draw();
        ToolBarUI.Draw();
        // No restart at the end either: nothing draws after this, and every End/Begin commits the
        // batch early, which makes the UI flicker.
    }

    void DrawPopups()
    {
        WeatherUI.Draw();
        DrawHelper.NewSpriteBatchForUI();
        CameraUI.Draw();
        DrawHelper.NewSpriteBatchForUI();
        EntityDespawnerUI.Draw();
    }
}
