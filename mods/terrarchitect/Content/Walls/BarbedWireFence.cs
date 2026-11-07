using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class BarbedWireFence : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        Main.wallLight[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.BarbedWireFenceItem>());
    }
}
