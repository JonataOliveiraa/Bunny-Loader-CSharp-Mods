using Terraria.ID;
using Terraria.ModLoader;
using topdownmode.Content.Tiles;

namespace topdownmode.Content.Items;

public class TopDownCaveStairsItem : ModItem
{
    public override void SetDefaults()
    {
        Item.DefaultToPlaceableTile(ModContent.TileType<TopDownCaveStairs>());
        Item.width = 28;
        Item.height = 28;
        Item.value = 5000;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 20)
            .AddTile(TileID.WorkBenches)
            .Register();
    }
}
