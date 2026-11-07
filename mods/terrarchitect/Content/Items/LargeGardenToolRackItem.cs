using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class LargeGardenToolRackItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 80;
        Item.height = 48;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.LargeGardenToolRack>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<ShovelItem>(), 1)
            .AddIngredient(ModContent.ItemType<RakeItem>(), 1)
            .AddIngredient(ModContent.ItemType<HoeItem>(), 1)
            .AddIngredient(ModContent.ItemType<PitchforkItem>(), 1)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
