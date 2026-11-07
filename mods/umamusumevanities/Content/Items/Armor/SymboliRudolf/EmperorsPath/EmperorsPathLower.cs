using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace umamusumevanities.Content.Items.Armor.SymboliRudolf.EmperorsPath;

[AutoloadEquip(EquipType.Legs)]
public class EmperorsPathLower : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 22;
        Item.height = 18;
        Item.maxStack = 1;
        Item.vanity = true;
        Item.rare = ItemRarityID.Pink;
        Item.value = Item.sellPrice(0, 0, 0, 0);
    }

    public override void AddRecipes()
    {
        CreateRecipe()
            .AddIngredient(ItemID.Silk, 7)
            .AddTile(TileID.Loom)
            .Register();
    }
}
