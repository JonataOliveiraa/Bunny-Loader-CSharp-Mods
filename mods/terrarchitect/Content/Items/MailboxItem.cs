using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class MailboxItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 40;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.MailboxNew>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 2)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.IronBar)
            .AddRecipeGroup("IronBar")
            .AddIngredient(ModContent.ItemType<Paper>(), 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
