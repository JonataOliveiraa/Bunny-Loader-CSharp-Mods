using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class TelephoneItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 20;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Telephone>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wire, 2)
            .AddIngredient(ItemID.IronBar)
            .AddRecipeGroup("IronBar")
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
