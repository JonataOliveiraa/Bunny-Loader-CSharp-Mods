using System;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace WorldGenPreviewerMod.Common.Map;

public struct WorldArea
{
    public float x, y, w, h;
}

public class FullMap
{
    const int SECTION = 256;

    GUIMap gui;
    // The game's garbage collector does not see C# memory: this keeps the GUIMap alive.
    GameHandle? handle;
    int width, height, cols, rows;
    int[]? top, bottom;
    int column, zoomColumn = -1;
    bool dirty;
    int dirtyX0, dirtyX1, dirtyY0, dirtyY1;

    public int sweep;
    public int finalLeft;

    public bool Ready => handle != null;

    public static void WithBounds(Action action)
    {
        int minX = Main.mapMinX, maxX = Main.mapMaxX, minY = Main.mapMinY, maxY = Main.mapMaxY;
        bool clear = Main.clearMap;
        try
        {
            action();
        }
        finally
        {
            Main.mapMinX = minX;
            Main.mapMaxX = maxX;
            Main.mapMinY = minY;
            Main.mapMaxY = maxY;
            Main.clearMap = clear;
        }
    }

    public void Open(int w, int h)
    {
        Close();
        width = w;
        height = h;
        cols = (w + SECTION - 1) / SECTION;
        rows = (h + SECTION - 1) / SECTION;
        gui = GUIMap.New();
        handle = new GameHandle(gui.Pointer);
        gui.activeX = cols;
        gui.activeY = rows;
        gui.InitialiseTargets();
        WithBounds(() => gui.ClearMap());
        top = new int[w];
        bottom = new int[w];
        Array.Fill(top, -1);
        Array.Fill(bottom, -1);
        column = 0;
        sweep = 0;
        zoomColumn = -1;
        dirty = false;
    }

    public void Close()
    {
        if (handle != null)
        {
            gui.CleanupMapTargets();
            handle.Dispose();
        }
        handle = null;
        gui = default;
        top = bottom = null;
    }

    public void BeginFinal()
    {
        finalLeft = width;
        zoomColumn = -1;
    }

    public int Scan => zoomColumn >= 0 ? zoomColumn : column;

    public void Sample(double deadline, WorldArea? view)
    {
        if (!Ready)
            return;
        if (finalLeft > 0)
            view = null;
        int rx0 = 0, rx1 = width, ry0 = 0, ry1 = height;
        if (view is WorldArea v)
        {
            rx0 = Math.Max(0, Math.Min(width - 1, (int)Math.Floor(v.x)));
            rx1 = Math.Max(0, Math.Min(width, (int)Math.Ceiling(v.x + v.w)));
            ry0 = Math.Max(0, Math.Min(height - 1, (int)Math.Floor(v.y)));
            ry1 = Math.Max(0, Math.Min(height, (int)Math.Ceiling(v.y + v.h)));
        }
        if (rx1 <= rx0 || ry1 <= ry0)
            return;
        if (view == null)
            zoomColumn = -1;
        while (Util.NowMs < deadline)
        {
            int x;
            if (view != null)
            {
                if (zoomColumn < rx0 || zoomColumn >= rx1)
                    zoomColumn = rx0;
                x = zoomColumn++;
                if (zoomColumn >= rx1)
                    zoomColumn = rx0;
            }
            else
            {
                if (finalLeft > 0 && --finalLeft == 0)
                    deadline = 0;
                x = column++;
                if (column >= width)
                {
                    column = 0;
                    sweep++;
                }
            }
            GameBindings.UpdateColumn(x, ry0, ry1);
            if (top![x] < 0 || ry0 < top[x])
                top[x] = ry0;
            if (ry1 > bottom![x])
                bottom[x] = ry1;
            if (!dirty)
            {
                dirty = true;
                dirtyX0 = x;
                dirtyX1 = x + 1;
                dirtyY0 = ry0;
                dirtyY1 = ry1;
            }
            else
            {
                dirtyX0 = Math.Min(dirtyX0, x);
                dirtyX1 = Math.Max(dirtyX1, x + 1);
                dirtyY0 = Math.Min(dirtyY0, ry0);
                dirtyY1 = Math.Max(dirtyY1, ry1);
            }
        }
    }

    public void Upload()
    {
        if (!Ready || !dirty)
            return;
        dirty = false;
        gui.ApplySection(dirtyX0, dirtyX1, dirtyY0, dirtyY1);
        gui.ApplyMap();
    }

    public void Draw(SpriteBatch spriteBatch, float ox, float oy, float scale, float viewX, float viewY, float viewWidth, float viewHeight)
    {
        if (!Ready)
            return;
        GameArray<Texture2D> targets = gui.mapTarget;
        int x0 = Math.Max(0, (int)Math.Floor((viewX - ox) / scale));
        int x1 = Math.Min(width, (int)Math.Ceiling((viewX + viewWidth - ox) / scale));
        int y0 = Math.Max(0, (int)Math.Floor((viewY - oy) / scale));
        int y1 = Math.Min(height, (int)Math.Ceiling((viewY + viewHeight - oy) / scale));
        int[] tops = top!, bottoms = bottom!;
        for (int sy = y0 / SECTION; sy * SECTION < y1; sy++)
        {
            int rowTop = sy * SECTION, rowEnd = Math.Min(height, rowTop + SECTION);
            for (int sx = x0 / SECTION; sx * SECTION < x1; sx++)
            {
                Texture2D texture = targets[sy * cols + sx];
                int left = sx * SECTION, right = Math.Min(width, left + SECTION);
                int x = Math.Max(left, x0);
                int stop = Math.Min(right, x1);
                while (x < stop)
                {
                    int a = Math.Max(rowTop, tops[x]), b = Math.Min(rowEnd, bottoms[x]);
                    int run = x + 1;
                    while (run < stop && tops[run] == tops[x] && bottoms[run] == bottoms[x])
                        run++;
                    if (tops[x] >= 0 && b > a)
                    {
                        int dx = Util.Round(ox + x * scale), dy = Util.Round(oy + a * scale);
                        spriteBatch.Draw(texture, new Rectangle(dx, dy, Util.Round(ox + run * scale) - dx, Util.Round(oy + b * scale) - dy),
                            new GameNullable<Rectangle>(new Rectangle(x - left, a - rowTop, run - x, b - a)), Color.White);
                    }
                    x = run;
                }
            }
        }
    }
}
