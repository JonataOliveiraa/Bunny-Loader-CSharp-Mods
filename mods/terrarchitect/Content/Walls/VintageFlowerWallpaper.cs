using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class VintageFlowerWallpaper : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.VintageFlowerWallpaperItem>());
        AddMapEntry(new Color(223, 154, 155));
    }
}
