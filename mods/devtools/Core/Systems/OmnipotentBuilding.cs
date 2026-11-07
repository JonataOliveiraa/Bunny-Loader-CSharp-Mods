using Terraria;

namespace DevToolMod.Core.Systems;

public static class OmnipotentBuilding
{
    public static bool IsActive;

    public static int CheckUseTime(Item item)
    {
        if (!IsActive) return -1;
        int useTime = -1;
        if (item.createTile > 0 || item.createWall > 0)
        {
            useTime = item.useTime;
            item.useTime = 0;
        }
        return useTime;
    }

    public static void UpdateTileRange()
    {
        if (IsActive)
        {
            Player.tileRangeX = 512;
            Player.tileRangeY = 512;
        }
    }
}
