using System;
using AtmosphericTorchesMod.Common;
using Terraria.ID;
using Terraria.ModLoader;

public class AtmosphericTorches : Mod
{
    public override void Load()
    {
        On_TorchID.TorchColor += TorchColor;
    }

    // TorchColor may run away from the game's main thread, so this does not use Main.rand.
    static float Flicker() => Random.Shared.Next(3) switch
    {
        0 => 0.93f,
        1 => 0.96f,
        _ => 1f,
    };

    static void TorchColor(On_TorchID.orig_TorchColor orig, int id, out float R, out float G, out float B)
    {
        orig(id, out R, out G, out B);

        TorchConfig config = TorchConfig.Instance;
        bool steady = config.DisableTorchFlickering;
        float mult = steady ? 1f : Flicker();
        float multClassicTorch = steady ? config.brightnessOffset : Flicker() * config.brightnessOffset;

        switch (id)
        {
            case 0:
                R = 255f * multClassicTorch;
                G = 199f * multClassicTorch;
                B = 137f * multClassicTorch;
                return;
            case 10:
                R = 0.75f * mult;
                G = 0.85f * mult;
                B = 1.4f * mult;
                return;
            case 15:
                R = 0.95f * mult;
                G = 0.75f * mult;
                B = 1.3f * mult;
                return;
            case 19:
                R = 1.4f * mult;
                G = 0.86f * mult;
                B = 0.55f * mult;
                return;
            case 20:
                R = 0.25f * mult;
                G = 1.3f * mult;
                B = 0.8f * mult;
                return;
            case 21:
                R = 0.95f * mult;
                G = 0.4f * mult;
                B = 1.4f * mult;
                return;
            case 23:
                R = 1.4f * mult;
                G = 0.7f * mult;
                B = 0.5f * mult;
                return;
            case 24:
                R = 1.25f * mult;
                G = 0.6f * mult;
                B = 1.2f * mult;
                return;
            case 25:
                R = 0.75f * mult;
                G = 1.45f * mult;
                B = 0.9f * mult;
                return;
        }

        if (steady)
            return;

        float flickering = Flicker();
        R *= flickering;
        G *= flickering;
        B *= flickering;
    }
}
