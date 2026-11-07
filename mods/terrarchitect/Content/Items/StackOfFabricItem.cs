using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class StackOfFabricItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 30;
        Item.height = 14;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.StackOfFabric>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Silk, 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
