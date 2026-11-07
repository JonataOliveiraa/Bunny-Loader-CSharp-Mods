using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class CoffinItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 62;
        Item.height = 18;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.CoffinNew>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 4)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.Bone, 6)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
