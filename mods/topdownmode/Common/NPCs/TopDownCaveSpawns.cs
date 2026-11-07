using Terraria;
using Terraria.ModLoader;
using topdownmode.Common.Systems;

namespace topdownmode.Common.NPCs;

public class TopDownCaveSpawns : GlobalNPC
{
    public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
    {
        if (TopDownCave.InCave)
        {
            maxSpawns = 0;
            spawnRate = 100000000;
        }
    }
}
