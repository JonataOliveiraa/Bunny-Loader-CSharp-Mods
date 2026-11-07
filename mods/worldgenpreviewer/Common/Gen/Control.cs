using System;
using System.IO;
using System.Threading;
using BunnyLoader.Interop;
using Terraria;
using Terraria.IO;
using Terraria.Utilities;
using Terraria.WorldBuilding;
using WorldGenPreviewerMod.Common.Configs;
using static WorldGenPreviewerMod.Common.Gen.State;
using NativePass = BunnyLoader.Interop.WorldGen.GenPass;

namespace WorldGenPreviewerMod.Common.Gen;

public static class Control
{
    const int PAUSE_SLEEP_MS = 25;
    const double NO_SCREEN_MS = 5000;
    const double NO_INPUT_MS = 20000;
    const double LOST_SCREEN_MS = 600000;

    // The original could lose Thread.Sleep on some devices. In C# it is always there.
    public const bool canWait = true;

    static WorldGenPreviewerConfig Cfg => WorldGenPreviewerConfig.Instance;

    static void Snapshot(WorldGenerator gen)
    {
        var list = new GameList(gen._passes);
        int n = list.Count;
        var newPasses = new IntPtr[n];
        var newNames = new string[n];
        var newWeights = new double[n];
        var newStatus = new int[n];
        var newRepeats = new int[n];
        var newSkip = new bool[n];
        Array.Copy(skip, newSkip, Math.Min(n, skip.Length));
        for (int k = 0; k < n; k++)
        {
            var pass = new NativePass(list[k]);
            newPasses[k] = pass.Pointer;
            newNames[k] = pass.Name ?? "";
            newWeights[k] = pass.Weight;
            newStatus[k] = Status.Pending;
            newRepeats[k] = 1;
        }
        // Names last: the screen rebuilds its rows when their number changes.
        passes = newPasses;
        labels = new string?[n];
        weights = newWeights;
        status = newStatus;
        repeats = newRepeats;
        skip = newSkip;
        names = newNames;
    }

    static void Begin(WorldGenerator gen)
    {
        generator = gen;
        PassNames.ClearLabels();
        session++;
        Snapshot(gen);
        ResetControls(Cfg.StartWorldgenPaused && canWait);
        worldPath = Main.worldPathName;
        try
        {
            existedBefore = !string.IsNullOrEmpty(worldPath) && File.Exists(worldPath);
        }
        catch (Exception)
        {
            existedBefore = true;
        }
        cancelled = false;
        cleanupPending = false;
        active = true;
    }

    static void End()
    {
        foreach (int i in disabled)
            new NativePass(passes[i]).Enable();
        disabled.Clear();
        active = false;
        paused = false;
        repeating = false;
        progress = default;
        generator = default;
    }

    static GenPassResult RunDisabled(On_WorldGenerator.orig_RunPass orig, WorldGenerator self, NativePass pass, int i)
    {
        bool enabled = pass.Enabled;
        if (enabled)
        {
            pass.Disable();
            disabled.Add(i);
        }
        try
        {
            return orig(self, pass);
        }
        finally
        {
            if (enabled)
            {
                pass.Enable();
                disabled.Remove(i);
            }
        }
    }

    static bool Visible(int i) => i >= 0 && names[i] != SETUP_PASS;

    // The game has no field behind WorldGen.genRand to replace, so as in the original a repeated
    // step only starts Main.rand again from the world's seed.
    static void Reseed(WorldGenerator gen)
    {
        try
        {
            Main.rand = UnifiedRandom.New(gen._seed);
        }
        catch (Exception e)
        {
            Util.LogOnce("reseed", "repeating without restarting the seed: " + e.Message);
        }
    }

    static void PauseLoop(On_WorldGenerator.orig_RunPass orig, WorldGenerator self)
    {
        if (!continueGen && !cancel)
        {
            paused = true;
            SetStatus("StatusPaused");
        }
        double since = Util.NowMs;
        while (!continueGen && !cancel)
        {
            if (repeat)
            {
                repeat = false;
                int target = lastApplied;
                if (target >= 0)
                {
                    repeating = true;
                    SetStatus("StatusRepeating");
                    status[target] = Status.Running;
                    Reseed(self);
                    orig(self, new NativePass(passes[target]));
                    status[target] = Status.Done;
                    repeats[target]++;
                    repeating = false;
                }
                if (!continueGen && !cancel)
                    SetStatus("StatusPaused");
                since = Util.NowMs;
                continue;
            }
            double now = Util.NowMs;
            double quiet = now - Math.Max(Volatile.Read(ref lastDraw), since);
            string? reason = null;
            if (drawnSession != session && quiet > NO_SCREEN_MS)
                reason = "the screen did not appear";
            else if (quiet > LOST_SCREEN_MS)
                reason = "the screen has been gone for 10 minutes";
            else if (!tapSeen && now - since > NO_INPUT_MS)
                reason = "no touch reached the screen";
            if (reason != null)
            {
                Util.Log(reason + ": continuing the generation");
                continueGen = true;
                pauseAfterContinue = false;
                SetStatus("StatusNormal");
                break;
            }
            Thread.Sleep(PAUSE_SLEEP_MS);
        }
        paused = false;
        if (pauseAfterContinue)
        {
            pauseAfterContinue = false;
            continueGen = false;
        }
    }

    static void AfterPass(On_WorldGenerator.orig_RunPass orig, WorldGenerator self, int i)
    {
        if (!canWait || cancel || lastApplied < 0)
            return;
        if (i >= passes.Length - 1)
            return;
        if (pauseAfterIndex >= 0 && pauseAfterIndex == lastApplied)
        {
            pauseAfterIndex = -1;
            continueGen = false;
        }
        PauseLoop(orig, self);
    }

    static int IndexOfPass(NativePass pass) => Array.IndexOf(passes, pass.Pointer);

    static GenPassResult OnRunPass(On_WorldGenerator.orig_RunPass orig, WorldGenerator self, NativePass pass)
    {
        if (!active)
            Begin(self);
        int i = IndexOfPass(pass);
        if (i < 0)
        {
            Snapshot(self);
            i = IndexOfPass(pass);
        }
        index = i;
        if (i < 0)
            return orig(self, pass);

        if (cancel || skip[i])
        {
            GenPassResult skipped = RunDisabled(orig, self, pass, i);
            status[i] = cancel ? Status.Cancelled : Status.Skipped;
            if (!cancel && Visible(i))
                AfterPass(orig, self, i);
            return skipped;
        }

        status[i] = Status.Running;
        string before = progress.IsNull ? "" : progress.Message ?? "";
        GenPassResult result = orig(self, pass);
        string after = (progress.IsNull ? "" : progress.Message ?? "").Trim();
        if (after.Length > 0 && after != before.Trim())
            labels[i] = after;
        status[i] = Status.Done;
        if (Visible(i))
        {
            lastApplied = i;
            AfterPass(orig, self, i);
        }
        return result;
    }

    public static void InstallControl() => On_WorldGenerator.RunPass += OnRunPass;

    static void FinishMap()
    {
        if (!canWait || !Cfg.LivePreview || drawnSession != session)
            return;
        mapFinished = false;
        finishing = true;
        SetStatus("StatusFinishingMap");
        double start = Util.NowMs;
        while (!mapFinished && !cancel)
        {
            double now = Util.NowMs;
            if (now - Volatile.Read(ref lastDraw) > NO_SCREEN_MS || now - start > LOST_SCREEN_MS)
                break;
            Thread.Sleep(PAUSE_SLEEP_MS);
        }
        finishing = false;
        SetStatus("StatusNormal");
    }

    public static void InstallWorldHooks()
    {
        On_WorldGenerator.GenerateWorld += (orig, self) =>
        {
            if (!active)
                Begin(self);
            else
                Snapshot(self);
            bool ok;
            try
            {
                ok = orig(self);
                if (!cancel)
                    FinishMap();
            }
            finally
            {
                End();
            }
            if (cancel)
            {
                cancelled = true;
                cleanupPending = true;
                return false;
            }
            return ok;
        };

        On_WorldFile.InternalSaveWorld += (orig, cloud, reset) =>
        {
            if (cancelled)
            {
                DeleteCancelledWorld();
                return;
            }
            orig(cloud, reset);
        };

        On_GUIWorldCreateMenu.CreateWorld += (orig, self) =>
        {
            cancelled = false;
            cleanupPending = false;
            orig(self);
        };

        On_WorldFile.LoadWorld += (orig, check) =>
        {
            cancelled = false;
            orig(check);
        };
    }

    public static void DeleteCancelledWorld()
    {
        string? path = worldPath;
        if (string.IsNullOrEmpty(path) || existedBefore)
            return;
        foreach (string file in new[] { path, path + ".bak", path + ".bl.json", path + ".bl.header.json" })
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception e)
            {
                Util.LogOnce("delete:" + file, "could not delete " + file + ": " + e.Message);
            }
        }
    }
}

public static class Commands
{
    // The worker reads these while they change: the flag that releases it is always set last.
    public static void Play()
    {
        pauseAfterContinue = false;
        pauseAfterIndex = -1;
        continueGen = true;
        SetStatus("StatusNormal");
    }

    public static void Pause()
    {
        continueGen = false;
        pauseAfterContinue = false;
        pauseAfterIndex = -1;
        SetStatus("StatusPausing");
    }

    public static void Next()
    {
        pauseAfterContinue = true;
        pauseAfterIndex = -1;
        continueGen = true;
        SetStatus("StatusPausing");
    }

    public static void Previous()
    {
        continueGen = false;
        pauseAfterContinue = false;
        pauseAfterIndex = -1;
        repeat = true;
        SetStatus("StatusWaitingRepeat");
    }

    public static void Cancel()
    {
        cancel = true;
        pauseAfterContinue = false;
        pauseAfterIndex = -1;
        continueGen = true;
        SetStatus("StatusCanceling");
    }

    public static void PauseAfter(int i)
    {
        int s = StatusOf(i);
        if (s == Status.Done || s == Status.Skipped || s == Status.Cancelled || i <= lastApplied)
            return;
        bool[] all = skip;
        if (i < all.Length)
            all[i] = false;
        pauseAfterContinue = false;
        pauseAfterIndex = i;
        continueGen = true;
        SetStatus("StatusPausingAfter", PassNames.Label(i));
    }

    public static void ToggleSkip(int i)
    {
        bool[] all = skip;
        if (StatusOf(i) != Status.Pending || i < 0 || i >= all.Length)
            return;
        all[i] = !all[i];
    }

    public static void ToggleStructures()
    {
        showStructures = !showStructures;
        SetStatus(showStructures ? "StatusStructuresOn" : "StatusStructuresOff");
    }
}
