using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class LargeHammerItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 42;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.LargeHammer>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.IronHammer)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();

        CreateRecipe()
            .AddIngredient(ItemID.LeadHammer)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
