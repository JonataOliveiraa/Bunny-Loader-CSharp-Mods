using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Items.Potions;

public class Carrot : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 32;
        Item.height = 30;
        Item.useTurn = true;
        Item.maxStack = 9999;
        Item.useAnimation = 50;
        Item.useTime = 50;
        Item.useStyle = ItemUseStyleID.EatFood;
        Item.UseSound = new SoundStyle("mitanpc/Assets/Sounds/CarrotConsume");
        Item.consumable = true;
        Item.value = Item.buyPrice(0, 0, 25, 0);
        Item.rare = ItemRarityID.Blue;
        Item.buffType = BuffID.WellFed2;
        Item.buffTime = 60 * 180;
    }
}
