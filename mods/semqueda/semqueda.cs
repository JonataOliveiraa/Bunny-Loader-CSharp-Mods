using System;
using Terraria;
using Terraria.ModLoader;

public class SemQueda : Mod
{
    public override void Load()
    {
        // Terraria measures a fall from fallStart to where the player lands.
        // Moving fallStart to the current tile every frame keeps that distance at zero.
        On_Player.Update += (orig, self, i) =>
        {
            orig(self, i);
            int tileY = (int)MathF.Floor(self.position.Y / 16f);
            self.fallStart = tileY;
            self.fallStart2 = tileY;
        };
        Logger.Info("No Fall Damage: active");
    }
}
