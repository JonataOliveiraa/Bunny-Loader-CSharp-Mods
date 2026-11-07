using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class ButcherTableItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 64;
        Item.height = 46;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.ButcherTable>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 10)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.Bunny)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
