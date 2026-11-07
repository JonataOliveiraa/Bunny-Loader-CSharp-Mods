using System;
using BunnyLoader.Interop;

namespace ModDifficultyMod.DifficultySystem.Utils;

// WorldCreateMenu_Layout and WorldSelectMenu_Layout have no C# proxy, so their fields are read
// by name at run time. These names only fail when the game runs.
public static unsafe class MenuLayouts
{
    static readonly GameClass CreateClass = new("", "WorldCreateMenu_Layout");
    static readonly GameClass SelectClass = new("", "WorldSelectMenu_Layout");

    static StaticRef createInstance, selectInstance;
    static int normal = -1, expert = -1, master = -1, journey = -1, greyout = -1, backing = -1, value = -1, title = -1, itemMode = -1;

    static IntPtr Create
    {
        get
        {
            IntPtr instance = GameInterop.ReadStatic<IntPtr>(ref createInstance, CreateClass, "Instance");
            return instance != IntPtr.Zero ? instance : throw new InvalidOperationException("WorldCreateMenu_Layout.Instance is null");
        }
    }

    static IntPtr Select
    {
        get
        {
            IntPtr instance = GameInterop.ReadStatic<IntPtr>(ref selectInstance, SelectClass, "Instance");
            return instance != IntPtr.Zero ? instance : throw new InvalidOperationException("WorldSelectMenu_Layout.Instance is null");
        }
    }

    static IntPtr Read(ref int slot, string field) => *(IntPtr*)(Create + GameInterop.Offset(ref slot, CreateClass, field));

    static TransactionButton_Layout Button(ref int slot, string field) => TransactionButton_Layout.From(new GameObject(Read(ref slot, field)));

    static Panel_Layout Panel(ref int slot, string field) => Panel_Layout.From(new GameObject(Read(ref slot, field)));

    public static TransactionButton_Layout Normal => Button(ref normal, "Normal");

    public static TransactionButton_Layout Expert => Button(ref expert, "Expert");

    public static TransactionButton_Layout Master => Button(ref master, "Master");

    public static TransactionButton_Layout Journey => Button(ref journey, "Journey");

    public static TransactionButton_Layout DifficultyValue => Button(ref value, "DifficultyValue");

    public static Panel_Layout Greyout => Panel(ref greyout, "Greyout");

    public static Panel_Layout DifficultyOptionsBacking => Panel(ref backing, "DifficultyOptionsBacking");

    public static String_Layout DifficultyTitle => String_Layout.From(new GameObject(Read(ref title, "DifficultyTitle")));

    public static String_Layout ItemMode =>
        String_Layout.From(new GameObject(*(IntPtr*)(Select + GameInterop.Offset(ref itemMode, SelectClass, "ItemMode"))));
}
