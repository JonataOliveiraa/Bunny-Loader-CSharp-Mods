using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace umamusumevanities.Content.Items.Armor.ManhattanCafe.Hair;

[AutoloadEquip(EquipType.Head)]
public class ManhattanCafeHair : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 56;
        Item.maxStack = 1;
        Item.vanity = true;
        Item.rare = ItemRarityID.Pink;
        Item.value = Item.sellPrice(0, 1, 0, 0);
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Silk, 7)
            .AddTile(TileID.Loom)
            .Register();
    }
}
