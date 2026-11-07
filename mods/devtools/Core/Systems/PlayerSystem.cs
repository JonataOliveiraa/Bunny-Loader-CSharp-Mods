using Terraria;

namespace DevToolMod.Core.Systems;

public static class PlayerSystem
{
    public static bool InfiniteMinions;

    public static void Update(Player player)
    {
        if (InfiniteMinions)
        {
            player.maxMinions = 1000;
            player.maxTurrets = 1000;
        }
    }
}
