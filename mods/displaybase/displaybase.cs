using System.IO;
using DisplayBaseMod.Lib;
using Terraria.ModLoader;

public class DisplayBase : Mod
{
    public override void HandlePacket(BinaryReader reader, int whoAmI) => Net.HandlePacket(reader, whoAmI);
}
