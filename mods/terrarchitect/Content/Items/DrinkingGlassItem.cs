using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class DrinkingGlassItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 10;
        Item.height = 12;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.DrinkingGlass>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Glass, 1)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
