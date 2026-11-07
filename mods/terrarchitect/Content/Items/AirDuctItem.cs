using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class AirDuctItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 12;
        Item.height = 12;
        Item.maxStack = 9999;

        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.AirDuct>();
    }

    public override void AddRecipes()
    {
        CreateRecipe(4)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
