using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class HazardWarningWallItem : ModItem
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
        Item.createWall = ModContent.WallType<Walls.HazardWarningWall>();
    }

    public override void AddRecipes()
    {
        CreateRecipe(4)
            .AddIngredient(ModContent.ItemType<HazardWarningBlockItem>())
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
