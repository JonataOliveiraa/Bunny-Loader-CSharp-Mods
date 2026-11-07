using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class BlueRoofTile : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileLavaDeath[Type] = true;
        Main.tileMergeDirt[Type] = false;
        Main.tileBlockLight[Type] = false;
        RegisterItemDrop(ModContent.ItemType<Items.BlueRoofTileItem>());
    }
}
