using DisableCorruptionSpreadMod.Common.Systems;
using Terraria;
using Terraria.ModLoader;

public class DisableCorruptionSpread : Mod
{
    public override void Load()
    {
        On_WorldGen.UpdateWorld_OvergroundTile += (orig, i, j, checkNPCSpawns, wallDist) =>
        {
            if (BiomeSpreadState.CorruptionSpreadDisabled)
                WorldGen.AllowedToSpreadInfections = false;
            orig(i, j, checkNPCSpawns, wallDist);
        };
        On_WorldGen.UpdateWorld_UndergroundTile += (orig, i, j, checkNPCSpawns, wallDist) =>
        {
            if (BiomeSpreadState.CorruptionSpreadDisabled)
                WorldGen.AllowedToSpreadInfections = false;
            orig(i, j, checkNPCSpawns, wallDist);
        };
        BiomeSpreadState.Apply();
    }
}
