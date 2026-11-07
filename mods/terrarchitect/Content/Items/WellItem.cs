using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class WellItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 80;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Well>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 10)
            .AddRecipeGroup("Wood")
            .AddIngredient(ModContent.ItemType<BucketItem>(), 1)
            .AddIngredient(ItemID.Rope, 6)
            .AddIngredient(ItemID.StoneBlock, 10)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
