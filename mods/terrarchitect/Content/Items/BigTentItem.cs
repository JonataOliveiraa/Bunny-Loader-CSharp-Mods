using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class BigTentItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 96;
        Item.height = 68;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.BigTent>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 10)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.Silk, 10)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
