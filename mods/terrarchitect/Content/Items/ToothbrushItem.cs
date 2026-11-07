using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class ToothbrushItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 8;
        Item.height = 16;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Toothbrush>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.Glass)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
