using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class BigWagonItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 90;
        Item.height = 46;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.BigWagon>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 12)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.IronBar, 2)
            .AddRecipeGroup("IronBar")
            .AddIngredient(ModContent.ItemType<WagonWheelsItem>(), 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
