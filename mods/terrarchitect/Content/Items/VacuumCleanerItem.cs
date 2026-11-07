using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class VacuumCleanerItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 26;
        Item.maxStack = 99;
        Item.value = 25;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = 1;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.VacuumCleaner>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<PlasticBar>(), 6)
            .AddIngredient(ModContent.ItemType<ElectronicComponent>())
            .AddIngredient(ItemID.Silk)
            .AddIngredient(ItemID.DirtBlock)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
