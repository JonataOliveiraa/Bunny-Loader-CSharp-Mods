using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class RoundPaneGlass : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileLavaDeath[Type] = false;
        Main.tileMergeDirt[Type] = false;
        TileID.Sets.DrawsWalls[Type] = true;
        Main.tileBlockLight[Type] = false;
        RegisterItemDrop(ModContent.ItemType<Items.RoundPaneGlassItem>());
    }
}
