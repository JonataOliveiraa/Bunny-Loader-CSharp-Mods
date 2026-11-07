using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace DisableCorruptionSpreadMod.Common.Systems;

public static class BiomeSpreadState
{
    public static bool CorruptionSpreadDisabled = true;

    public static void Apply()
    {
        if (CorruptionSpreadDisabled)
            WorldGen.AllowedToSpreadInfections = false;
    }

    public static void Toggle()
    {
        CorruptionSpreadDisabled = !CorruptionSpreadDisabled;
        Apply();

        string message = ModContent.GetInstance<DisableCorruptionSpread>()
            .GetLocalization(CorruptionSpreadDisabled ? "CorruptionSpreadDisabledMessage" : "CorruptionSpreadEnabledMessage").Value ?? "";
        Color color = Color.MediumVioletRed;

        if (Main.netMode == NetmodeID.SinglePlayer)
        {
            Main.NewText(message, color.R, color.G, color.B, false);
            return;
        }

        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            Main.NewText("DisableCorruptionSpread Error", color.R, color.G, color.B, false);
            return;
        }

        // Dedicated server, or a phone host (netMode 3).
        ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(message), color, -1);
        NetMessage.SendData(MessageID.WorldData, -1, -1, default, 0, 0, 0, 0, 0, 0, 0);
    }
}

public class DisableCorruptionSpreadSystem : ModSystem
{
    public override void ClearWorld()
    {
        BiomeSpreadState.CorruptionSpreadDisabled = true;
    }

    public override void PostWorldLoad()
    {
        BiomeSpreadState.Apply();
    }

    public override void SaveWorldData(TagCompound tag)
    {
        tag["CorruptionSpreadDisabled"] = BiomeSpreadState.CorruptionSpreadDisabled;
    }

    public override void LoadWorldData(TagCompound tag)
    {
        if (tag.ContainsKey("CorruptionSpreadDisabled"))
            BiomeSpreadState.CorruptionSpreadDisabled = tag.GetBool("CorruptionSpreadDisabled");
        BiomeSpreadState.Apply();
    }

    public override void NetSend(BinaryWriter writer)
    {
        writer.WriteFlags(BiomeSpreadState.CorruptionSpreadDisabled);
    }

    public override void NetReceive(BinaryReader reader)
    {
        reader.ReadFlags(out BiomeSpreadState.CorruptionSpreadDisabled);
        BiomeSpreadState.Apply();
    }
}
