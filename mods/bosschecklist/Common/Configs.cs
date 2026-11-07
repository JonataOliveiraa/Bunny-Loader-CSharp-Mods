using System;
using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader.Config;
using static BossChecklistMod.Common.ModText;

namespace BossChecklistMod.Common;

public enum BossFilterMode { Show, HideWhenCompleted }

public enum EntryFilterMode { Show, HideWhenCompleted, Hide }

public enum CheckmarkType { Check_Empty, Check_X, X_Empty, StrikeThrough }

public enum MessageMode { Disabled, Generic, Unique }

public class BossLogConfiguration : ModConfig
{
    public static BossLogConfiguration Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("BossLogUI")]
    [ColorNoAlpha]
    [DefaultValue(typeof(Color), "87, 181, 92, 255")]
    public Color BossLogColor;

    [DefaultValue(true)]
    public bool ShowInteractionTooltips;

    [Range(0.5f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(0.85f)]
    public float BookScale;

    [Range(0.5f, 1.5f)]
    [Increment(0.05f)]
    [DefaultValue(0.9f)]
    public float ChecklistScale;

    [Header("BossLogChecklist")]
    [DefaultValue(true)]
    public bool AutomaticChecklist;

    [DefaultValue(false)]
    public bool ProgressiveChecklist;

    [DefaultValue(false)]
    public bool PromptDisabled;

    [DefaultValue(true)]
    public bool HideUnavailable;

    [DefaultValue(false)]
    public bool HideUnsupported;

    [DefaultValue(false)]
    public bool OnlyShowBossContent;

    [Cycle]
    [DefaultValue(BossFilterMode.Show)]
    public BossFilterMode FilterBosses;

    [Cycle]
    [DefaultValue(EntryFilterMode.Show)]
    public EntryFilterMode FilterMiniBosses;

    [Cycle]
    [DefaultValue(EntryFilterMode.Show)]
    public EntryFilterMode FilterEvents;

    [DefaultValue(true)]
    public bool ColoredBossText;

    [Cycle]
    [DefaultValue(CheckmarkType.Check_Empty)]
    public CheckmarkType SelectedCheckmarkType;

    [DefaultValue(true)]
    public bool DrawNextMark;

    [DefaultValue(true)]
    public bool ShowProgressBars;

    [DefaultValue(false)]
    public bool SpawnItemCraftingChecklist;

    [DefaultValue(false)]
    public bool LootCheckVisibility;

    [DefaultValue(false)]
    public bool OnlyCheckDroppedLoot;

    [Header("Debug")]
    [DefaultValue(false)]
    public bool ModCallLogVerbose;

    [DefaultValue(false)]
    public bool AccessInternalNames;

    [DefaultValue(false)]
    public bool ShowProgressionValue;

    [DefaultValue(false)]
    public bool ShowCollectionType;

    [DefaultValue(false)]
    public bool EnabledResetOptions;

    bool lastResetOptions;

    public override void OnLoaded() => lastResetOptions = EnabledResetOptions;

    public override void OnChanged()
    {
        // The hook has no field name: the option this rule watches is compared by hand.
        bool turnedOn = EnabledResetOptions && !lastResetOptions;
        lastResetOptions = EnabledResetOptions;
        if (turnedOn && !ShowInteractionTooltips)
            Cfg.SetLog(c => c.ShowInteractionTooltips = true);
        Cfg.Notify("");
    }
}

public class FeatureConfiguration : ModConfig
{
    public static FeatureConfiguration Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("BossRecords")]
    [DefaultValue(true)]
    public bool RecordTrackingEnabled;

    [DefaultValue(true)]
    public bool AllowNewRecords;

    [DefaultValue(true)]
    public bool NewRecordLogGlow;

    [DefaultValue(false)]
    public bool DisplayRecordTracking;

    [Header("ChatMessages")]
    [Cycle]
    [DefaultValue(MessageMode.Generic)]
    public MessageMode DespawnMessageType;

    [Cycle]
    [DefaultValue(MessageMode.Generic)]
    public MessageMode LimbMessages;

    [Cycle]
    [DefaultValue(MessageMode.Generic)]
    public MessageMode MoonMessages;

    [DefaultValue(true)]
    public bool TimerSounds;

    [Header("ItemMapDetection")]
    [DefaultValue(true)]
    public bool TreasureBagsOnMap;

    [DefaultValue(true)]
    public bool FragmentsOnMap;

    [DefaultValue(false)]
    public bool ScalesOnMap;

    [Header("BossRadar")]
    [DefaultValue(true)]
    public bool EnableBossRadar;

    [DefaultValue(false)]
    public bool RadarMiniBosses;

    [Range(0.35f, 0.85f)]
    [Increment(0.05f)]
    [DefaultValue(0.75f)]
    public float RadarOpacity;

    bool guard, lastTracking = true, lastAllowNew = true, lastRadarMini;

    public override void OnLoaded()
    {
        lastTracking = RecordTrackingEnabled;
        lastAllowNew = AllowNewRecords;
        lastRadarMini = RadarMiniBosses;
    }

    public override void OnChanged()
    {
        if (guard)
            return;
        CheckRecordOption(nameof(RecordTrackingEnabled), RecordTrackingEnabled, ref lastTracking, value => RecordTrackingEnabled = value);
        CheckRecordOption(nameof(AllowNewRecords), AllowNewRecords, ref lastAllowNew, value => AllowNewRecords = value);
        if (RadarMiniBosses != lastRadarMini)
        {
            lastRadarMini = RadarMiniBosses;
            Cfg.Notify(nameof(RadarMiniBosses));
        }
        Cfg.Notify("");
    }

    // These two options cannot change while a boss is alive: the change is undone with a message.
    void CheckRecordOption(string key, bool value, ref bool last, Action<bool> set)
    {
        if (value == last)
            return;
        string? blocker = Main.gameMenu ? null : Fight.ActiveBossName();
        if (blocker != null)
        {
            guard = true;
            set(last);
            SaveChanges();
            guard = false;
            Util.Say(L("Configs.Common.InvalidChange", L("Configs.FeatureConfiguration." + key + ".Label"), blocker), Data.Orange);
            return;
        }
        last = value;
        Cfg.Notify(key);
    }
}

public static class Cfg
{
    public static BossLogConfiguration Log => BossLogConfiguration.Instance;
    public static FeatureConfiguration Feature => FeatureConfiguration.Instance;

    static readonly List<Action<string>> listeners = new();

    public static void OnConfigChanged(Action<string> fn) => listeners.Add(fn);

    public static void Notify(string key)
    {
        foreach (Action<string> fn in listeners)
            Util.Safe("config " + key, () => fn(key));
    }

    public static bool Tracking => Feature.RecordTrackingEnabled;
    public static bool NewRecords => Feature.RecordTrackingEnabled && Feature.AllowNewRecords;

    public static void SetLog(Action<BossLogConfiguration> change)
    {
        change(Log);
        Log.SaveChanges();
    }

    public static Color LogColor(int alpha = 255)
    {
        Color c = Log.BossLogColor;
        return new Color(c.R, c.G, c.B, alpha);
    }
}
