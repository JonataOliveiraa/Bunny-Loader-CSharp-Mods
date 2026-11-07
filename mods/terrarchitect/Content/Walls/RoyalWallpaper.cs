using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace terrarchitect.Content.Walls;

public class RoyalWallpaper : ModWall
{
    public override void SetStaticDefaults()
    {
        Main.wallHouse[Type] = true;
        RegisterItemDrop(ModContent.ItemType<Items.RoyalWallpaperItem>());
        AddMapEntry(new Color(204, 124, 59));
    }
}
