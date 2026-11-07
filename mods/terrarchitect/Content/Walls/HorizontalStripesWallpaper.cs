using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class HorizontalStripesWallpaper : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        Main.wallLight[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.HorizontalStripesWallpaperItem>());
        AddMapEntry(new Color(224, 155, 141));
    }
}
