using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class OldMinecartItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 48;
        Item.height = 38;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.OldMinecart>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Minecart, 1)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
