using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class LargeMetalCageItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 60;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.LargeMetalCage>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.IronBar, 2)
            .AddRecipeGroup("IronBar")
            .AddIngredient(ItemID.IronFence, 8)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();

        CreateRecipe()
            .AddIngredient(ItemID.IronBar, 2)
            .AddRecipeGroup("IronBar")
            .AddIngredient(ItemID.LeadFence, 8)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
