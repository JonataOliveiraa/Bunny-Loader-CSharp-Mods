using System;
using System.IO;
using GnorpBuilderMod.Common;
using Terraria.ModLoader;

public class GnorpBuilder : Mod
{
    public static GnorpBuilder Instance = null!;

    public override void Load()
    {
        Instance = this;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        try
        {
            Placement.HandlePacket(reader, whoAmI);
        }
        catch (Exception e)
        {
            Logger.Error("packet from player " + whoAmI + " failed: " + e.Message);
        }
    }
}
