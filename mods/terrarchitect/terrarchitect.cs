using terrarchitect.Content.Items;
using terrarchitect.Content.Tiles;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect;

public class Terrarchitect : Mod
{
    public override void AddRecipes()
    {
        int catalogue = ModContent.TileType<TerrarchitectCatalogue>();

        Recipe.Create(ItemID.Leather)
            .AddIngredient(ItemID.Squirrel)
            .AddRecipeGroup("Squirrels")
            .AddTile(catalogue)
            .Register();

        Recipe.Create(ItemID.Leather)
            .AddIngredient(ItemID.Bunny)
            .AddTile(catalogue)
            .Register();

        Recipe.Create(ItemID.Spike, 10)
            .AddIngredient(ItemID.IronBar)
            .AddRecipeGroup("IronBar")
            .AddTile(catalogue)
            .Register();

        Recipe.Create(ItemID.Book)
            .AddIngredient(ModContent.ItemType<Paper>(), 4)
            .AddTile(catalogue)
            .Register();
    }
}
