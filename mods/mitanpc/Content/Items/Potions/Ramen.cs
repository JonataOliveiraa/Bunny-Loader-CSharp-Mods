using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Items.Potions;

public class Ramen : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 32;
        Item.useTurn = true;
        Item.maxStack = 9999;
        Item.useAnimation = 100;
        Item.useTime = 100;
        Item.useStyle = ItemUseStyleID.EatFood;
        Item.UseSound = new SoundStyle("mitanpc/Assets/Sounds/RamenConsume");
        Item.consumable = true;
        Item.value = Item.buyPrice(0, 12, 0, 0);
        Item.rare = ItemRarityID.Blue;
        Item.buffType = BuffID.WellFed3;
        Item.buffTime = 60 * 60 * 8;
    }
}
