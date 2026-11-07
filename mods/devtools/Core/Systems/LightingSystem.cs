using Microsoft.Xna.Framework;
using Terraria.Graphics.Light;

namespace DevToolMod.Core.Systems;

public static class LightingSystem
{
    public static bool IsActive;

    public static void Update(LightingEngine lightingEngine)
    {
        if (!IsActive) return;

        LightMap workingLightMap = lightingEngine._workingLightMap;
        workingLightMap.LightDecayThroughAir = 1;
        workingLightMap.LightDecayThroughSolid = 1;
        workingLightMap.LightDecayThroughCrackedBrick = 1;
        workingLightMap.LightDecayThroughHoney = Vector3.One;
        workingLightMap.LightDecayThroughWater = Vector3.One;
    }
}
