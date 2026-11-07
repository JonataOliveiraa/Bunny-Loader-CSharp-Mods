using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class SecurityCameraItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.maxStack = 99;
        Item.value = 25;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = 1;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.SecurityCamera>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup("IronBar")
            .AddIngredient(ItemID.Lens)
            .AddIngredient(ModContent.ItemType<ElectronicComponent>())
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
