using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class TerrarchitectCatalogueItem : ModItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>());
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = 9999;
        Item.value = 5;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup("Wood", 10)
            .AddTile(TileID.Sawmill)
            .Register();
    }
}
