using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace TeleportKeeperMod.Common.Players;

public class TeleportPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (Player.whoAmI == Main.myPlayer)
            Teleports.EnterWorld();
    }

    public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
    {
        // Only a dedicated server has no local player. The phone's host (mode 3) has one.
        if (Player.whoAmI != Main.myPlayer || Main.netMode == 2)
            return;
        Teleports.Death(Teleports.PlayerName(Player), Player.position.X / 16f, Player.position.Y / 16f);
        Util.Say("DeathTpAdded", 255, 0, 0);
    }

    public override void PlayerDisconnect()
    {
        if (Util.IsServer)
            Teleports.DeleteDeath(Teleports.PlayerName(Player));
    }
}
