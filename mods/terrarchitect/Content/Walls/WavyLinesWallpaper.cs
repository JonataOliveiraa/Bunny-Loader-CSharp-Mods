using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class WavyLinesWallpaper : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.WavyLinesWallpaperItem>());
        AddMapEntry(new Color(221, 148, 133));
    }
}
