using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class LeadlightWall : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        Main.wallLight[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.LeadlightWallItem>());
        AddMapEntry(new Color(73, 110, 115));
    }
}
