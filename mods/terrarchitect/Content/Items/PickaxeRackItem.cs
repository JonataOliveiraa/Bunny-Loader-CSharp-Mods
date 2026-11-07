using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class PickaxeRackItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 56;
        Item.height = 46;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.PickaxeRackNew>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Wood, 12)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.IronPickaxe)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();

        CreateRecipe()
            .AddIngredient(ItemID.Wood, 12)
            .AddRecipeGroup("Wood")
            .AddIngredient(ItemID.LeadPickaxe)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
