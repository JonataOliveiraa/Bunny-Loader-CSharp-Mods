using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class LeadlightWallItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 12;
        Item.height = 12;
        Item.maxStack = 999;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 7;
        Item.useStyle = 1;
        Item.consumable = true;
        Item.value = 50;
        Item.createWall = ModContent.WallType<Walls.LeadlightWall>();
    }

    public override void AddRecipes()
    {
        CreateRecipe(4)
            .AddIngredient(ModContent.ItemType<LeadlightItem>())
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
