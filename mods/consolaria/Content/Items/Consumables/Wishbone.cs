using Consolaria.Common;

using Microsoft.Xna.Framework;

using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Consolaria.Content.Items.Consumables {
    public class Wishbone : ModItem {
        public override void SetStaticDefaults() {
            Item.ResearchUnlockCount = 3;
        }

        public override void SetDefaults() {
            int width = 30; int height = 22;
            Item.width = width; Item.height = height;

            Item.maxStack = 9999;
            Item.UseSound = SoundID.NPCHit2;

            Item.value = Item.sellPrice(gold: 3);
            Item.rare = ItemRarityID.Orange;

            Item.useAnimation = 18;
            Item.useTime = 18;

            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.consumable = true;
        }

        public override bool CanUseItem(Player player)
            => !SeasonalEvents.allEventsForToday;

        public override bool? UseItem(Player player) {
            string text = "The spirits of celebration arise...";
            if (player.whoAmI == Main.myPlayer && player.itemAnimation >= player.itemAnimationMax) {
                SeasonalEvents.allEventsForToday = true;
                if (Main.netMode == NetmodeID.SinglePlayer)
                    Main.NewText(text, Color.HotPink);
                else if ((Main.netMode & NetmodeID.Server) != 0)
                    ChatHelper.BroadcastChatMessage(NetworkText.FromKey(text), Color.HotPink);
                if ((Main.netMode & NetmodeID.Server) != 0)
                    NetMessage.SendData(MessageID.WorldData);
            }
            return true;
        }
    }
}