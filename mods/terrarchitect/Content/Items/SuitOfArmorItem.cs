using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Content.Items;

public class SuitOfArmorItem : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 58;
        Item.maxStack = 99;
        Item.value = 5;
        Item.useTurn = true;
        Item.autoReuse = true;
        Item.useAnimation = 15;
        Item.useTime = 10;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.consumable = true;
        Item.createTile = ModContent.TileType<Tiles.SuitOfArmor>();
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.IronHelmet)
            .AddIngredient(ItemID.IronChainmail)
            .AddIngredient(ItemID.IronGreaves)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();

        CreateRecipe()
            .AddIngredient(ItemID.LeadHelmet)
            .AddIngredient(ItemID.LeadChainmail)
            .AddIngredient(ItemID.LeadGreaves)
            .AddTile(ModContent.TileType<Tiles.TerrarchitectCatalogue>())
            .Register();
    }
}
