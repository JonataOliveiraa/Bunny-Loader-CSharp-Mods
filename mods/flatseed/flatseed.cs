using System;
using FlatSeedMod.Common;
using Terraria.ModLoader;

public class FlatSeed : Mod
{
    public override void Load()
    {
        FlatSeedRegistration.RegisterFlatSeed();
    }

    public override object? Call(params object?[] args)
    {
        object? method = args.Length > 0 ? args[0] : null;
        if (method as string == "IsFlatWorld")
            return FlatSeedRegistration.IsFlatWorld();
        throw new ArgumentException("Unknown method \"" + method + "\".");
    }
}
