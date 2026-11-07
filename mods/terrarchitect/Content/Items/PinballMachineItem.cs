using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class PinballMachineItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 64;
        Item.maxStack = 99;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.value = 25;
        Item.useStyle = 1;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.PinballMachine>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddRecipeGroup("IronBar")
            .AddIngredient(ModContent.ItemType<PlasticBar>(), 4)
            .AddIngredient(ItemID.Glass, 2)
            .AddIngredient(ModContent.ItemType<ElectronicComponent>(), 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
