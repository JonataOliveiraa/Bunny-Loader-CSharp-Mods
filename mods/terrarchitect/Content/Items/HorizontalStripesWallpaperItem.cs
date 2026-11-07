using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class HorizontalStripesWallpaperItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = 9999;
        Item.value = 50;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 7;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createWall = ModContent.WallType<Walls.HorizontalStripesWallpaper>();
    }
}
