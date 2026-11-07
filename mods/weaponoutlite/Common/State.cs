using System;
using System.Collections.Generic;
using Terraria;

namespace WeaponOutLiteMod.Common;

public static class State
{
    public static readonly HashSet<int> customItemHoldStyles = new();
    public static readonly List<Func<Player, Item, PoseData, double, double, int, double, PoseData>> customPreDrawDataFuncs = new();
    public static readonly List<Func<Player, Item, int, double, int>> customDrawDepthFuncs = new();
    public static readonly List<Func<Player, Item, int, double, int>> customUpdateIdleBodyFrameFuncs = new();
    public static readonly Dictionary<int, PoseGroup> priorityItemHoldGroups = new();
    public static readonly Dictionary<int, PoseGroup> compatibilityItemHoldGroups = new();
    public static readonly Dictionary<int, DrawItemPoseID> priorityItemHoldPose = new();

    static readonly HashSet<string> once = new();

    public static void Log(string text) => Net.mod?.Logger.Info(text);

    public static void LogOnce(string key, string text)
    {
        if (once.Add(key))
            Log(text);
    }
}
