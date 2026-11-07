using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class RoyalWallpaperItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 12;
        Item.height = 12;
        Item.maxStack = 9999;
        Item.value = 50;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 7;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createWall = ModContent.WallType<Walls.RoyalWallpaper>();
    }
}
