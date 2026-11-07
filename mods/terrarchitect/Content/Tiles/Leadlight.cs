using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class Leadlight : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = false;
        TileID.Sets.DrawsWalls[Type] = true;
        Main.tileLighted[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.LeadlightItem>());
    }
}
