using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class MortarAndPestleItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 14;
        Item.height = 24;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.MortarAndPestle>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.StoneBlock, 2)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
