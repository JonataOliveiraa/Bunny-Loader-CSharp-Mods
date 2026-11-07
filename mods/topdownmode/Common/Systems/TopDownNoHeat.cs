using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.Graphics.Effects;
using topdownmode.Common.Configs;

namespace topdownmode.Common.Systems;

// The Underworld's heat filter makes no sense seen from above; it is switched off as it turns on.
public static class TopDownNoHeat
{
    const string Key = "HeatDistortion";

    static bool enabled = true, hooked, fallbackTried;
    static int calls, seenActive, hits, errors;
    static Filter filter;
    static long lastAt;
    static string lastErr = "";
    static readonly HashSet<string> logged = new();

    static void LogOnce(string key, string message)
    {
        if (logged.Add(key))
            TopDownMode.Log("Underworld heat: " + message);
    }

    static bool Applies()
    {
        if (!enabled || Main.gameMenu)
            return false;
        if (!TopDownWorldSystem.IsTopDown)
            return false;
        return TopDownConfig.Instance.HideHellHeat;
    }

    static bool Deactivate(FilterManager scene, Filter f)
    {
        try
        {
            scene.Deactivate(Key, Array.Empty<object>());
            if (!f.IsActive())
                return true;
            lastErr = "Deactivate left the filter active";
        }
        catch (Exception e)
        {
            lastErr = "Deactivate: " + e.Message;
        }
        return false;
    }

    public static void SuppressHeat()
    {
        try
        {
            if (!Applies())
                return;
            calls++;
            FilterManager scene = Filters.Scene;
            if (scene.IsNull)
            {
                LogOnce("nosc", "Filters.Scene unavailable");
                return;
            }
            if (filter.IsNull)
                filter = scene[Key];
            Filter f = filter;
            if (f.IsNull)
            {
                LogOnce("nofilter", "filter " + Key + " not found");
                return;
            }
            if (!f.IsActive())
                return;
            seenActive++;
            if (seenActive == 1)
                LogOnce("seen", "active filter detected; turning it off");
            if (Deactivate(scene, f))
            {
                hits++;
            }
            else if (seenActive > 120 && hits == 0 && !fallbackTried)
            {
                fallbackTried = true;
                Main.UseHeatDistortion = false;
                LogOnce("fb", "Deactivate had no effect; set Main.UseHeatDistortion = false");
            }
        }
        catch (Exception e)
        {
            errors++;
            lastErr = e.Message;
            if (errors <= 3)
                TopDownMode.Log("Underworld heat (error): " + e);
            if (errors >= 10)
            {
                enabled = false;
                TopDownMode.Log("Underworld heat: off after repeated errors.");
            }
        }
    }

    static void Throttled()
    {
        long now = Environment.TickCount64;
        if (now - lastAt < 4)
            return;
        lastAt = now;
        SuppressHeat();
    }

    public static void InstallNoHeat()
    {
        enabled = true;
        if (hooked)
            return;
        hooked = true;
        On_FilterManager.CanCapture += (orig, self) =>
        {
            Throttled();
            return orig(self);
        };
        On_FilterManager.HasActiveFilter += (orig, self) =>
        {
            Throttled();
            return orig(self);
        };
        On_Main.DoDraw_UpdateCameraPosition += orig =>
        {
            Throttled();
            orig();
        };
        TopDownMode.Log("Underworld heat: hooks installed");
    }

    public static void UninstallNoHeat() => enabled = false;

    public static string Status() =>
        "Underworld heat: active=" + enabled + " calls=" + calls + " active filter seen=" + seenActive + " turned off=" + hits + " errors=" + errors + (lastErr != "" ? " last error: " + lastErr : "");
}
