using mitanpc.Common.Players;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Items.Accessories;

public class TravelRing : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 20;
        Item.rare = ItemRarityID.Blue;
        Item.accessory = true;
        Item.value = Item.buyPrice(1, 25, 0, 0);
    }

    public override void SetStaticDefaults()
    {
        ItemID.Sets.AnimatesAsSoul[Type] = true;
        Main.RegisterItemAnimation(Type, new DrawAnimationVertical(12, 2));
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<MitaNPCPlayer>().travelRing = true;
        player.moveSpeed += 0.1f;
    }
}
