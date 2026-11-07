using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class AshtrayItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 12;
        Item.maxStack = 99;
        Item.value = 5;

        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Ashtray>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Glass)
            .AddIngredient(ItemID.Deathweed)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
