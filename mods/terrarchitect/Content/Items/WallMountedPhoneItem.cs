using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class WallMountedPhoneItem : ModItem
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
        Item.createTile = ModContent.TileType<Tiles.WallMountedPhone>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ModContent.ItemType<PlasticBar>(), 4)
            .AddIngredient(ModContent.ItemType<ElectronicComponent>())
            .AddIngredient(ItemID.Wire)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
