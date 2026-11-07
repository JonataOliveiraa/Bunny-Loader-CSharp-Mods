using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class LibraryWallItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.maxStack = 9999;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createWall = ModContent.WallType<Walls.LibraryWall>();
    }

    public override void AddRecipes()
    {
        CreateRecipe(4)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.Book)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
