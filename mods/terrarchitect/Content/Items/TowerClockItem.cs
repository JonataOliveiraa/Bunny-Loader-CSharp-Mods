using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class TowerClockItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 60;
        Item.height = 60;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.TowerClock>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Cog, 4)
            .AddIngredient(ItemID.Wire, 2)
            .AddIngredient(ItemID.Timer1Second)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
