using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class HallstandItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 64;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Hallstand>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 8)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.Silk, 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
