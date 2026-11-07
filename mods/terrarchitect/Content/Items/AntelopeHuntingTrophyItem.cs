using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class AntelopeHuntingTrophyItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 64;
        Item.maxStack = 99;
        Item.value = Item.buyPrice(0, 1, 0, 0);

        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.AntelopeHuntingTrophy>();
    }
}
