using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class TanningRackItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 64;
        Item.height = 48;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = 1;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.TanningRack>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup("Squirrels")
            .AddRecipeGroup("Wood", 8)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();

        CreateRecipe()
            .AddIngredient(ItemID.Bunny)
            .AddRecipeGroup("Wood", 8)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
