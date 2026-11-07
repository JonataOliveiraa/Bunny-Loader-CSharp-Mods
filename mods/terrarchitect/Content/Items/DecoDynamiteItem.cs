using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class DecoDynamiteItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 34;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.DecoDynamite>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Dynamite)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
