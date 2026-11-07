using ImprovedMovementMod.Common.Players;
using Terraria;
using Terraria.ModLoader;

public class ImprovedMovement : Mod
{
    public override void Load()
    {
        On_Player.GetLegsDrawOffset += (orig, self) =>
        {
            self.GetModPlayer<MovementPlayer>().BeforeDraw();
            return orig(self);
        };
    }
}
