using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class WagonWheelsItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 30;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.WagonWheels>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 6)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.IronBar, 1)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
