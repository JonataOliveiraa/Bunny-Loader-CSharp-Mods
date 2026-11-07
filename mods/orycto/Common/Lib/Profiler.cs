using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OryctoMod.Common.Configs;
using Terraria;
using Terraria.GameContent;

namespace OryctoMod.Common.Lib;

public static class Profiler
{
    const int RefreshMs = 500;
    const int DumpMs = 15000;
    const int MaxRows = 9;

    sealed class Timing
    {
        public double Ms;
        public int Calls;
    }

    readonly record struct Row(string Name, double PerFrame, double Calls);

    static readonly Dictionary<string, Timing> Timings = new();
    static List<Row> rows = new();
    static int drawn, lastWorlds;
    static long since = Environment.TickCount64, lastDump = Environment.TickCount64;
    static string fpsText = "";

    static string Fixed(double value, int digits) => value.ToString("F" + digits, CultureInfo.InvariantCulture);

    static void Record(string name, long start)
    {
        if (!Timings.TryGetValue(name, out Timing? timing))
            Timings[name] = timing = new Timing();
        timing.Ms += (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        timing.Calls++;
    }

    // Timing hooks are only installed when the option is on as the game starts, as in the
    // JavaScript version, so they cost nothing otherwise.
    public static void Install()
    {
        if (!BoostConfig.Instance.Profiler)
            return;

        On_Main.DoDraw += (orig, self, gameTime) => { long t = Stopwatch.GetTimestamp(); try { orig(self, gameTime); } finally { Record("DoDraw", t); } };
        On_Main.DoUpdateInWorld += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DoUpdateInWorld", t); } };
        On_Main.DrawInterface += (orig, self, gameTime) => { long t = Stopwatch.GetTimestamp(); try { orig(self, gameTime); } finally { Record("DrawInterface", t); } };
        On_Main.DrawNPCs += (orig, self, behindTiles) => { long t = Stopwatch.GetTimestamp(); try { orig(self, behindTiles); } finally { Record("DrawNPCs", t); } };
        On_Main.DrawProjectiles += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawProjectiles", t); } };
        On_Main.DrawDust += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawDust", t); } };
        On_Main.DrawGore += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawGore", t); } };
        On_Main.DrawRain += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawRain", t); } };
        On_Main.DrawItems += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawItems", t); } };
        On_Main.DrawBG += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawBG", t); } };
        On_Main.DrawSurfaceBG += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawSurfaceBG", t); } };
        On_Main.DrawWalls += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawWalls", t); } };
        On_Main.DrawTiles += (orig, self, solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride) => { long t = Stopwatch.GetTimestamp(); try { orig(self, solidLayer, forRenderTargets, intoRenderTargets, waterStyleOverride); } finally { Record("DrawTiles", t); } };
        On_Main.DrawWires += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DrawWires", t); } };
        On_Main.DrawLiquid += (orig, self, bg, waterStyle, alpha, waterOnly) => { long t = Stopwatch.GetTimestamp(); try { orig(self, bg, waterStyle, alpha, waterOnly); } finally { Record("DrawLiquid", t); } };
        On_Main.DrawMenu += (orig, self, gameTime) => { long t = Stopwatch.GetTimestamp(); try { orig(self, gameTime); } finally { Record("DrawMenu", t); } };
        On_Main.DoDraw_WallsAndBlacks += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DoDraw_WallsAndBlacks", t); } };
        On_Main.DoDraw_Tiles_NonSolid += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DoDraw_Tiles_NonSolid", t); } };
        On_Main.DoDraw_Tiles_Solid += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DoDraw_Tiles_Solid", t); } };
        On_Main.DoDraw_DrawNPCsOverTiles += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DoDraw_DrawNPCsOverTiles", t); } };
        On_Main.DoDraw_DrawNPCsBehindTiles += (orig, self) => { long t = Stopwatch.GetTimestamp(); try { orig(self); } finally { Record("DoDraw_DrawNPCsBehindTiles", t); } };
        On_Lighting.LightTiles += (orig, firstX, lastX, firstY, lastY) => { long t = Stopwatch.GetTimestamp(); try { orig(firstX, lastX, firstY, lastY); } finally { Record("Lighting.LightTiles", t); } };

        BoostLog.Info("profiler timing on");
    }

    // Once per drawn frame: counts and, twice a second, turns the timings into the rows shown.
    public static void Frame()
    {
        drawn++;
        long now = Environment.TickCount64;
        if (now - since < RefreshMs)
            return;
        double seconds = (now - since) / 1000.0;
        int worldTicks = FrameLoop.WorldTicks;
        fpsText = Math.Round(drawn / seconds, MidpointRounding.AwayFromZero) + " draws/s  " + Math.Round((worldTicks - lastWorlds) / seconds, MidpointRounding.AwayFromZero) + " world/s";
        lastWorlds = worldTicks;
        int frames = Math.Max(1, drawn);
        rows = Timings
            .Select(entry => new Row(entry.Key, entry.Value.Ms / frames, (double)entry.Value.Calls / frames))
            .OrderByDescending(row => row.PerFrame)
            .Take(MaxRows)
            .ToList();
        Timings.Clear();
        drawn = 0;
        since = now;
        if (now - lastDump > DumpMs)
        {
            lastDump = now;
            BoostLog.Info("profile [" + fpsText + "]: " + string.Join(", ", rows.Select(row => row.Name + " " + Fixed(row.PerFrame, 2) + "ms")));
        }
    }

    public static void Draw()
    {
        if (!BoostConfig.Instance.Profiler)
            return;
        const int x = 12, y0 = 150, line = 17;

        SpriteBatch batch = Main.spriteBatch;
        SpriteFont font = FontAssets.MouseText.Value;
        Color white = new(235, 238, 245, 255), black = new(0, 0, 0, 255);
        Color amber = new(250, 200, 70, 255), red = new(240, 84, 84, 255);

        batch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x - 6, y0 - 6, 330, line * (rows.Count + 2) + 10), new Color(0, 0, 0, 150));
        Utils.DrawBorderStringFourWay(batch, font, fpsText.Length > 0 ? fpsText : "measuring...", x, y0, amber, black, Vector2.Zero, 0.8f);
        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            Color hot = row.PerFrame > 3 ? red : row.PerFrame > 1 ? amber : white;
            string text = row.Name + "  " + Fixed(row.PerFrame, 2) + " ms  x" + Fixed(row.Calls, 1);
            Utils.DrawBorderStringFourWay(batch, font, text, x, y0 + (i + 1) * line, hot, black, Vector2.Zero, 0.75f);
        }
    }
}
