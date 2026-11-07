using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class SocketItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 64;
        Item.maxStack = 99;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.value = 5;
        Item.useStyle = 1;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.Socket>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<PlasticBar>())
            .AddIngredient(ModContent.ItemType<ElectronicComponent>())
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
