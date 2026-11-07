using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Map;
using WorldGenPreviewerMod.Common.Gen;

namespace WorldGenPreviewerMod.Common.Map;

public static class Preview
{
    public static readonly FullMap full = new();
    public static int worldW, worldH;

    static WorldArea? view;
    static bool finalStarted;

    public static bool Ready => full.Ready;

    public static bool Start()
    {
        int w = Main.maxTilesX, h = Main.maxTilesY;
        if (w <= 0 || h <= 0)
            return false;
        if (full.Ready && w == worldW && h == worldH)
            return true;
        GameMapFor(w, h);
        worldW = w;
        worldH = h;
        full.Open(w, h);
        view = null;
        finalStarted = false;
        return true;
    }

    public static void Stop()
    {
        finalStarted = false;
        full.Close();
        worldW = worldH = 0;
        view = null;
    }

    public static void Look(WorldArea? area) => view = area;

    public static void Work(double ms)
    {
        if (!Start())
            return;
        if (State.finishing && !finalStarted)
        {
            finalStarted = true;
            full.BeginFinal();
        }
        full.Sample(Util.NowMs + ms, view);
        full.Upload();
        if (finalStarted && full.finalLeft == 0)
            State.mapFinished = true;
    }

    public static int TypeAt(int wx, int wy)
    {
        if (wx < 0 || wy < 0 || wx >= worldW || wy >= worldH)
            return -1;
        return MapHelper.CreateMapTile(wx, wy, 255).Type;
    }

    static void GameMapFor(int w, int h)
    {
        WorldMap map = Main.Map;
        if (!map.IsNull)
        {
            GameBindings.GetMapSize(map, out int width, out int height);
            if (width >= w && height >= h)
                return;
        }
        FullMap.WithBounds(() =>
        {
            if (map.IsNull)
            {
                map = GameBindings.NewMap();
                Main.Map = map;
            }
            GameBindings.AllocateMap(map, w, h);
        });
    }

    static readonly List<Rectangle> Regular = new(), Protected = new();

    // A copy only when the list grew or shrank: passes only add to it, and Reset empties it.
    public static List<Rectangle> Structures(bool isProtected)
    {
        List<Rectangle> cache = isProtected ? Protected : Regular;
        GameList list = GameBindings.StructureList(isProtected);
        if (list.IsNull)
            return cache;
        int count = list.Count;
        if (count == cache.Count)
            return cache;
        cache.Clear();
        for (int k = 0; k < count; k++)
            cache.Add(list.Get<Rectangle>(k));
        return cache;
    }

    static readonly Dictionary<int, string> Names = new();

    public static string TileName(int type)
    {
        if (type <= 0)
            return "";
        if (!Names.TryGetValue(type, out string? name))
        {
            name = Lang.GetMapObjectName(type) ?? "";
            Names[type] = name;
        }
        return name;
    }
}
