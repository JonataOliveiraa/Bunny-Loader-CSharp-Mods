using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class FruitBowlItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 28;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.FruitBowl>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.ClayBlock, 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
