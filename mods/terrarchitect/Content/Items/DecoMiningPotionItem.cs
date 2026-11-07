using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class DecoMiningPotionItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 34;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.DecoMiningPotion>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.MiningPotion)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
