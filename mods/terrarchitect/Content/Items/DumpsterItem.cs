using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class DumpsterItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 54;
        Item.height = 38;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Dumpster>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.OldShoe)
            .AddIngredient(ItemID.TinCan)
            .AddIngredient(ItemID.IronBar, 2)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
