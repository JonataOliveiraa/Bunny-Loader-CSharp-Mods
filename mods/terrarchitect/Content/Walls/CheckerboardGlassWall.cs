using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class CheckerboardGlassWall : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        Main.wallLight[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.CheckerboardGlassWallItem>());
    }
}
