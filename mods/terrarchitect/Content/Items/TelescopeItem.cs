using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class TelescopeItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 38;
        Item.height = 48;
        Item.maxStack = 99;
        Item.value = Item.buyPrice(0, 2, 0, 0);
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Telescope>();
    }
}
