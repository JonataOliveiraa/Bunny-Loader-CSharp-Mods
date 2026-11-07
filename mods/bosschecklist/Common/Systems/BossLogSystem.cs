using System;
using System.Collections.Generic;
using BossChecklistMod.Common.Players;
using BossChecklistMod.Common.UI;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace BossChecklistMod.Common.Systems;

public class BossLogSystem : ModSystem
{
    public UserInterface bossChecklistInterface = null!;
    public BossChecklistUI bossChecklistUI = null!;
    public UserInterface BossLogInterface = null!;
    public BossLogUI BossLog = null!;
    public BossRadarUI BossRadar = null!;

    GameInterfaceLayer[]? layers;
    bool toggleChecklist;

    public static ModTouchButton BossLogButton = null!;
    public static ModTouchButton BossChecklistButton = null!;

    public bool Ready => BossLog != null;

    static bool InWorld() => !Main.gameMenu && !Main.LocalPlayer.IsNull;

    public override void Load()
    {
        BossLogButton = TouchButtonLoader.RegisterButton(Mod, "BossLog");
        BossLogButton.Icon = "Assets/Buttons/BossLog";
        BossLogButton.Enabled = InWorld;
        BossLogButton.OnPress = _ => ToggleBossLog();
        BossChecklistButton = TouchButtonLoader.RegisterButton(Mod, "BossChecklist");
        BossChecklistButton.Icon = "Assets/Buttons/BossChecklist";
        BossChecklistButton.Enabled = InWorld;
        BossChecklistButton.OnPress = _ => ToggleChecklist();
    }

    public override void OnModLoad()
    {
        if (Main.dedServ)
            return;
        Refs.system = this;
        Shield.Install();
        BossChecklistPlayer.OnEnter(() =>
        {
            if (bossChecklistUI != null)
                bossChecklistUI.Visible = false;
            if (BossLog != null && BossLog.BossLogVisible)
                BossLog.ToggleBossLog(false);
        });
        BossChecklistWorld.OnWorldDataChanged(() =>
        {
            BossRadar?.InvalidateWhitelist();
            if (bossChecklistUI != null && bossChecklistUI.Visible)
                bossChecklistUI.UpdateCheckboxes();
            if (BossLog != null && BossLog.BossLogVisible && BossLog.PageNum == LogPages.PAGE_TOC && !BossLog.HiddenEntriesMode)
                BossLog.RefreshPageContent();
        });
        Cfg.OnConfigChanged(key =>
        {
            if (key == nameof(FeatureConfiguration.RadarMiniBosses))
                BossRadar?.InvalidateWhitelist();
            if (BossLog != null && BossLog.BossLogVisible)
            {
                BossLog.UpdateIndicators();
                BossLog.RefreshPageContent();
            }
        });
    }

    public override void PostSetupContent()
    {
        if (Main.dedServ)
            return;
        Refs.system = this;
        BuildUI();
    }

    public override void OnWorldLoad()
    {
        if (!Main.dedServ)
            Util.Safe("rebuild UI", BuildUI);
    }

    void BuildUI()
    {
        bossChecklistUI = new BossChecklistUI();
        bossChecklistUI.Activate();
        bossChecklistInterface = new UserInterface();

        BossLog = new BossLogUI();
        BossLog.Activate();
        BossLogInterface = new UserInterface();

        BossRadar = new BossRadarUI();
        BossRadar.Activate();
    }

    public override void OnWorldUnload()
    {
        if (BossLog != null && BossLog.BossLogVisible)
            BossLog.ToggleBossLog(false);
        if (bossChecklistUI != null)
            bossChecklistUI.Visible = false;
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (bossChecklistUI == null || bossChecklistInterface == null || BossLogInterface == null)
            return;
        if (toggleChecklist)
        {
            toggleChecklist = false;
            bossChecklistUI.Toggle();
        }
        bossChecklistInterface.Update(gameTime);

        // The closed book has no state in its interface: an interface with a state replays
        // every touch each frame, which is work a closed book has no use for.
        bool wanted = BossLog.NeedsUpdate;
        if (wanted && BossLogInterface.CurrentState == null)
            BossLogInterface.SetState(BossLog);
        else if (!wanted && BossLogInterface.CurrentState != null)
            BossLogInterface.SetState(null);
        BossLogInterface.Update(gameTime);

        Shield.rects.Clear();
        if (BossLog.BossLogVisible)
            BossLog.AddShieldRects(Shield.rects);
        if (bossChecklistUI.Visible)
            bossChecklistUI.AddShieldRects(Shield.rects);
    }

    public void ToggleBossLog()
    {
        if (BossLog != null)
            BossLog.PendingToggleBossLogUI = true;
    }

    public void ToggleChecklist() => toggleChecklist = true;

    static void Ordered(Action draw)
    {
        D.Ordered(true);
        try
        {
            draw();
        }
        finally
        {
            D.Ordered(false);
        }
    }

    GameInterfaceLayer[] Layers()
    {
        if (layers != null)
            return layers;
        // Restarting the batch has a cost, so a layer with nothing to draw leaves it alone.
        Action drawChecklist = () => bossChecklistInterface.Draw(Main.spriteBatch, default);
        Action drawLog = () => BossLogInterface.Draw(Main.spriteBatch, default);
        Action drawRadar = () => BossRadar.Draw(Main.spriteBatch);
        Action drawHover = Hover.Draw;
        Action drawTracker = () => Util.Safe("record tracker", DrawRecordTracker);
        layers = new GameInterfaceLayer[]
        {
            new LegacyGameInterfaceLayer("BossChecklist: Boss Checklist", () =>
            {
                if (bossChecklistUI != null && bossChecklistUI.Visible)
                    Ordered(drawChecklist);
                return true;
            }, InterfaceScaleType.UI),
            new LegacyGameInterfaceLayer("BossChecklist: Boss Log UI", () =>
            {
                if (BossLogInterface?.CurrentState != null && BossLog.BossLogVisible)
                    Ordered(drawLog);
                return true;
            }, InterfaceScaleType.UI),
            new LegacyGameInterfaceLayer("BossChecklist: Boss Radar", () =>
            {
                if (BossRadar != null && BossRadar.Prepare())
                    Ordered(drawRadar);
                return true;
            }, InterfaceScaleType.UI),
            new LegacyGameInterfaceLayer("BossChecklist: Custom UI Hover Text", () =>
            {
                if (Hover.HasWork)
                    Ordered(drawHover);
                return true;
            }, InterfaceScaleType.UI),
            new LegacyGameInterfaceLayer("BossChecklist: Record Tracker Debugger", () =>
            {
                if (Cfg.Feature.DisplayRecordTracking && Util.IsSP && Fight.CurrentTracker() != null)
                    Ordered(drawTracker);
                return true;
            }, InterfaceScaleType.UI),
        };
        return layers;
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> list)
    {
        if (BossLog == null)
            return;
        GameInterfaceLayer[] own = Layers();
        int index = list.FindIndex(static layer => layer.Name == "Vanilla: Mouse Text");
        if (index == -1)
            index = list.Count;
        list.Insert(index, own[0]);
        list.Insert(++index, own[1]);
        list.Insert(++index, own[2]);
        list.Insert(index + 2 <= list.Count ? index + 2 : list.Count, own[3]);
        int chat = list.FindIndex(static layer => layer.Name == "Vanilla: Player Chat");
        if (chat != -1)
            list.Insert(chat, own[4]);
    }

    static void DrawRecordTracker()
    {
        PersonalRecords? r = Fight.CurrentTracker();
        if (r == null)
            return;
        EntryInfo? e = Tracker.FindEntryFromKey(r.bossKey);
        if (e == null)
            return;
        string text = "Boss Checklist: Record Tracker\n[#" + e.GetIndex + "] " + e.DisplayName + " (" + r.RecordIndex + ")\n"
            + (r.trackDeaths > 0 ? "[i:321] " + r.trackDeaths + " " : "") + "[i:901] " + r.trackHits + " [i:3099] " + Records.TimeConversion(r.trackDuration);
        RichLayout layout = Rich.Layout(text, 460, 1, C.Tomato);
        Rich.Draw(layout, Main.screenWidth / 2f - layout.MeasuredWidth() / 2f, Main.screenHeight - 50 - 11 - 24 - layout.height, 1);
    }
}
