using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class SwordWallMountItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 36;
        Item.height = 32;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.SwordWallMount>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.IronBroadsword)
            .AddIngredient(ItemID.IronBar)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();

        CreateRecipe()
            .AddIngredient(ItemID.LeadBroadsword)
            .AddIngredient(ItemID.IronBar)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
