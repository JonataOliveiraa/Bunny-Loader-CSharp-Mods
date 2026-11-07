using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class Paper : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 24;
        Item.maxStack = 9999;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup("Wood")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
