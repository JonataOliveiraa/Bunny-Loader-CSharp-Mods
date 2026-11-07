using System;
using System.Collections.Generic;
using System.Text;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.IO;
using Terraria.ModLoader;
using Terraria.UI;
using topdownmode.Common.Utils;
using topdownmode.Content.Tiles;
using topdownmode.Content.Walls;

namespace topdownmode.Common.Systems;

// The cave is the same world file rebuilt in memory: the surface is saved first, the world is
// cleared and a cave is written over it, and saving is blocked while inside so the file on disk
// stays the surface. Coming back loads the file again.
public static class TopDownCave
{
    public const int CaveW = 320, CaveH = 320, Border = 6;
    const double Fill = 0.48;
    const double OreDensity = 1;
    const int SmoothMs = 30, NeedStable = 45, MinHold = 90, MaxHold = 900;

    enum Phase { Idle, Leaving, Menu, Entering, Loading, Arrive, FadeOut, Swapping, FadeIn }

    public static bool InCave;
    public static double Alpha;
    public static bool TargetIsCave;

    // Leaving through the title screen instead of swapping in place. The JavaScript version kept
    // this as a console switch; it stays off by default.
    public static bool ViaMenu;

    static bool on = true, hooked, swapping, guardInternal, guardSavePlayer, tickA, tickB;
    static int guardSaveWorld;
    static Phase phase = Phase.Idle;
    static bool hasTarget;
    static int fade, wait, arriveTicks, blocked, errors, hold, stable;
    static long lastNow;
    static string pathPlayer = "", pathWorld = "", lastErr = "";
    static Point? ret;
    static Vector2? arrival;
    static readonly Random random = new();

    struct OreKind
    {
        public int A, B, From, To, Per, Size;
    }

    public static bool Swapping => swapping;

    static void Say(string message) => TopDownMode.Log("cave: " + message);

    static void Tell(string message) => Main.NewText(message, 255, 231, 176);

    static void Fail(string where, Exception e)
    {
        errors++;
        string text = e.ToString();
        lastErr = where + ": " + (text.Length > 200 ? text[..200] : text);
        Say("error in " + lastErr);
    }

    static string PathOf(WorldFileData d) => d.IsNull ? "" : d.Path ?? d._path ?? "";

    static string PathOf(PlayerFileData d) => d.IsNull ? "" : d.Path ?? d._path ?? "";

    static bool GuardActive() => on && InCave;

    static void InstallGuards()
    {
        On_WorldFile.InternalSaveWorld += (orig, useCloudSaving, resetTime) =>
        {
            if (!NoteBlocked())
                orig(useCloudSaving, resetTime);
        };
        guardInternal = true;
        On_WorldFile.SaveWorld_WorldSaveContext += (orig, saveContext) =>
        {
            if (!NoteBlocked())
                orig(saveContext);
        };
        On_WorldFile.SaveWorld_bool_bool_WorldSaveContext += (orig, useCloudSaving, resetTime, saveContext) =>
        {
            if (!NoteBlocked())
                orig(useCloudSaving, resetTime, saveContext);
        };
        guardSaveWorld = 2;
        // Inside the cave the player's map file must not receive the cave.
        On_Player.SavePlayer += (orig, playerFile, skipMapSave, forceSave) =>
            orig(playerFile, GuardActive() || skipMapSave, forceSave);
        guardSavePlayer = true;
        Say("save blocks: InternalSaveWorld=" + guardInternal + " SaveWorld=" + guardSaveWorld + " SavePlayer(map)=" + guardSavePlayer);
    }

    static bool NoteBlocked()
    {
        if (!GuardActive())
            return false;
        blocked++;
        if (blocked == 1 || blocked % 20 == 0)
            Say("world save blocked inside the cave (" + blocked + ")");
        return true;
    }

    static void InstallTicks()
    {
        On_Main.UpdateUIStates += (orig, gameTime) =>
        {
            orig(gameTime);
            Tick();
        };
        tickA = true;
        On_Main.DoUpdate_HandleInput += (orig, self) =>
        {
            orig(self);
            Tick();
        };
        tickB = true;
    }

    static void Tick()
    {
        if (phase != Phase.Menu)
            return;
        try
        {
            MenuTick();
        }
        catch (Exception e)
        {
            Fail("menu tick", e);
            phase = Phase.Idle;
        }
    }

    static PlayerFileData FindPlayer(string path)
    {
        var list = new GameList(Main.PlayerList);
        for (int i = 0; i < list.Count; i++)
        {
            PlayerFileData d = list.Get<PlayerFileData>(i);
            if (PathOf(d) == path)
                return d;
        }
        return default;
    }

    static WorldFileData FindWorld(string path)
    {
        var list = new GameList(Main.WorldList);
        for (int i = 0; i < list.Count; i++)
        {
            WorldFileData d = list.Get<WorldFileData>(i);
            if (PathOf(d) == path)
                return d;
        }
        return default;
    }

    static void MenuTick()
    {
        if (!Main.gameMenu)
            return;
        if (wait > 0)
        {
            wait--;
            return;
        }
        phase = Phase.Entering;
        Say("at the title; loading " + (TargetIsCave ? "the cave" : "the surface") + " (world " + pathWorld + ")");
        Main.LoadPlayers(false);
        PlayerFileData pdata = FindPlayer(pathPlayer);
        if (pdata.IsNull)
        {
            Say("character not in the list; pick it in the menu");
            phase = Phase.Idle;
            return;
        }
        Main.SelectPlayer(pdata);
        WorldFileData wdata = FindWorld(pathWorld);
        if (wdata.IsNull)
        {
            Main.LoadWorlds(false);
            wdata = FindWorld(pathWorld);
        }
        if (wdata.IsNull)
        {
            Say("world not in the list; pick it in the menu");
            phase = Phase.Idle;
            return;
        }
        wdata.SetAsActive();
        phase = Phase.Loading;
        arriveTicks = 0;
        Main.menuMode = 10;
        WorldGen.playWorld();
    }

    static bool StartLeave(bool toCave)
    {
        if (!on)
            return false;
        if (phase != Phase.Idle)
            return false;
        // Solo only: the world is swapped in memory, so the host of a phone game is refused as well.
        if (Main.netMode != 0)
        {
            Tell("The cave only works in single player for now.");
            return false;
        }
        if (!guardInternal || !(tickA || tickB))
        {
            Tell("Cave unavailable: the safety hooks were not installed (see the log).");
            return false;
        }
        string pp = PathOf(Main.ActivePlayerFileData), wp = PathOf(Main.ActiveWorldFileData);
        if (pp == "" || wp == "")
        {
            Tell("Could not identify the active character and world.");
            return false;
        }
        pathPlayer = pp;
        pathWorld = wp;
        TargetIsCave = toCave;
        hasTarget = true;
        phase = Phase.Leaving;
        if (!ViaMenu)
        {
            if (toCave && !SaveNow())
            {
                Tell("Could not save the world; going down was canceled.");
                return false;
            }
            TargetIsCave = toCave;
            phase = Phase.FadeOut;
            fade = 0;
            return true;
        }
        Tell(toCave ? "Going down..." : "Going up...");
        try
        {
            WorldGen.SaveAndQuit();
        }
        catch (Exception e)
        {
            Fail("SaveAndQuit", e);
            phase = Phase.Idle;
            return false;
        }
        return true;
    }

    static bool SaveNow()
    {
        bool ok = false;
        try
        {
            WorldFile.SaveWorld(WorldFile.WorldSaveContext.Normal);
            ok = true;
        }
        catch (GameException e)
        {
            Fail("saving the world", e);
        }
        try
        {
            Player.SavePlayer(Main.ActivePlayerFileData, false, false);
        }
        catch (GameException e)
        {
            Fail("saving the player", e);
        }
        Say("surface saved before swapping: " + ok);
        return ok;
    }

    static void RefreshRender()
    {
        var res = new StringBuilder();
        Refresh(res, "Lighting.Clear", Lighting.Clear);
        Refresh(res, "Lighting.Initialize", () => Lighting.Initialize(false));
        Refresh(res, "refreshMap", () => Main.refreshMap = true);
        // TilesRenderer.Reset and sectionManager.SetAllFramedSectionsAsUnframed, which the
        // JavaScript version also tried, do not exist in the phone's game.
        Say("rendering refreshed:" + res);
    }

    static void Refresh(StringBuilder res, string name, Action action)
    {
        try
        {
            action();
            res.Append(' ').Append(name).Append("=ok");
        }
        catch (GameException)
        {
            res.Append(' ').Append(name).Append("=missing");
        }
    }

    static void LoadSurface()
    {
        swapping = true;
        try
        {
            WorldGen.clearWorld();
        }
        finally
        {
            swapping = false;
        }
        WorldFile.LoadWorld(false);
        InCave = false;
        TopDownWorldSystem.InCave = false;
        Main.worldSurface = Main.maxTilesY - 16;
        Main.rockLayer = Main.maxTilesY - 14;
        try
        {
            GameBindings.MapClear();
        }
        catch (Exception e)
        {
            Say("Map.Clear unavailable: " + e.Message);
        }
        try
        {
            GameBindings.MapLoad();
            Say("surface map reloaded");
        }
        catch (Exception e)
        {
            Say("Map.Load unavailable: " + e.Message);
        }
    }

    static void TopLeft(int i, int j, out int x, out int y)
    {
        Tile t = Main.tile[i, j];
        x = i - (t.frameX % 36) / 18;
        y = j - (t.frameY % 36) / 18;
    }

    public static bool Near(int i, int j) => Main.LocalPlayer.IsWithinSnappngRangeToTile(i, j, 72);

    public static void Use(int i, int j, bool down)
    {
        try
        {
            if (!Near(i, j))
            {
                Tell("Get closer.");
                return;
            }
            if (down)
            {
                if (InCave)
                    return;
                TopLeft(i, j, out int tx, out int ty);
                ret = new Point(tx, ty);
                StartLeave(true);
            }
            else if (InCave)
            {
                StartLeave(false);
            }
        }
        catch (Exception e)
        {
            Fail("use", e);
            phase = Phase.Idle;
        }
    }

    public static bool Enter()
    {
        Player p = Main.LocalPlayer;
        ret = new Point((int)Math.Floor(p.position.X / 16.0), (int)Math.Floor(p.position.Y / 16.0) - 2);
        return StartLeave(true);
    }

    public static bool Leave() => InCave && StartLeave(false);

    static void BuildCave()
    {
        long t0 = Environment.TickCount64;
        int WD = Main.maxTilesX, HT = Main.maxTilesY;
        int w = Math.Max(40, Math.Min(CaveW, WD - 40)), h = Math.Max(40, Math.Min(CaveH, HT - 40));
        int x0 = (WD - w) >> 1, y0 = (HT - h) >> 1, B = Math.Max(2, Border);
        int floor = ModContent.WallType<TopDownCaveFloor>();
        int exitType = ModContent.TileType<TopDownCaveExit>();
        if (floor <= 0 || exitType <= 0)
            throw new InvalidOperationException("cave floor or exit not registered (floor=" + floor + " exit=" + exitType + ")");
        const int stone = TileID.Stone;

        InCave = true;
        TopDownWorldSystem.InCave = true;
        string? keepEvil = TopDownWorldSystem.Evil;
        swapping = true;
        try
        {
            WorldGen.clearWorld();
        }
        finally
        {
            swapping = false;
        }
        TopDownWorldSystem.IsTopDown = true;
        TopDownWorldSystem.DeepHidden = true;
        TopDownWorldSystem.DeepOpen = true;
        TopDownWorldSystem.InCave = true;
        TopDownWorldSystem.Evil = keepEvil;

        uint seed = (uint)(random.Next(int.MaxValue) ^ (int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        double Rnd()
        {
            seed += 0x6D2B79F5;
            uint t = seed;
            t = (t ^ (t >> 15)) * (t | 1);
            t ^= t + (t ^ (t >> 7)) * (t | 61);
            return (t ^ (t >> 14)) / 4294967296.0;
        }

        int cxl = w >> 1, cyl = h >> 1;
        byte[] g = new byte[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
                g[y * w + x] = (byte)((x < B || y < B || x >= w - B || y >= h - B || Rnd() < Fill) ? 1 : 0);
        }
        for (int it = 0; it < 5; it++)
        {
            byte[] n = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (x < B || y < B || x >= w - B || y >= h - B)
                    {
                        n[y * w + x] = 1;
                        continue;
                    }
                    int c = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            if (g[(y + dy) * w + x + dx] != 0)
                                c++;
                        }
                    }
                    n[y * w + x] = (byte)(c >= 5 ? 1 : 0);
                }
            }
            g = n;
        }
        const int R = 8;
        for (int y = -R; y <= R; y++)
        {
            for (int x = -R; x <= R; x++)
            {
                if (x * x + y * y <= R * R)
                    g[(cyl + y) * w + cxl + x] = 0;
            }
        }
        byte[] seen = new byte[w * h];
        var stack = new Stack<int>();
        stack.Push(cyl * w + cxl);
        seen[cyl * w + cxl] = 1;
        Span<int> neighbors = stackalloc int[4];
        while (stack.Count > 0)
        {
            int p = stack.Pop(), px = p % w;
            neighbors[0] = p - 1;
            neighbors[1] = p + 1;
            neighbors[2] = p - w;
            neighbors[3] = p + w;
            foreach (int q in neighbors)
            {
                if (q < 0 || q >= w * h || seen[q] != 0 || g[q] != 0)
                    continue;
                int qx = q % w;
                if (Math.Abs(qx - px) > 1)
                    continue;
                seen[q] = 1;
                stack.Push(q);
            }
        }
        int open = 0;
        for (int i = 0; i < w * h; i++)
        {
            if (g[i] == 0 && seen[i] == 0)
                g[i] = 1;
            else if (g[i] == 0)
                open++;
        }

        OreKind[] ores =
        {
            new() { A = TileID.Copper, B = TileID.Tin, From = 10, To = 9999, Per = 26, Size = 14 },
            new() { A = TileID.Iron, B = TileID.Lead, From = 45, To = 9999, Per = 20, Size = 12 },
            new() { A = TileID.Silver, B = TileID.Tungsten, From = 90, To = 9999, Per = 13, Size = 10 },
            new() { A = TileID.Gold, B = TileID.Platinum, From = 140, To = 9999, Per = 8, Size = 9 },
        };
        ushort[] ore = new ushort[w * h];
        var oreCount = new Dictionary<int, int>();
        int area = (w - 2 * B) * (h - 2 * B);
        foreach (OreKind o in ores)
        {
            int veins = (int)Math.Floor(area / 10000.0 * o.Per * OreDensity + 0.5);
            for (int v = 0, tries = 0; v < veins && tries < veins * 40; tries++)
            {
                int x = B + 1 + (int)Math.Floor(Rnd() * (w - 2 * B - 2)), y = B + 1 + (int)Math.Floor(Rnd() * (h - 2 * B - 2));
                double d = Math.Sqrt((double)(x - cxl) * (x - cxl) + (double)(y - cyl) * (y - cyl));
                if (d < o.From || d > o.To || g[y * w + x] == 0 || ore[y * w + x] != 0)
                    continue;
                int kind = Rnd() < 0.5 ? o.A : o.B;
                int px = x, py = y, placed = 0;
                for (int k = 0; k < o.Size * 3 && placed < o.Size; k++)
                {
                    int i = py * w + px;
                    if (g[i] != 0 && ore[i] == 0 && px >= B && py >= B && px < w - B && py < h - B)
                    {
                        ore[i] = (ushort)kind;
                        placed++;
                    }
                    double r = Rnd();
                    if (r < 0.25) px++;
                    else if (r < 0.5) px--;
                    else if (r < 0.75) py++;
                    else py--;
                    if (px < B || py < B || px >= w - B || py >= h - B)
                    {
                        px = x;
                        py = y;
                    }
                }
                if (placed != 0)
                {
                    v++;
                    oreCount.TryGetValue(kind, out int total);
                    oreCount[kind] = total + placed;
                }
            }
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                Tile t = Main.tile[x0 + x, y0 + y];
                t.wall = (ushort)floor;
                int i = y * w + x;
                if (g[i] != 0)
                {
                    t.sTileHeader = TileScan.ActiveBit;
                    t.type = ore[i] != 0 ? ore[i] : (ushort)stone;
                }
            }
        }
        Say("relief: " + open + " open tiles of " + w * h + "; ores " + string.Join(", ", oreCount));
        try
        {
            for (int x = x0 - 1; x <= x0 + w + 1; x += 80)
                WorldGen.RangeFrame(x, y0 - 1, Math.Min(x + 80, x0 + w + 1), y0 + h + 1);
        }
        catch (GameException e)
        {
            Fail("RangeFrame", e);
        }

        int cx = x0 + (w >> 1), cy = y0 + (h >> 1);
        int ex = cx - 1, ey = cy - 2;
        for (int dy = 0; dy < 2; dy++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                Tile t = Main.tile[ex + dx, ey + dy];
                t.sTileHeader = TileScan.ActiveBit;
                t.type = (ushort)exitType;
                t.frameX = (short)(dx * 18);
                t.frameY = (short)(dy * 18);
            }
        }

        Main.worldSurface = 20;
        Main.rockLayer = 30;
        Main.spawnTileX = cx;
        Main.spawnTileY = ey + 4;
        arrival = new Vector2(cx * 16 + 8, (ey + 4) * 16);
        Say("cave written: " + w + "x" + h + " tiles at (" + x0 + "," + y0 + "), exit at " + ex + "," + ey + " (" + (Environment.TickCount64 - t0) + " ms)");
    }

    static void PlacePlayer(double px, double py)
    {
        Player p = Main.LocalPlayer;
        p.position.X = (float)(px - p.width / 2.0);
        p.position.Y = (float)(py - p.height);
        p.velocity.X = 0;
        p.velocity.Y = 0;
        p.fallStart = (int)Math.Floor(p.position.Y / 16.0);
    }

    static void Arrive()
    {
        phase = Phase.Idle;
        try
        {
            if (hasTarget && TargetIsCave && arrival != null)
            {
                PlacePlayer(arrival.Value.X, arrival.Value.Y);
                if (guardSavePlayer)
                {
                    try
                    {
                        GameBindings.MapClear();
                        Main.refreshMap = true;
                        Say("map cleared for the cave");
                    }
                    catch (Exception e)
                    {
                        Fail("Map.Clear", e);
                    }
                }
                else
                {
                    Say("map NOT cleared (the player's map is not protected)");
                }
                Tell("You went down into the cave. The stairs going up glow at the center.");
            }
            else if (hasTarget && !TargetIsCave)
            {
                if (ret != null)
                    PlacePlayer((ret.Value.X + 1) * 16, (ret.Value.Y + 4) * 16);
                Tell("You are back on the surface.");
            }
        }
        catch (Exception e)
        {
            Fail("arrive", e);
        }
    }

    public static string Status() =>
        "cave: active=" + on + " phase=" + phase + " target=" + (TargetIsCave ? "cave" : "surface") + " inCave=" + InCave
        + " blocks=InternalSaveWorld:" + guardInternal + "/SaveWorld:" + guardSaveWorld + "/SavePlayer:" + guardSavePlayer + " ticks=" + tickA + "/" + tickB
        + " saves blocked=" + blocked + " errors=" + errors + (lastErr != "" ? " last: " + lastErr : "");

    public static void InstallCave()
    {
        on = true;
        if (hooked)
            return;
        hooked = true;
        try
        {
            InstallGuards();
        }
        catch (Exception e)
        {
            Fail("InstallGuards", e);
        }
        try
        {
            InstallTicks();
        }
        catch (Exception e)
        {
            Fail("InstallTicks", e);
        }
    }

    public static void UninstallCave() => on = false;

    internal static void OnPostWorldLoad()
    {
        if (phase != Phase.Loading)
            return;
        try
        {
            string here = PathOf(Main.ActiveWorldFileData);
            if (pathWorld != "" && here != "" && here != pathWorld)
            {
                Say("the loaded world is not the expected one; nothing was done");
                phase = Phase.Idle;
                return;
            }
            if (TargetIsCave)
                BuildCave();
            phase = Phase.Arrive;
            arriveTicks = 0;
        }
        catch (Exception e)
        {
            Fail("buildCave", e);
            phase = Phase.Arrive;
            arriveTicks = 0;
            Say("the cave failed: saving stays blocked; leave to the title (the surface world on disk is intact)");
        }
    }

    internal static void OnPostUpdateEverything()
    {
        if (Main.gameMenu)
            return;
        if (phase == Phase.FadeOut)
        {
            fade++;
            Alpha = Math.Min(1, fade / 20.0);
            Player p = Main.LocalPlayer;
            p.velocity.X = 0;
            p.velocity.Y = 0;
            if (fade >= 24)
            {
                phase = Phase.Swapping;
                try
                {
                    if (TargetIsCave)
                        BuildCave();
                    else
                        LoadSurface();
                    RefreshRender();
                    Arrive();
                }
                catch (Exception e)
                {
                    Fail("direct swap", e);
                    Say("the swap failed; saving stays blocked if you are in the cave. Leave through the menu: the surface file is intact.");
                }
                phase = Phase.FadeIn;
                fade = 0;
                hold = 0;
                stable = 0;
                lastNow = 0;
            }
            return;
        }
        if (phase == Phase.FadeIn)
        {
            long now = Environment.TickCount64, dt = lastNow != 0 ? now - lastNow : 999;
            lastNow = now;
            if (fade < 0)
                fade = 0;
            hold++;
            stable = dt < SmoothMs ? stable + 1 : 0;
            bool ready = (hold >= MinHold && stable >= NeedStable) || hold >= MaxHold;
            if (!ready)
            {
                Alpha = 1;
                return;
            }
            fade++;
            Alpha = Math.Max(0, 1 - fade / 25.0);
            if (fade >= 25)
            {
                Alpha = 0;
                phase = Phase.Idle;
                hold = 0;
                stable = 0;
                lastNow = 0;
                Say("transition done");
            }
            return;
        }
        if (phase != Phase.Arrive)
            return;
        if (++arriveTicks < 5)
            return;
        Arrive();
    }

    internal static void OnClearWorld()
    {
        if (swapping)
            return;
        InCave = false;
        TopDownWorldSystem.InCave = false;
    }

    internal static void OnWorldUnload()
    {
        InCave = false;
        TopDownWorldSystem.InCave = false;
        if (phase == Phase.Leaving)
        {
            phase = Phase.Menu;
            wait = 30;
            Say("world unloaded; back in the game in a moment");
        }
    }
}

public class TopDownCaveSystem : ModSystem
{
    UserInterface? ui;
    UIPanel? panel;
    UIText? text;

    public override void PostWorldLoad() => TopDownCave.OnPostWorldLoad();

    public override void PostUpdateEverything() => TopDownCave.OnPostUpdateEverything();

    public override void PostSetupContent()
    {
        var state = new UIState();
        panel = new UIPanel();
        panel.Width.Set(0, 1);
        panel.Height.Set(0, 1);
        panel.BorderColor = new Color(0, 0, 0, 0);
        text = new UIText("", 1.1f);
        text.HAlign = 0.5f;
        text.VAlign = 0.5f;
        panel.Append(text);
        state.Append(panel);
        state.Activate();
        ui = new UserInterface();
        ui.SetState(state);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (panel == null || text == null || ui == null)
            return;
        double alpha = TopDownCave.Alpha;
        if (alpha <= 0)
            return;
        int a = Math.Max(0, Math.Min(255, (int)Math.Floor(alpha * 255 + 0.5)));
        panel.BackgroundColor = new Color(0, 0, 0, a);
        text.SetText(alpha > 0.6 ? (TopDownCave.TargetIsCave ? "Going down into the cave..." : "Back to the surface...") : "");
        text.TextColor = new Color(235, 225, 200, a);
        ui.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (panel == null || !(TopDownCave.Alpha > 0))
            return;
        int i = layers.FindIndex(l => l.Name == "Vanilla: Mouse Text");
        if (i == -1)
            return;
        layers.Insert(i, new LegacyGameInterfaceLayer("TopDown: Cave", () =>
        {
            if (TopDownCave.Alpha > 0)
                ui!.Draw(Main.spriteBatch, default);
            return true;
        }, InterfaceScaleType.UI));
    }

    public override void ClearWorld() => TopDownCave.OnClearWorld();

    public override void OnWorldUnload() => TopDownCave.OnWorldUnload();
}
