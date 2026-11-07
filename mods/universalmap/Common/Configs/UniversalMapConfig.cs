using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace UniversalMapMod.Common.Configs;

public class UniversalMapConfig : ModConfig
{
    public static UniversalMapConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Sync")]
    [Range(1, 10)]
    [Increment(1)]
    [DefaultValue(1)]
    public int Interval;

    [DefaultValue(false)]
    public bool Notifications;

    public static int IntervalMinutes => Math.Max(1, Math.Min(10, Instance == null ? 1 : Instance.Interval));

    public static bool ShowNotifications => Instance != null && Instance.Notifications;
}
