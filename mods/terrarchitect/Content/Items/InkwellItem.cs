using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class InkwellItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 32;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Inkwell>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Feather)
            .AddIngredient(ItemID.BlackInk)
            .AddIngredient(ItemID.Bottle)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
