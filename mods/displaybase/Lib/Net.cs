using System.IO;
using DisplayBaseMod.Content.TileEntities;
using Terraria.ModLoader;

namespace DisplayBaseMod.Lib;

public static class Net
{
    const byte Place = 0;

    public static void SendPlace(int x, int y, int type, int prefix, int stack)
    {
        ModPacket packet = ModContent.GetInstance<DisplayBase>().GetPacket();
        packet.Write(Place);
        packet.Write(x);
        packet.Write(y);
        packet.Write(type);
        packet.Write(prefix);
        packet.Write(stack);
        packet.Send(-1, -1);
    }

    public static void HandlePacket(BinaryReader reader, int whoAmI)
    {
        if (reader.ReadByte() != Place)
            return;
        int x = reader.ReadInt32(), y = reader.ReadInt32();
        int type = reader.ReadInt32(), prefix = reader.ReadInt32(), stack = reader.ReadInt32();
        TEDisplayBase.TryPlacing(x, y, type, prefix, stack);
    }
}
