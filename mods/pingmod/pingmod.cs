using System.IO;
using PingModPort.Common;
using Terraria.ModLoader;

public class PingMod : Mod
{
    public override void Load() => Ping.Install(this);

    public override void HandlePacket(BinaryReader reader, int whoAmI) => Ping.HandlePacket(reader, whoAmI);
}
