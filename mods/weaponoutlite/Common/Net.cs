using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WeaponOutLiteMod.Common.Players;
using WeaponOutLiteMod.Common.Poses;

namespace WeaponOutLiteMod.Common;

public static class Net
{
    const byte UpdateWeaponVisual = 3;
    const byte UpdateCombatTimer = 4;

    public static Mod? mod;

    public static void SendUpdateWeaponVisual(Player player, WeaponOutPlayer modPlayer)
    {
        if (mod == null || Main.netMode == NetmodeID.SinglePlayer)
            return;
        ModPacket packet = mod.GetPacket();
        packet.Write(UpdateWeaponVisual);
        packet.Write((byte)player.whoAmI);
        packet.Write(modPlayer.IsShowingHeldItem());
        packet.Write(modPlayer.PoseId);
        packet.Send(-1, player.whoAmI);
    }

    public static void SendUpdateCombatTimer(Player player, WeaponOutPlayer modPlayer)
    {
        if (mod == null || Main.netMode == NetmodeID.SinglePlayer)
            return;
        ModPacket packet = mod.GetPacket();
        packet.Write(UpdateCombatTimer);
        packet.Write((byte)player.whoAmI);
        packet.Write(modPlayer.CombatDelayTimer);
        packet.Send(-1, player.whoAmI);
    }

    public static void HandlePacket(BinaryReader reader)
    {
        byte code = reader.ReadByte();
        byte who = reader.ReadByte();
        Player player = Main.player[who];
        WeaponOutPlayer? modPlayer = player.IsNull ? null : WeaponOutPlayer.Of(player);
        // The phone's host has netMode 3 and dedServ false, and it is the one that passes packets on.
        bool server = (Main.netMode & NetmodeID.Server) != 0;
        if (code == UpdateWeaponVisual)
        {
            bool showing = reader.ReadBoolean();
            int pose = reader.ReadInt32();
            if (modPlayer == null)
                return;
            modPlayer.isShowingHeldItem = showing;
            modPlayer.CurrentDrawItemPose = ItemPoses.PoseOf((DrawItemPoseID)pose);
            if (server)
                SendUpdateWeaponVisual(player, modPlayer);
        }
        else if (code == UpdateCombatTimer)
        {
            int timer = reader.ReadInt32();
            if (modPlayer == null)
                return;
            modPlayer.CombatDelayTimer = timer;
            if (server)
                SendUpdateCombatTimer(player, modPlayer);
        }
    }
}
