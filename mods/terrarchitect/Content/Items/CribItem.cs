using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class CribItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 32;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Crib>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 10)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.Silk, 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
