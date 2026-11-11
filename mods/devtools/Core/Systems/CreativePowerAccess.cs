using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Terraria.GameContent.Creative;

namespace DevToolMod.Core.Systems;

// Journey mode's time powers, looked up by the names the JavaScript version used.
public static class CreativePowerAccess
{
    // The manager is unique and holds its powers for the whole process, so the addresses stay good.
    static readonly Dictionary<string, IntPtr> Powers = new();

    static GameObject GetPower(string name)
    {
        if (Powers.TryGetValue(name, out IntPtr cached)) return new GameObject(cached);

        CreativePowerManager manager = CreativePowerManager.Instance;
        if (manager.IsNull) return default;

        GameObject powersByName = manager._powersByName;
        if (powersByName.IsNull) return default;
        if (!new GameDictionary(powersByName).TryGetValue(GameString.New(name), out IntPtr power) || power == IntPtr.Zero)
            return default;
        Powers[name] = power;
        return new GameObject(power);
    }

    public static bool TimeFrozen
    {
        get
        {
            GameObject power = GetPower("time_setfrozen");
            return !power.IsNull && CreativePowers.ASharedTogglePower.From(power).Enabled;
        }
    }

    public static void SetTimeFrozen(bool enabled)
    {
        GameObject power = GetPower("time_setfrozen");
        if (power.IsNull) return;
        CreativePowers.ASharedTogglePower.From(power).SetPowerInfo(enabled);
    }

    public static int TargetTimeRate
    {
        get
        {
            GameObject power = GetPower("time_setspeed");
            return power.IsNull ? 1 : CreativePowers.ModifyTimeRate.From(power).TargetTimeRate;
        }
        set
        {
            GameObject power = GetPower("time_setspeed");
            if (!power.IsNull)
                CreativePowers.ModifyTimeRate.From(power).TargetTimeRate = value;
        }
    }
}
