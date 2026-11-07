using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class BustItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 28;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Bust>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.MarbleBlock, 5)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
