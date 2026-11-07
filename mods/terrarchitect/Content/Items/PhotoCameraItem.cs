using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class PhotoCameraItem : ModItem
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
        Item.createTile = ModContent.TileType<Tiles.PhotoCamera>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<PlasticBar>(), 2)
            .AddIngredient(ModContent.ItemType<ElectronicComponent>(), 2)
            .AddIngredient(ItemID.Lens)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
