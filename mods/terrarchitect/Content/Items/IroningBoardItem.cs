using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class IroningBoardItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 26;
        Item.maxStack = 99;
        Item.value = 25;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = 1;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.IroningBoard>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup("Wood", 4)
            .AddIngredient(ItemID.Silk, 2)
            .AddIngredient(ModContent.ItemType<PlasticBar>())
            .AddIngredient(ModContent.ItemType<ElectronicComponent>())
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
