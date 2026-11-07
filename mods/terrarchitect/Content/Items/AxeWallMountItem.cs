using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class AxeWallMountItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 32;
        Item.maxStack = 99;
        Item.value = 5;

        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.AxeWallMount>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.LeadAxe)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();

        CreateRecipe()
            .AddIngredient(ItemID.IronAxe)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
