using mitanpc.Common.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace mitanpc.Content.Items.PermanentBoosters;

public class MartialArtsManual : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 34;
        Item.useTurn = true;
        Item.maxStack = 9999;
        Item.useAnimation = 50;
        Item.useTime = 50;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.UseSound = SoundID.Item92;
        Item.consumable = true;
        Item.value = Item.buyPrice(0, 15, 0, 0);
        Item.rare = ItemRarityID.Blue;
    }

    public override bool CanUseItem(Player player) => !player.GetModPlayer<MitaNPCPlayer>().martialArtsManualUsed;

    public override bool? UseItem(Player player)
    {
        MitaNPCPlayer state = player.GetModPlayer<MitaNPCPlayer>();
        if (state.martialArtsManualUsed)
            return false;
        state.martialArtsManualUsed = true;
        return true;
    }
}
