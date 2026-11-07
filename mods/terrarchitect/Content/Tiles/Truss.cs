using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Tiles;

public class Truss : ModTile
{
    public override void SetStaticDefaults()
    {
        Main.tileSolid[Type] = true;
        Main.tileBlockLight[Type] = false;
        TileID.Sets.DrawsWalls[Type] = true;
        Main.tileLighted[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.TrussItem>());
        AddMapEntry(new Color(150, 150, 150));
    }
}
