using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class PileOfScrollsItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 10;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.PileOfScrolls>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<Paper>(), 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
