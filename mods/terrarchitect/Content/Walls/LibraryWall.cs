using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class LibraryWall : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        // The original writes this tile table at the wall's number. Kept as it is.
        Main.tileBlockLight[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.LibraryWallItem>());
    }
}
