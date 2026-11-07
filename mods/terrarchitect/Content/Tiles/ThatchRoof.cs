using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class ThatchRoof : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = false;
        Main.tileLighted[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.ThatchRoofItem>());
    }
}
