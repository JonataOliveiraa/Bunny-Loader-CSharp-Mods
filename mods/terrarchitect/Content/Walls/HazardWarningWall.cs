using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class HazardWarningWall : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.HazardWarningWallItem>());
    }
}
