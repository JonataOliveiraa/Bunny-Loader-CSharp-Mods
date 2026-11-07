using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class VerticalStripesWallpaper : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.VerticalStripesWallpaperItem>());
        AddMapEntry(new Color(224, 155, 141));
    }
}
