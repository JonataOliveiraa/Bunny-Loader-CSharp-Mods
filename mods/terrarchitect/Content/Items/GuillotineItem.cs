using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class GuillotineItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 80;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Guillotine>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 20)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.IronBar, 1)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
