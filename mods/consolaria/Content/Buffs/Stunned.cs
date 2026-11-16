using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ModLoader;

namespace Consolaria.Content.Buffs {
    public class Stunned : ModBuff {

        public override void SetStaticDefaults() {
            Main.buffNoSave[Type] = true;
            Main.debuff[Type] = true;
        }
    }

    // Upstream sets the flag from ModBuff.Update(NPC) and clears it in ResetEffects, with an empty
    // FindFrame. On the phone each of those enters C# for every NPC on every update, so PreAI reads
    // the buff itself: the buff updates before the AI in the same frame, so the result is the same.
    internal class StunnedNPC : GlobalNPC {
        public override bool InstancePerEntity => true;

        public bool stunned;
        private Rectangle NpcFrame;

        public override void SetDefaults(NPC npc) {
            if (!stunned)
                NpcFrame = npc.frame;
            else
                npc.frame = NpcFrame;
        }

        public override bool PreAI(NPC npc) {
            stunned = HasStun(npc);
            if (stunned) {
                npc.localAI[0] = 0;
                npc.localAI[1] = 0;
                npc.ai[0] = 0;
                npc.ai[1] = 0;

                npc.velocity.X *= 0.85f;
                if (npc.noGravity == true) npc.velocity.Y *= 0.85f;

                npc.frameCounter = 0;
                //npc.noGravity = false;
                return false;
            }
            return base.PreAI(npc);
        }

        // What NPC.FindBuffIndex does, read from the NPC's arrays instead of a call into the game for every NPC.
        private static bool HasStun(NPC npc) {
            int type = ModContent.BuffType<Stunned>();
            var types = npc.buffType;
            var times = npc.buffTime;
            for (int i = 0; i < types.Length; i++) {
                if (types[i] == type && times[i] > 0)
                    return true;
            }
            return false;
        }
    }
}
