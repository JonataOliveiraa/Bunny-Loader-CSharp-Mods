using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class PipeItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 12;
        Item.height = 12;
        Item.maxStack = 9999;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = 1;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Pipe>();
    }

    public override void AddRecipes()
    {
        CreateRecipe(4)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
