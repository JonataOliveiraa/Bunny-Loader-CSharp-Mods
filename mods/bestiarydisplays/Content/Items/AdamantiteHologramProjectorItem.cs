using BestiaryDisplaysMod.Content.Tiles;
using Terraria.ModLoader;

namespace BestiaryDisplaysMod.Content.Items;

public class AdamantiteHologramProjectorItem : ModItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<AdamantiteHologramProjectorTile>(), 0);
        Item.width = 38;
        Item.height = 18;
        Item.value = 50000;
        Item.rare = 4;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(391, 15)
            .AddIngredient(520, 3)
            .AddIngredient(1344, 5)
            .AddTile(134)
            .Register();
    }
}
