using System;
using System.Collections.Generic;
using Terraria.WorldBuilding;

namespace WorldGenPreviewerMod.Common.Gen;

public static class Status
{
    public const int Pending = 0, Running = 1, Done = 2, Skipped = 3, Cancelled = 4;
}

// Shared by the generation's worker thread, which runs the passes and waits on these flags, and
// the game thread, which draws them and sets them from touches. In C# both really run at once,
// so the flags are volatile and the per-pass arrays are only ever replaced whole.
public static class State
{
    public const string SETUP_PASS = "WorldGenPreviewer";

    public static volatile bool active;
    public static WorldGenerator generator;
    public static GenerationProgress progress;
    public static IntPtr[] passes = Array.Empty<IntPtr>();
    public static string[] names = Array.Empty<string>();
    public static string?[] labels = Array.Empty<string?>();
    public static double[] weights = Array.Empty<double>();
    public static int[] status = Array.Empty<int>();
    public static int[] repeats = Array.Empty<int>();
    public static bool[] skip = Array.Empty<bool>();
    public static readonly HashSet<int> disabled = new();
    public static volatile int index = -1;
    public static volatile int lastApplied = -1;

    public static volatile bool continueGen = true;
    public static volatile bool pauseAfterContinue;
    public static volatile int pauseAfterIndex = -1;
    public static volatile bool repeat;
    public static volatile bool cancel;
    public static volatile bool cancelled;
    public static volatile bool paused;
    public static volatile bool repeating;

    public static volatile string statusKey = "StatusNormal";
    public static string[] statusArgs = Array.Empty<string>();

    public static volatile bool showStructures;
    public static string? worldPath;
    public static bool existedBefore = true;
    public static volatile bool cleanupPending;
    public static volatile bool finishing;
    public static volatile bool mapFinished;
    public static double lastDraw;
    public static volatile int drawnSession = -1;
    public static volatile bool tapSeen;
    public static volatile int session;

    public static void SetStatus(string key, params string[] args)
    {
        statusArgs = args;
        statusKey = key;
    }

    public static void ResetControls(bool startPaused)
    {
        Array.Clear(skip);
        disabled.Clear();
        index = -1;
        lastApplied = -1;
        continueGen = !startPaused;
        pauseAfterContinue = false;
        pauseAfterIndex = -1;
        repeat = false;
        cancel = false;
        paused = false;
        repeating = false;
        showStructures = false;
        SetStatus("StatusNormal");
    }

    public static List<int> VisibleIndices()
    {
        var result = new List<int>();
        string[] all = names;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != SETUP_PASS)
                result.Add(i);
        }
        return result;
    }

    // The arrays can be replaced while the screen draws an older row.
    public static int StatusOf(int i)
    {
        int[] all = status;
        return i >= 0 && i < all.Length ? all[i] : Status.Pending;
    }

    public static bool IsSkipped(int i)
    {
        bool[] all = skip;
        return i >= 0 && i < all.Length && all[i];
    }

    public static int RepeatsOf(int i)
    {
        int[] all = repeats;
        return i >= 0 && i < all.Length ? all[i] : 1;
    }
}
