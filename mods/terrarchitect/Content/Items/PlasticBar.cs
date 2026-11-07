using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class PlasticBar : ModItem
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
            .AddIngredient(ItemID.Gel, 5)
            .AddIngredient(ItemID.AshBlock, 5)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
