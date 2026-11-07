using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class BackpackItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 44;
        Item.height = 40;
        Item.maxStack = 99;
        Item.value = Item.buyPrice(0, 1, 0, 0);

        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;

        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Backpack>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Leather, 4)
            .AddIngredient(ItemID.Silk, 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
