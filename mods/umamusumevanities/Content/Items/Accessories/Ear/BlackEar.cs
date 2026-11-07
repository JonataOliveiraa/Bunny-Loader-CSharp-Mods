using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace umamusumevanities.Content.Items.Accessories.Ear;

[AutoloadEquip(EquipType.Face)]
public class BlackEar : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 16;
        Item.height = 8;
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
