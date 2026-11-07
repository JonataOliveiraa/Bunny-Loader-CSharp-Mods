using System.IO;
using Terraria.ModLoader;
using UniversalMapMod.Common;

public class UniversalMap : Mod
{
    public override void Load() => Sync.mod = this;

    public override void HandlePacket(BinaryReader reader, int whoAmI) => Sync.Handle(reader, whoAmI);
}
