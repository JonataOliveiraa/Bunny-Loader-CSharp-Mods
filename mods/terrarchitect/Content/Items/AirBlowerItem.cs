using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class AirBlowerItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 26;
        Item.maxStack = 99;
        Item.value = 5;

        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.AirBlower>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Leather, 2)
            .AddRecipeGroup("Wood", 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
