using Terraria;
using Terraria.ModLoader;

public class InstantPlatformFallthrough : Mod
{
    public override void Load()
    {
        On_Player.TileCollision += (orig, self, position, velocity, fallThrough, ignorePlats) =>
        {
            bool fall = self.controlDown;
            return orig(self, position, velocity, fall, fall);
        };
    }
}
