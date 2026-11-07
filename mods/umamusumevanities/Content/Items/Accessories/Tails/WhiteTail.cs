using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace umamusumevanities.Content.Items.Accessories.Tails;

[AutoloadEquip(EquipType.Waist)]
public class WhiteTail : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 26;
        Item.height = 10;
        Item.maxStack = 1;
        Item.vanity = true;
        Item.rare = ItemRarityID.Pink;
        Item.value = Item.sellPrice(0, 0, 0, 0);
        Item.accessory = true;
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Silk, 3)
            .AddTile(TileID.Loom)
            .Register();
    }
}
