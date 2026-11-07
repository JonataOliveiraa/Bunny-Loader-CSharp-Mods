using System.IO;
using ColoredDamageTypesMod.Common;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

public class ColoredDamageTypes : Mod
{
    public override void Load()
    {
        Net.Mod = this;
    }

    public override object? Call(params object?[] args)
    {
        if (args.Length > 0 && args[0] is string method && method == "AddDamageType")
        {
            object? At(int index) => index < args.Length ? args[index] : null;
            if (!DamageTypes.AddDamageType(At(1), At(2), At(3), At(4)))
                DamageTypes.Log("AddDamageType: invalid arguments");
        }
        return this;
    }

    public override void HandlePacket(BinaryReader reader, int whoAmI)
    {
        int npcIndex = reader.ReadInt32();
        int colorId = reader.ReadInt32();
        bool sentry = reader.ReadBoolean();

        // The phone's host has netMode 3: it passes the packet on and also shows the number.
        if ((Main.netMode & NetmodeID.Server) != 0)
        {
            ModPacket packet = GetPacket();
            packet.Write(npcIndex);
            packet.Write(colorId);
            packet.Write(sentry);
            packet.Send(-1, whoAmI);
        }
        if ((Main.netMode & NetmodeID.MultiplayerClient) != 0 && npcIndex >= 0 && npcIndex < Main.npc.Length - 1)
            DamageNumbers.OnOthersHit(Main.npc[npcIndex], DamageTypes.ClassOfColorId(colorId), sentry);
    }
}
