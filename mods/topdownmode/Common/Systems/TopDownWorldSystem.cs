using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Generation;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.WorldBuilding;
using topdownmode.Common.Configs;
using topdownmode.Common.Players;

namespace topdownmode.Common.Systems;

public class TopDownWorldSystem : ModSystem
{
    static readonly HashSet<string> KeepVanilla = new() { "Reset", "Guide" };

    public static bool IsTopDown, DeepHidden, DeepOpen, InCave;

    // "Crimson", "Corrupt" or null.
    public static string? Evil;

    static bool lightBroken;

    // Config floats are read back as the decimal the slider shows, so generation sees the same
    // number the JavaScript version did (1.6, not 1.60000002).
    static double Exact(float value) => Math.Round(value, 4);

    public static TopDownWorldGen.GenOpts GenOpts()
    {
        TopDownConfig c = TopDownConfig.Instance;
        return new TopDownWorldGen.GenOpts
        {
            SoftEdges = c.SoftEdges,
            TreeDensity = c.TreeDensity > 0 ? Exact(c.TreeDensity) : 1.6,
            Decor = c.Decor,
            DecorDensity = c.DecorDensity > 0 ? Exact(c.DecorDensity) : 1,
            Evil = c.EvilBiome,
            EvilSize = c.EvilSize > 0 ? Exact(c.EvilSize) : 1,
            Rivers = c.Rivers,
            RiverDensity = c.RiverDensity > 0 ? Exact(c.RiverDensity) : 1,
            RiverWidth = c.RiverWidth > 0 ? Exact(c.RiverWidth) : 1,
            Lakes = c.Lakes,
            Cactus = c.Cactus,
            CactusDensity = c.CactusDensity > 0 ? Exact(c.CactusDensity) : 1,
        };
    }

    static bool SoftOn() => TopDownConfig.Instance.SoftEdges;

    public static TopDownWorldGen.Point2 Build(int? seed)
    {
        TopDownWorldGen.GenResult r = TopDownWorldGen.GenerateTerrain(default, seed ?? TopDownWorldGen.SeedFromWorld(), GenOpts());
        IsTopDown = true;
        Evil = r.Evil;
        DeepHidden = true;
        DeepOpen = true;
        TopDownWorldGen.ApplyFloorLight(r.Palette, SoftOn());
        return r.Spawn;
    }

    public static string Decorate(double? density)
    {
        TopDownWorldGen.DecorResult r = TopDownWorldGen.DecorateOpenWorld(density ?? GenOpts().DecorDensity);
        return "decoration redone: " + r.Rocks + " rocks, " + r.Plants + " tufts. Save the world.";
    }

    public static string Undecorate()
    {
        TopDownWorldGen.DecorResult r = TopDownWorldGen.UndecorateOpenWorld();
        return "decoration removed: " + r.Cleared + " pieces. Save the world.";
    }

    public static string AddTrees(double? density)
    {
        TopDownWorldGen.TreeResult r = TopDownWorldGen.AddTreesOpenWorld(density ?? GenOpts().TreeDensity);
        return "new trees: " + r.Ok + "/" + r.Tried + ". Save the world.";
    }

    public static string Blend()
    {
        TopDownWorldGen.BlendResult r = TopDownWorldGen.BlendOpenWorld(true);
        return "edges redone: " + r.Written + " tiles (old blends erased: " + r.Resets + "). Save the world.";
    }

    public static string Unblend()
    {
        TopDownWorldGen.BlendResult r = TopDownWorldGen.BlendOpenWorld(false);
        return "edges removed: " + r.Resets + " tiles back to the plain floor. Save the world.";
    }

    public static void HideDeep()
    {
        if (TopDownWorldGen.HideDeepOpenWorld().Failed == 0)
            DeepHidden = true;
    }

    public static void OpenDeep()
    {
        if (TopDownWorldGen.OpenDeepOpenWorld().Failed == 0)
            DeepOpen = true;
    }

    static void SyncMechanics()
    {
        if (TopDownConfig.Instance.OnlyInTopDownWorlds)
            TopDownPlayer.On = IsTopDown;
    }

    static void RaiseColor(ref Color color, double fr, double fg, double fb)
    {
        if (lightBroken)
            return;
        try
        {
            double r = Math.Max(color.R, fr), g = Math.Max(color.G, fg), b = Math.Max(color.B, fb);
            if (r == color.R && g == color.G && b == color.B)
                return;
            color = new Color((int)r, (int)g, (int)b);
        }
        catch (Exception e)
        {
            lightBroken = true;
            TopDownMode.Log("minimum light off (error): " + e);
        }
    }

    public static double Daylight()
    {
        if (InCave)
            return 0;
        if (!Main.dayTime)
            return 0;
        double t = Main.time;
        const double ramp = 3600;
        double f = Math.Max(0, Math.Min(Math.Min(1, t / ramp), (54000 - t) / ramp));
        return f * f * (3 - 2 * f);
    }

    public override void ModifySunLightColor(ref Color tileColor, ref Color backgroundColor)
    {
        if (!IsTopDown || InCave)
            return;
        TopDownConfig c = TopDownConfig.Instance;
        double day = c.AmbientLight;
        if (!c.DayNight)
        {
            if (day > 0)
                RaiseColor(ref tileColor, day, day, day);
            return;
        }
        double night = c.NightLight;
        double f = Daylight();
        double fr = night * 0.6 + (day - night * 0.6) * f;
        double fg = night * 0.78 + (day - night * 0.78) * f;
        double fb = night + (day - night) * f;
        if (fr > 0 || fg > 0 || fb > 0)
            RaiseColor(ref tileColor, fr, fg, fb);
    }

    public override void ModifyLightingBrightness(ref float scale)
    {
        if (!IsTopDown)
            return;
        float k = TopDownConfig.Instance.Brightness;
        if (k > 0 && k != 1)
            scale *= k;
    }

    public override void ClearWorld()
    {
        IsTopDown = false;
        DeepHidden = false;
        DeepOpen = false;
    }

    public override void ModifyWorldGenTasks(List<GenPass> tasks)
    {
        TopDownConfig c = TopDownConfig.Instance;
        if (!c.GenerateTopDownWorld)
            return;
        bool skip = c.SkipVanillaWorldGen;
        List<TopDownWorldGen.Stage> stages = TopDownWorldGen.MakeStages(() =>
        {
            TopDownWorldGen.GenOpts o = GenOpts();
            o.Shape = TopDownConfig.Instance.WorldShape;
            o.Trees = TopDownConfig.Instance.Trees;
            o.Fresh = skip;
            return o;
        }, r =>
        {
            Evil = r.Evil;
            IsTopDown = true;
            DeepHidden = true;
            DeepOpen = true;
            TopDownWorldGen.ApplyFloorLight(r.Palette, SoftOn());
        });
        var passes = new List<GenPass>(stages.Count);
        foreach (TopDownWorldGen.Stage st in stages)
            passes.Add(new PassLegacy(st.Name, (progress, configuration) => st.Run(progress), st.Weight));

        if (skip)
        {
            // Vanilla passes reach C# as the loader's own wrapper, so PassLegacy here means a pass
            // a mod added, as it did in JavaScript.
            int before = tasks.Count;
            tasks.RemoveAll(p => !(p is PassLegacy || KeepVanilla.Contains(p.Name)));
            TopDownMode.Log("normal Terraria generation skipped: " + (before - tasks.Count) + " of " + before + " passes removed.");
        }
        int reset = tasks.FindIndex(p => p.Name == "Reset");
        tasks.InsertRange(reset + 1, passes);
        TopDownMode.Log(passes.Count + " generation passes added (from position " + (reset + 1) + " of " + tasks.Count + ").");
    }

    public override void SaveWorldData(TagCompound tag)
    {
        if (!IsTopDown)
            return;
        tag["topDownWorld"] = true;
        if (DeepHidden)
            tag["topDownDeepHidden"] = true;
        if (DeepOpen)
            tag["topDownDeepOpen"] = true;
        if (Evil == "Crimson")
            tag["topDownEvilCrimson"] = true;
        else if (Evil == "Corrupt")
            tag["topDownEvilCorrupt"] = true;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        IsTopDown = tag.ContainsKey("topDownWorld");
        DeepHidden = tag.ContainsKey("topDownDeepHidden");
        DeepOpen = tag.ContainsKey("topDownDeepOpen");
        Evil = tag.ContainsKey("topDownEvilCrimson") ? "Crimson" : tag.ContainsKey("topDownEvilCorrupt") ? "Corrupt" : null;
        TopDownWorldGen.SetEvilFlavor(Evil ?? "Corrupt");
    }

    public override void PostWorldLoad()
    {
        if (IsTopDown)
            TopDownWorldGen.ApplyFloorLight(TopDownWorldGen.BuildPalette(), SoftOn());
        if (IsTopDown && !DeepHidden)
        {
            try
            {
                HideDeep();
            }
            catch (Exception e)
            {
                TopDownMode.Log("deep sea: " + e);
            }
        }
        if (IsTopDown && !DeepOpen)
        {
            try
            {
                OpenDeep();
            }
            catch (Exception e)
            {
                TopDownMode.Log("deep sea (opening): " + e);
            }
        }
        SyncMechanics();
    }

    public override void OnWorldUnload()
    {
        TopDownWorldGen.RestoreFloorLight();
        IsTopDown = false;
    }
}
