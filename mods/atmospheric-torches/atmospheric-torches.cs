using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using AtmosphericTorchesMod.Common;
using BunnyLoader;
using BunnyLoader.Interop;
using Terraria.ModLoader;

// TorchID has no members or On_ hooks in the C# API, so TorchColor goes through a raw hook
// with the signature the JavaScript version used.
public unsafe class AtmosphericTorches : Mod
{
    static AtmosphericTorches instance = null!;
    static IntPtr* original;
    static bool reported;

    public override void Load()
    {
        instance = this;
        try
        {
            original = Hook.Install(new GameClass("Terraria.ID", "TorchID"),
                "void TorchColor(int torchID, out float R, out float G, out float B)",
                (IntPtr)(delegate* unmanaged<int, float*, float*, float*, IntPtr, void>)&TorchColor).OriginalSlot;
        }
        catch (Exception e)
        {
            Logger.Error("The TorchColor hook could not be installed: " + e.Message);
        }
    }

    // TorchColor may run away from the game's main thread, so this does not use Main.rand.
    static float Flicker() => Random.Shared.Next(3) switch
    {
        0 => 0.93f,
        1 => 0.96f,
        _ => 1f,
    };

    static void Original(int id, float* r, float* g, float* b, IntPtr method)
    {
        CallFrame frame;
        Unsafe.SkipInit(out frame);
        frame.X[0] = id;
        frame.X[1] = (long)r;
        frame.X[2] = (long)g;
        frame.X[3] = (long)b;
        frame.X[4] = (long)method;
        frame.Ret = RetKind.Int;
        GameInterop.Call(*original, &frame);
    }

    // Exceptions must not leave this method: there is no managed caller to catch them.
    [UnmanagedCallersOnly]
    static void TorchColor(int id, float* R, float* G, float* B, IntPtr method)
    {
        try
        {
            Original(id, R, G, B, method);

            TorchConfig config = TorchConfig.Instance;
            bool steady = config.DisableTorchFlickering;
            float mult = steady ? 1f : Flicker();
            float multClassicTorch = steady ? config.brightnessOffset : Flicker() * config.brightnessOffset;

            switch (id)
            {
                case 0:
                    *R = 255f * multClassicTorch;
                    *G = 199f * multClassicTorch;
                    *B = 137f * multClassicTorch;
                    return;
                case 10:
                    *R = 0.75f * mult;
                    *G = 0.85f * mult;
                    *B = 1.4f * mult;
                    return;
                case 15:
                    *R = 0.95f * mult;
                    *G = 0.75f * mult;
                    *B = 1.3f * mult;
                    return;
                case 19:
                    *R = 1.4f * mult;
                    *G = 0.86f * mult;
                    *B = 0.55f * mult;
                    return;
                case 20:
                    *R = 0.25f * mult;
                    *G = 1.3f * mult;
                    *B = 0.8f * mult;
                    return;
                case 21:
                    *R = 0.95f * mult;
                    *G = 0.4f * mult;
                    *B = 1.4f * mult;
                    return;
                case 23:
                    *R = 1.4f * mult;
                    *G = 0.7f * mult;
                    *B = 0.5f * mult;
                    return;
                case 24:
                    *R = 1.25f * mult;
                    *G = 0.6f * mult;
                    *B = 1.2f * mult;
                    return;
                case 25:
                    *R = 0.75f * mult;
                    *G = 1.45f * mult;
                    *B = 0.9f * mult;
                    return;
            }

            if (steady)
                return;

            float flickering = Flicker();
            *R *= flickering;
            *G *= flickering;
            *B *= flickering;
        }
        catch (Exception e)
        {
            if (!reported)
            {
                reported = true;
                instance.Logger.Error("TorchColor: " + e.Message);
            }
        }
    }
}
