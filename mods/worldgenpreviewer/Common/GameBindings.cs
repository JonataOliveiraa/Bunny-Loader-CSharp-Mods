using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BunnyLoader;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Map;
using Terraria.WorldBuilding;
using WorldGenPreviewerMod.Common.UI;

namespace WorldGenPreviewerMod.Common;

// Game members the C# API does not expose, bound by name at run time, the way the JavaScript
// version reached them. The names were checked in the game's metadata, but only fail when the
// game runs: nothing the C# compiler checks stands in for them.
public static unsafe class GameBindings
{
    static readonly GameClass MenuLayoutClass = new("", "WorldGenerateMenu_Layout");
    static readonly GameClass ProgressBarClass = new("", "GUIProgressBar");
    static readonly GameClass StatusMenuClass = new("", "GUIStatusMenu");
    static readonly GameClass InputClass = new("UnityEngine", "Input");
    static readonly GameClass ScreenClass = new("UnityEngine", "Screen");
    static readonly GameClass TouchClass = new("UnityEngine", "Touch");

    static readonly int[] NoParameters = { -1 };
    static readonly int[] OneParameter = { -1, -1 };
    static readonly int[] TwoParameters = { -1, -1, -1 };
    static readonly int[] ThreeParameters = { -1, -1, -1, -1 };
    static readonly int[] FillingLayout = { -1, 8, -1, -1, -1, 4, 4, 4, -1 };

    // A UnityEngine.Touch is 68 bytes. An offset from the game counts the boxed struct's header.
    const int TouchBufferSize = 128;
    const int BoxHeaderSize = 0x10;

    static Hook? generateMenuHook, statusMenuHook;
    static MethodRef mapUpdate, mapAllocate, mapConstructor, drawFilling2;
    static MethodRef touchCount, getTouch, screenWidth, screenHeight;
    static StaticRef layoutInstance;
    static int maxWidth = -1, maxHeight = -1;
    static int statusOffset = -1, progressOffset = -1, stageOffset = -1;
    static int backingCorrupt = -1, backingCrimson = -1, backingRandom = -1;
    static int colorCorrupt = -1, colorCrimson = -1, colorRandom = -1;
    static int barBacking = -1, barScale = -1, barLocation = -1, barFillingSize = -1, barFillingLocation = -1, barColor = -1, barEmpty = -1;
    static int structures = -1, protectedStructures = -1;
    static int touchPosition = -1;
    static bool drawErrorReported;

    // ---- Draw hooks: neither menu has On_ events in the C# API. ----

    // A missing name leaves the game's own screen and costs nothing else: the mod still loads.
    public static void InstallDrawHooks()
    {
        try
        {
            generateMenuHook = Hook.Install(GUIWorldGenerateMenu.Class, "void Draw()", (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&GenerateMenuDraw);
        }
        catch (Exception e)
        {
            Util.mod?.Logger.Error("[WorldGenPreviewer] GUIWorldGenerateMenu.Draw could not be hooked: " + e.Message);
        }
        try
        {
            statusMenuHook = Hook.Install(StatusMenuClass, "void Draw()", (IntPtr)(delegate* unmanaged<IntPtr, IntPtr, void>)&StatusMenuDraw);
        }
        catch (Exception e)
        {
            Util.mod?.Logger.Error("[WorldGenPreviewer] GUIStatusMenu.Draw could not be hooked: " + e.Message);
        }
    }

    static void Original(Hook hook, IntPtr self, IntPtr method)
    {
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)self;
        frame.X[1] = (long)method;
        frame.Ret = RetKind.Int;
        GameInterop.Call(hook.Original, &frame);
    }

    static void Report(Exception e)
    {
        if (drawErrorReported)
            return;
        drawErrorReported = true;
        Util.mod?.Logger.Error("[WorldGenPreviewer] a draw hook failed: " + e);
    }

    // An exception must never leave these two: the game's frames are above them.
    [UnmanagedCallersOnly]
    static void GenerateMenuDraw(IntPtr self, IntPtr method)
    {
        try
        {
            if (Screen.BeginGenerateMenu())
                Original(generateMenuHook!, self, method);
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

    [UnmanagedCallersOnly]
    static void StatusMenuDraw(IntPtr self, IntPtr method)
    {
        try
        {
            Original(statusMenuHook!, self, method);
            Screen.AfterStatusMenu();
        }
        catch (Exception e)
        {
            Report(e);
        }
    }

    // ---- WorldMap ----

    public static void GetMapSize(WorldMap map, out int width, out int height)
    {
        width = *(int*)(map.Pointer + GameInterop.Offset(ref maxWidth, WorldMap.Class, "MaxWidth"));
        height = *(int*)(map.Pointer + GameInterop.Offset(ref maxHeight, WorldMap.Class, "MaxHeight"));
    }

    public static WorldMap NewMap()
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref mapConstructor, WorldMap.Class, "void .ctor()", NoParameters);
        IntPtr map = GameInterop.NewObject(WorldMap.Class);
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)map;
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
        return WorldMap.From(new GameObject(map));
    }

    public static void AllocateMap(WorldMap map, int width, int height)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref mapAllocate, WorldMap.Class, "void Allocate(int maxWidth, int maxHeight)", TwoParameters);
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = (long)map.Pointer;
        frame.X[1] = width;
        frame.X[2] = height;
        frame.X[3] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
    }

    // Main.Map.Update(x, y, 255) for one column.
    public static void UpdateColumn(int x, int y0, int y1)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref mapUpdate, WorldMap.Class, "void Update(int x, int y, byte light)", ThreeParameters);
        IntPtr map = Main.Map.Pointer;
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        for (int y = y0; y < y1; y++)
        {
            frame.X[0] = (long)map;
            frame.X[1] = x;
            frame.X[2] = y;
            frame.X[3] = 255;
            frame.X[4] = (long)method.Info;
            frame.Ret = RetKind.Int;
            GameInterop.Call(method.Fn, &frame);
        }
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

    static IntPtr MenuLayout
    {
        get
        {
            IntPtr instance = GameInterop.ReadStatic<IntPtr>(ref layoutInstance, MenuLayoutClass, "Instance");
            return instance != IntPtr.Zero ? instance : throw new InvalidOperationException("WorldGenerateMenu_Layout.Instance is null");
        }
    }

    static IntPtr MenuField(ref int slot, string field) => *(IntPtr*)(MenuLayout + GameInterop.Offset(ref slot, MenuLayoutClass, field));

    public static String_Layout StatusLayout => String_Layout.From(new GameObject(MenuField(ref statusOffset, "Status")));

    static Bar ReadBar(IntPtr bar, Texture2D texture, Color color, bool total)
    {
        GameClass c = ProgressBar_Layout.Class;
        return new Bar
        {
            texture = texture,
            color = color,
            empty = *(Color*)(bar + GameInterop.Offset(ref barEmpty, c, "FillingColorEmpty")),
            backingScale = *(float*)(bar + GameInterop.Offset(ref barScale, c, "BackingScale")),
            locationY = ((Vector2*)(bar + GameInterop.Offset(ref barLocation, c, "Location")))->Y,
            fillingLocation = *(Vector2*)(bar + GameInterop.Offset(ref barFillingLocation, c, "FillingLocation")),
            fillingSize = *(Vector2*)(bar + GameInterop.Offset(ref barFillingSize, c, "FillingSize")),
            total = total,
        };
    }

    static Texture2D BarTexture(IntPtr bar) =>
        Texture_Layout.From(new GameObject(*(IntPtr*)(bar + GameInterop.Offset(ref barBacking, ProgressBar_Layout.Class, "Backing")))).Texture;

    // The total bar takes its look from the world's evil. The two come back from top to bottom.
    public static void ReadBars(out Bar first, out Bar second)
    {
        bool random = WorldGen.drunkWorldGen, crimson = WorldGen.crimson;
        IntPtr backing = random ? MenuField(ref backingRandom, "BackingRandom") : crimson ? MenuField(ref backingCrimson, "BackingCrimson") : MenuField(ref backingCorrupt, "BackingCorrupt");
        Color color = random ? MenuColor(ref colorRandom, "FillingColorRandom") : crimson ? MenuColor(ref colorCrimson, "FillingColorCrimson") : MenuColor(ref colorCorrupt, "FillingColorCorrupt");
        IntPtr progress = MenuField(ref progressOffset, "Progress"), stage = MenuField(ref stageOffset, "StageProgress");
        Bar total = ReadBar(progress, Texture_Layout.From(new GameObject(backing)).Texture, color, true);
        Bar value = ReadBar(stage, BarTexture(stage), *(Color*)(stage + GameInterop.Offset(ref barColor, ProgressBar_Layout.Class, "FillingColor")), false);
        bool inOrder = total.locationY <= value.locationY;
        first = inOrder ? total : value;
        second = inOrder ? value : total;
    }

    static Color MenuColor(ref int slot, string field) => *(Color*)(MenuLayout + GameInterop.Offset(ref slot, MenuLayoutClass, field));

    public static void DrawFilling2(SpriteBatch spriteBatch, Vector2 topLeft, int height, int completedWidth, int totalWidth, Color filled, Color separator, Color empty)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref drawFilling2, ProgressBarClass,
            "void DrawFilling2(SpriteBatch spritebatch, Vector2 topLeft, int height, int completedWidth, int totalWidth, Color filled, Color separator, Color empty)", FillingLayout);
        CallFrame frame = default;
        frame.X[0] = (long)spriteBatch.Pointer;
        frame.D[0] = Abi.F32(topLeft.X);
        frame.D[1] = Abi.F32(topLeft.Y);
        frame.X[1] = height;
        frame.X[2] = completedWidth;
        frame.X[3] = totalWidth;
        *(Color*)(frame.X + 4) = filled;
        *(Color*)(frame.X + 5) = separator;
        *(Color*)(frame.X + 6) = empty;
        frame.X[7] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
    }

    // ---- StructureMap ----

    public static GameList StructureList(bool isProtected)
    {
        StructureMap map = GenVars.structures;
        if (map.IsNull)
            return default;
        int offset = isProtected
            ? GameInterop.Offset(ref protectedStructures, StructureMap.Class, "_protectedStructures")
            : GameInterop.Offset(ref structures, StructureMap.Class, "_structures");
        return new GameList(*(IntPtr*)(map.Pointer + offset));
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
