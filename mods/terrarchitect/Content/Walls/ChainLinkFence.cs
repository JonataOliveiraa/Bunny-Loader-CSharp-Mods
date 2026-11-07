using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class ChainLinkFence : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        Main.wallLight[Type] = true;
        // The original names ChainLinkFenceItem here, an item that does not exist in the mod, so this wall drops nothing.
    }
}
