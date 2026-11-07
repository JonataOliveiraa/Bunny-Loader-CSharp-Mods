using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class DecoHellstone : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = true;
        Main.tileLighted[Type] = true;
        Main.tileMergeDirt[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.DecoHellstoneItem>());
    }

    public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
    {
        r = 0.15f;
        g = 0f;
        b = 0f;
    }
}
