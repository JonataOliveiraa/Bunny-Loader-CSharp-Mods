using Terraria;
using Terraria.ModLoader;

namespace BossChecklistMod.Common.GlobalNPCs;

public class KillTracker : GlobalNPC
{
    public override void OnKill(NPC npc) => Util.Safe("kill", () => Fight.OnKill(npc));
}
