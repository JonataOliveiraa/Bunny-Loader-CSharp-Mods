using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class BigBarrelItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 42;
        Item.height = 48;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.BigBarrel>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 6)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.IronBar)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
