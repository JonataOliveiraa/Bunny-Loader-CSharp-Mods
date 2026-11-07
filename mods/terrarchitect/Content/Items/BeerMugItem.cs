using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class BeerMugItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 22;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.BeerMug>();
    }
}
