using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class RhinoHuntingTrophyItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 42;
        Item.maxStack = 99;
        Item.value = Item.buyPrice(0, 1, 0, 0);
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.RhinoHuntingTrophy>();
    }
}
