using DisableCorruptionSpreadMod.Common.Systems;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace DisableCorruptionSpreadMod.Common.Players;

public class DisableCorruptionSpreadPlayer : ModPlayer
{
    public override void OnEnterWorld()
    {
        if (Player.whoAmI != Main.myPlayer)
            return;
        BiomeSpreadState.Apply();
        if (BiomeSpreadState.CorruptionSpreadDisabled)
            return;

        Color color = Color.Orange;
        Main.NewText(Mod.GetLocalization("OnEnterWorldWarnCorruptionSpreadEnabled").Value, color.R, color.G, color.B, false);
    }
}
