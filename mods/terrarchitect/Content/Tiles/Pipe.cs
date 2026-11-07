using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class Pipe : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        TileID.Sets.DrawsWalls[Type] = true;
        Main.tileBlockLight[Type] = false;
        Main.tileLighted[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.PipeItem>());
        AddMapEntry(new Color(150, 150, 150));
    }
}
