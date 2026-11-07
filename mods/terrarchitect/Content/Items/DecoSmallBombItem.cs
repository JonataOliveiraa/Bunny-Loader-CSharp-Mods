using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class DecoSmallBombItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 18;
        Item.height = 26;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.DecoSmallBomb>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Bomb)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
