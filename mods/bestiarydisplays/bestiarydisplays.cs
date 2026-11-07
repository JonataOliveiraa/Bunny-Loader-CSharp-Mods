using System.IO;
using BestiaryDisplaysMod.Common;
using BestiaryDisplaysMod.Content.TileEntities;
using Terraria.ModLoader;

public class BestiaryDisplays : Mod
{
    public override void HandlePacket(BinaryReader reader, int whoAmI) => Util.Safe("packet", () => HologramTileEntity.HandlePacket(reader));
}
