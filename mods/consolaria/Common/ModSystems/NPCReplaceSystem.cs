using Consolaria.Content.NPCs;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Common.ModSystems {
    // Upstream hooks NPC.NewNPC in each of these five NPCs. On the phone natural spawns go through
    // NPC.Spawner, so the replacement sits there, as in the phone's package, and only runs on a spawn.
    public class NPCReplaceSystem : ModSystem {
        public override void Load() {
            On_NPC_Spawner.SpawnNPC_int_int_int_int_float_float_float_float_int += SpawnNPC;
        }

        private static NPC SpawnNPC(On_NPC_Spawner.orig_SpawnNPC_int_int_int_int_float_float_float_float_int orig, NPC.Spawner self, int X, int Y, int Type, int Start, float ai0, float ai1, float ai2, float ai3, int Target) {
            return orig(self, X, Y, GetTypeLookup(Type), Start, ai0, ai1, ai2, ai3, Target);
        }

        private static int GetTypeLookup(int type) {
            switch (type) {
                case NPCID.Antlion:
                    return Main.rand.NextBool(20) ? ModContent.NPCType<AlbinoAntlion>() : type;
                case NPCID.WalkingAntlion:
                    return Main.rand.NextBool(20) ? ModContent.NPCType<AlbinoCharger>() : type;
                case NPCID.FlyingAntlion:
                    return Main.rand.NextBool(20) ? ModContent.NPCType<AlbinoSwarmer>() : type;
                case NPCID.GiantWalkingAntlion:
                    return Main.rand.NextBool(20) ? ModContent.NPCType<GiantAlbinoCharger>() : type;
                case NPCID.GiantFlyingAntlion:
                    return Main.rand.NextBool(20) ? ModContent.NPCType<GiantAlbinoSwarmer>() : type;
                default:
                    return type;
            }
        }
    }
}
