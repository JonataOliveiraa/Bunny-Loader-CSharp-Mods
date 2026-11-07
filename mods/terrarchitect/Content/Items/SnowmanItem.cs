using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class SnowmanItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 40;
        Item.height = 62;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Snowman>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.SnowBlock, 12)
            .AddIngredient(ItemID.Silk, 2)
            .AddIngredient(ItemID.Wood, 2)
            .AddRecipeGroup("Wood")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
