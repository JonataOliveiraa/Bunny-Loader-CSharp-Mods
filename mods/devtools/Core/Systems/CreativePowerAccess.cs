using System;
using System.Collections.Generic;
using BunnyLoader.Interop;

namespace DevToolMod.Core.Systems;

// Journey mode's time powers. CreativePowerManager and CreativePowers have no C# API, so these
// go through the loader's low-level interop, by the names the JavaScript version used.
public static unsafe class CreativePowerAccess
{
    const string CreativeNamespace = "Terraria.GameContent.Creative";

    static readonly GameClass Manager = new(CreativeNamespace, "CreativePowerManager");
    static readonly GameClass TogglePower = new(CreativeNamespace, "CreativePowers", "ASharedTogglePower");
    static readonly GameClass TimeRatePower = new(CreativeNamespace, "CreativePowers", "ModifyTimeRate");

    static MethodRef getInstance;
    static MethodRef setPowerInfo;
    static int powersByNameOffset = -1;
    static int enabledOffset = -1;
    static int targetTimeRateOffset = -1;

    // The manager is unique and holds its powers for the whole process, so the addresses stay good.
    static readonly Dictionary<string, IntPtr> Powers = new();

    static IntPtr GetPower(string name)
    {
        if (Powers.TryGetValue(name, out IntPtr cached)) return cached;

        ref readonly MethodRef method = ref GameInterop.Method(ref getInstance, Manager, "CreativePowerManager get_Instance()", new[] { -1 });
        CallFrame frame = default;
        frame.X[0] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
        IntPtr manager = (IntPtr)(long)frame.Out[0];
        if (manager == IntPtr.Zero) return IntPtr.Zero;

        IntPtr powersByName = *(IntPtr*)(manager + GameInterop.Offset(ref powersByNameOffset, Manager, "_powersByName"));
        if (powersByName == IntPtr.Zero) return IntPtr.Zero;
        if (!new GameDictionary(powersByName).TryGetValue(GameString.New(name), out IntPtr power) || power == IntPtr.Zero)
            return IntPtr.Zero;
        Powers[name] = power;
        return power;
    }

    public static bool TimeFrozen
    {
        get
        {
            IntPtr power = GetPower("time_setfrozen");
            return power != IntPtr.Zero && *(bool*)(power + GameInterop.Offset(ref enabledOffset, TogglePower, "<Enabled>k__BackingField"));
        }
    }

    public static void SetTimeFrozen(bool enabled)
    {
        IntPtr power = GetPower("time_setfrozen");
        if (power == IntPtr.Zero) return;
        ref readonly MethodRef method = ref GameInterop.Method(ref setPowerInfo, TogglePower, "void SetPowerInfo(bool enabled)", new[] { -1, -1 });
        CallFrame frame = default;
        frame.X[0] = (long)power;
        frame.X[1] = enabled ? 1 : 0;
        frame.X[2] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
    }

    public static int TargetTimeRate
    {
        get
        {
            IntPtr power = GetPower("time_setspeed");
            return power == IntPtr.Zero ? 1 : *(int*)(power + GameInterop.Offset(ref targetTimeRateOffset, TimeRatePower, "<TargetTimeRate>k__BackingField"));
        }
        set
        {
            IntPtr power = GetPower("time_setspeed");
            if (power != IntPtr.Zero)
                *(int*)(power + GameInterop.Offset(ref targetTimeRateOffset, TimeRatePower, "<TargetTimeRate>k__BackingField")) = value;
        }
    }
}
