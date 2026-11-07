using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class LecternItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 32;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Lectern>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 4)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.Book)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
