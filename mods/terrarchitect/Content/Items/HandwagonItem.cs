using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class HandwagonItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 80;
        Item.height = 30;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Handwagon>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 8)
            .AddRecipeGroup("Wood")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
