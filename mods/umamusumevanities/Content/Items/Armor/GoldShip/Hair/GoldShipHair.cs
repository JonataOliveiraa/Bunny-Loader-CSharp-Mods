using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace umamusumevanities.Content.Items.Armor.GoldShip.Hair;

[AutoloadEquip(EquipType.Head)]
public class GoldShipHair : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 48;
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
