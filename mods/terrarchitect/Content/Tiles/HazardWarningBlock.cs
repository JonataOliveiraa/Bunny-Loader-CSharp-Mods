using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class HazardWarningBlock : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileLavaDeath[Type] = false;
        Main.tileMergeDirt[Type] = false;
        Main.tileBlockLight[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.HazardWarningBlockItem>());
    }
}
