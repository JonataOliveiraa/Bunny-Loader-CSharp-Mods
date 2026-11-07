using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class AirDuct : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = true;
        Main.tileLighted[Type] = true;

        // AddMapEntry(new Color(150, 150, 150));

        RegisterItemDrop(ModContent.ItemType<Items.AirDuctItem>());
    }
}
