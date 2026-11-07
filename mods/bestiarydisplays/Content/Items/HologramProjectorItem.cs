using BestiaryDisplaysMod.Content.Tiles;
using Terraria.ID;
using Terraria.ModLoader;

namespace BestiaryDisplaysMod.Content.Items;

public class HologramProjectorItem : ModItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<HologramProjectorTile>(), 0);
        Item.width = 38;
        Item.height = 18;
        Item.value = 25000;
        Item.rare = 2;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup(RecipeGroups.IronBar, 15)
            .AddIngredient(182, 1)
            .AddIngredient(530, 3)
            .AddTile(16)
            .Register();
    }
}
