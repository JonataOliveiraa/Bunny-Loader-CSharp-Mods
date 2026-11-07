using Consolaria.Common.ModSystems;

using Terraria.GameContent.UI;
using Terraria.ModLoader;

namespace Consolaria.Content.EmoteBubbles {
    public class LepusEmote : ModEmoteBubble {
        public override void SetStaticDefaults() {
            AddToCategory(EmoteID.Category.Bosses);
        }

        public override bool IsUnlocked() {
            return DownedBossSystem.downedLepus;
        }
    }
}