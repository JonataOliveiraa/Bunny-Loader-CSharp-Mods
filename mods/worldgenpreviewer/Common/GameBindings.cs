using System;
using System.Runtime.CompilerServices;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Map;
using Terraria.WorldBuilding;
using WorldGenPreviewerMod.Common.UI;

namespace WorldGenPreviewerMod.Common;

// The menu draw hooks, the menu layout reads and the structure lists. UnityEngine's Input,
// Screen and Touch have no C# proxy, so those are bound by name at run time, the way the
// JavaScript version reached them: their names only fail when the game runs.
public static unsafe class GameBindings
{
    static readonly GameClass InputClass = new("UnityEngine", "Input");
    static readonly GameClass ScreenClass = new("UnityEngine", "Screen");
    static readonly GameClass TouchClass = new("UnityEngine", "Touch");

    static readonly int[] NoParameters = { -1 };
    static readonly int[] OneParameter = { -1, -1 };

    // A UnityEngine.Touch is 68 bytes. An offset from the game counts the boxed struct's header.
    const int TouchBufferSize = 128;
    const int BoxHeaderSize = 0x10;

    static MethodRef touchCount, getTouch, screenWidth, screenHeight;
    static int touchPosition = -1;
    static bool drawErrorReported;

    // ---- Draw hooks ----

    // A missing name leaves the game's own screen and costs nothing else: the mod still loads.
    public static void InstallDrawHooks()
    {
        try
        {
            On_GUIWorldGenerateMenu.Draw += GenerateMenuDraw;
        }
        catch (Exception e)
        {
            Util.mod?.Logger.Error("[WorldGenPreviewer] GUIWorldGenerateMenu.Draw could not be hooked: " + e.Message);
        }
        try
        {
            On_GUIStatusMenu.Draw += StatusMenuDraw;
        }
        catch (Exception e)
        {
            Util.mod?.Logger.Error("[WorldGenPreviewer] GUIStatusMenu.Draw could not be hooked: " + e.Message);
        }
    }

    static void Report(Exception e)
    {
        if (drawErrorReported)
            return;
        drawErrorReported = true;
        Util.mod?.Logger.Error("[WorldGenPreviewer] a draw hook failed: " + e);
    }

    // An exception must never leave these two: the game's frames are above them.
    static void GenerateMenuDraw(On_GUIWorldGenerateMenu.orig_Draw orig, GUIWorldGenerateMenu self)
    {
        try
        {
            if (Screen.BeginGenerateMenu())
                orig(self);
        }
        catch (Exception e)
        {
            Report(e);
        }
        finally
        {
            Screen.EndGenerateMenu();
        }
    }

    static void StatusMenuDraw(On_GUIStatusMenu.orig_Draw orig, GUIStatusMenu self)
    {
        try
        {
            orig(self);
            Screen.AfterStatusMenu();
        }
        catch (Exception e)
        {
            Report(e);
        }
    }

    // ---- WorldMap ----

    // Main.Map.Update(x, y, 255) for one column.
    public static void UpdateColumn(int x, int y0, int y1)
    {
        WorldMap map = Main.Map;
        for (int y = y0; y < y1; y++)
            map.Update(x, y, 255);
    }

    // ---- WorldGenerateMenu_Layout and ProgressBar_Layout ----

    public struct Bar
    {
        public Texture2D texture;
        public Color color, empty;
        public float backingScale, locationY;
        public Vector2 fillingLocation, fillingSize;
        public bool total;
    }

    static WorldGenerateMenu_Layout MenuLayout
    {
        get
        {
            WorldGenerateMenu_Layout instance = WorldGenerateMenu_Layout.Instance;
            return !instance.IsNull ? instance : throw new InvalidOperationException("WorldGenerateMenu_Layout.Instance is null");
        }
    }

    public static String_Layout StatusLayout => MenuLayout.Status;

    static Bar ReadBar(ProgressBar_Layout bar, Texture2D texture, Color color, bool total)
    {
        return new Bar
        {
            texture = texture,
            color = color,
            empty = bar.FillingColorEmpty,
            backingScale = bar.BackingScale,
            locationY = bar.Location.Y,
            fillingLocation = bar.FillingLocation,
            fillingSize = bar.FillingSize,
            total = total,
        };
    }

    // The total bar takes its look from the world's evil. The two come back from top to bottom.
    public static void ReadBars(out Bar first, out Bar second)
    {
        bool random = WorldGen.drunkWorldGen, crimson = WorldGen.crimson;
        WorldGenerateMenu_Layout menu = MenuLayout;
        Texture_Layout backing = random ? menu.BackingRandom : crimson ? menu.BackingCrimson : menu.BackingCorrupt;
        Color color = random ? menu.FillingColorRandom : crimson ? menu.FillingColorCrimson : menu.FillingColorCorrupt;
        ProgressBar_Layout progress = menu.Progress, stage = menu.StageProgress;
        Bar total = ReadBar(progress, backing.Texture, color, true);
        Bar value = ReadBar(stage, stage.Backing.Texture, stage.FillingColor, false);
        bool inOrder = total.locationY <= value.locationY;
        first = inOrder ? total : value;
        second = inOrder ? value : total;
    }

    // ---- StructureMap ----

    public static GameList StructureList(bool isProtected)
    {
        StructureMap map = GenVars.structures;
        if (map.IsNull)
            return default;
        return new GameList(isProtected ? map._protectedStructures : map._structures);
    }

    // ---- UnityEngine input ----

    static int CallInt(ref MethodRef slot, GameClass cls, string signature)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref slot, cls, signature, NoParameters);
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
        return (int)frame.Out[0];
    }

    public static int TouchCount() => CallInt(ref touchCount, InputClass, "int get_touchCount()");

    public static int ScreenWidth() => CallInt(ref screenWidth, ScreenClass, "int get_width()");

    public static int ScreenHeight() => CallInt(ref screenHeight, ScreenClass, "int get_height()");

    public static Vector2 TouchPosition(int index)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref getTouch, InputClass, "Touch GetTouch(int index)", OneParameter);
        int offset = GameInterop.Offset(ref touchPosition, TouchClass, "m_Position") - BoxHeaderSize;
        if (offset < 0 || offset + sizeof(Vector2) > TouchBufferSize)
            throw new InvalidOperationException("UnityEngine.Touch.m_Position is at " + offset);
        byte* touch = stackalloc byte[TouchBufferSize];
        CallFrame frame = default;
        frame.X[0] = index;
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.Indirect;
        frame.Indirect = (IntPtr)touch;
        frame.RetSize = TouchBufferSize;
        GameInterop.Call(method.Fn, &frame);
        return *(Vector2*)(touch + offset);
    }
}
