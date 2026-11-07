using System;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.UI;

namespace TeleportKeeperMod.Common.UI;

// Keeps the game's on-screen controls from reacting to a touch that lands on the panel.
public static class Shield
{
    static bool installed, active;
    static CalculatedStyle rect;

    public static void Set(CalculatedStyle panel)
    {
        rect = panel;
        active = true;
    }

    public static void Clear() => active = false;

    public static void Install()
    {
        if (installed)
            return;
        installed = true;
        On_GUIVirtualInputController.HandleTouchCursorDown += static (orig, self, slot, position, cursor) =>
        {
            bool blocked = active && position.X >= rect.X && position.X < rect.X + rect.Width && position.Y >= rect.Y && position.Y < rect.Y + rect.Height;
            if (!blocked)
                orig(self, slot, position, cursor);
        };
    }
}

public static class Screen
{
    // The phone batches by texture unless told otherwise, and this UI draws things in layers:
    // the batch is restarted in order for the layer, and put back as it was afterwards.
    public static void Ordered(Action draw)
    {
        SpriteBatch sb = Main.spriteBatch;
        bool restarted = false;
        SpriteSortMode sort = default;
        BlendState blend = default;
        DepthStencilState depth = default;
        RasterizerState raster = default;
        Effect effect = default;
        GameNullable<Matrix> matrix = default;
        bool deferred = false;
        if (sb._beginCalled)
        {
            sort = sb._sortMode;
            blend = sb._blendState;
            depth = sb._depthStencilState;
            raster = sb._rasterizerState;
            effect = sb._effect;
            matrix = sb._hasMatrix ? new GameNullable<Matrix>(sb._renderMatrix) : default;
            deferred = sb.BatchDeffered;
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, blend, SamplerState.PointClamp, depth, raster, effect, matrix, false);
            restarted = true;
        }
        try
        {
            draw();
        }
        finally
        {
            if (restarted)
            {
                sb.End();
                sb.Begin(sort, blend, Main.DefaultSamplerState, depth, raster, effect, matrix, deferred);
            }
            else
            {
                sb.Flush();
            }
        }
    }

    public static bool GameUIBusy()
    {
        if (Main.gameMenu || Main.mapFullscreen || Main.playerInventory)
            return true;
        if (Main.ingameOptionsWindow || Main.editSign || Main.inFancyUI)
            return true;
        if (!string.IsNullOrEmpty(Main.npcChatText))
            return true;
        Player p = Main.LocalPlayer;
        if (p.IsNull || p.dead || p.chest != -1 || p.talkNPC != -1)
            return true;
        GUIInstance g = GUIInstance.Active;
        if (g.IsNull)
            return false;
        GUISettingsPauseMenu pause = g.GUISettingsPauseMenu;
        if (!pause.IsNull && pause.Enabled)
            return true;
        GUISettings settings = g.GUISettings;
        return !settings.IsNull && settings.Visible;
    }

    public static void CloseInventory()
    {
        if (!Main.playerInventory)
            return;
        GUIInstance g = GUIInstance.Active;
        if (!g.IsNull)
        {
            GUIPageIcons icons = g.GUIPageIcons;
            if (!icons.IsNull)
                icons.Close(true);
        }
        Main.playerInventory = false;
    }

    public static void HideMouseOverInteractions()
    {
        Main.player[Main.myPlayer].mouseInterface = true;
        Main.mouseText = true;
    }
}
