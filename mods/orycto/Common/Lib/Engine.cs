using System;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;

namespace OryctoMod.Common.Lib;

// The Unity members this mod needs have no C# binding: BunnyLoader.dll only
// generates the types in its list. They are reached here by name, the same way the
// generated code does it, so a member that is missing fails at run time, not at build
// time. Those calls throw, and the callers catch and log them.
public static unsafe class Engine
{
    // A touch is about 68 bytes in Unity 2021. The game writes it here, and the bridge
    // copies at most this much, so a larger struct is cut short and cannot overflow.
    const int TouchBufferSize = 128;

    // An offset from the game counts the header of the boxed struct.
    const int BoxHeaderSize = 16;

    public const int PhaseBegan = 0;
    public const int PhaseEnded = 3;
    public const int PhaseCanceled = 4;

    static readonly GameClass Application = new("UnityEngine", "Application");
    static readonly GameClass Input = new("UnityEngine", "Input");
    static readonly GameClass Touch = new("UnityEngine", "Touch");

    static MethodRef setTargetFrameRate, getTouchCount, getTouch, getMouseDown, getMouseUp;
    static int phaseSlot = -1, phaseOffset;

    // One layout entry per parameter, plus the return value. -1 skips the size check.
    static readonly int[] NoParameters = { -1 };
    static readonly int[] OneParameter = { -1, -1 };

    public static bool TouchCountReady { get; private set; }
    public static bool TouchReady { get; private set; }
    public static bool MouseReady { get; private set; }

    public static void SetTargetFrameRate(int fps)
    {
        ref readonly MethodRef method = ref GameInterop.Method(ref setTargetFrameRate, Application, "void set_targetFrameRate(int value)", OneParameter);
        CallFrame frame = default;
        frame.X[0] = fps;
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
    }

    public static long FrameTickTicks
    {
        get => Game.FrameTickTime.Ticks;
        set => Game.FrameTickTime = new TimeSpan(value);
    }

    public static long FrameClampTicks
    {
        get => Game.FrameClampTickTime.Ticks;
        set => Game.FrameClampTickTime = new TimeSpan(value);
    }

    public static long FrameIgnoreTicks
    {
        get => Game.FrameIgnoreTime.Ticks;
        set => Game.FrameIgnoreTime = new TimeSpan(value);
    }

    // Each group binds on its own: without the touches, the mouse check still works.
    public static void BindInput()
    {
        try
        {
            GameInterop.Method(ref getTouchCount, Input, "int get_touchCount()", NoParameters);
            TouchCountReady = true;
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("touch count", e);
        }

        try
        {
            GameInterop.Method(ref getTouch, Input, "Touch GetTouch(int index)", OneParameter);
            phaseOffset = GameInterop.Offset(ref phaseSlot, Touch, "m_Phase") - BoxHeaderSize;
            TouchReady = phaseOffset >= 0 && phaseOffset + sizeof(int) <= TouchBufferSize;
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("touch phase", e);
        }

        try
        {
            GameInterop.Method(ref getMouseDown, Input, "bool GetMouseButtonDown(int button)", OneParameter);
            GameInterop.Method(ref getMouseUp, Input, "bool GetMouseButtonUp(int button)", OneParameter);
            MouseReady = true;
        }
        catch (Exception e)
        {
            BoostLog.WarnOnce("mouse buttons", e);
        }
    }

    public static int TouchCount()
    {
        CallFrame frame = default;
        frame.X[0] = (long)getTouchCount.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(getTouchCount.Fn, &frame);
        return (int)frame.Out[0];
    }

    public static int TouchPhase(int index)
    {
        byte* touch = stackalloc byte[TouchBufferSize];
        CallFrame frame = default;
        frame.X[0] = index;
        frame.X[1] = (long)getTouch.Info;
        frame.Ret = RetKind.Indirect;
        frame.Indirect = (IntPtr)touch;
        frame.RetSize = TouchBufferSize;
        GameInterop.Call(getTouch.Fn, &frame);
        return *(int*)(touch + phaseOffset);
    }

    public static bool MouseButtonChanged(int button) => CallMouse(getMouseDown, button) || CallMouse(getMouseUp, button);

    static bool CallMouse(in MethodRef method, int button)
    {
        CallFrame frame = default;
        frame.X[0] = button;
        frame.X[1] = (long)method.Info;
        frame.Ret = RetKind.Int;
        GameInterop.Call(method.Fn, &frame);
        return (frame.Out[0] & 0xFF) != 0;
    }
}
