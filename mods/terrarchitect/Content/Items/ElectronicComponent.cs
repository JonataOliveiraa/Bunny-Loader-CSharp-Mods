using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class ElectronicComponent : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 24;
        Item.maxStack = 99;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.SandBlock, 1)
            .AddIngredient(ItemID.GoldOre)
            .AddIngredient(ItemID.Wire, 1)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();

        CreateRecipe()
            .AddIngredient(ItemID.SandBlock, 1)
            .AddIngredient(ItemID.Wire, 1)
            .AddIngredient(ItemID.PlatinumOre)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
