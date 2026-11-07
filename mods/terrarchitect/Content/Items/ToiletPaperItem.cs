using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class ToiletPaperItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 16;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.ToiletPaper>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Silk, 1)
            .AddIngredient(ModContent.ItemType<Paper>(), 1)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
