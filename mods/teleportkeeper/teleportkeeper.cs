using System.IO;
using TeleportKeeperMod.Common;
using Terraria.ModLoader;

public class TeleportKeeper : Mod
{
    public override void Load()
    {
        Net.Install(this);
        Teleports.InstallNet();
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI) => Util.Safe("packet", () => Net.Handle(reader, whoAmI));
}
