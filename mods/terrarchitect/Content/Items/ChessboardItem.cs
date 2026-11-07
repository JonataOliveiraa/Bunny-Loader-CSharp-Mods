using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class ChessboardItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 6;
        Item.maxStack = 99;
        Item.value = Item.buyPrice(0, 0, 50, 0);
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Chessboard>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.MarbleBlock)
            .AddIngredient(ItemID.Obsidian)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
