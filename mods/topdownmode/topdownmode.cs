using System;
using Terraria.ModLoader;
using topdownmode.Common.NPCs;
using topdownmode.Common.Players;
using topdownmode.Common.Systems;

public class TopDownMode : Mod
{
    static TopDownMode? instance;

    public static void Log(string message) => instance?.Logger.Info(message);

    public override void Load()
    {
        instance = this;
        TopDownCombat.InstallCombatHook();
        TopDownAnimation.InstallAnimationHook();
        Guard("cactus", TopDownCactus.InstallCactus);
        TopDownTreeDepth.InstallTreeDepth();
        TopDownPlayer.InstallNoStep();
        TopDownNoHeat.InstallNoHeat();
        TopDownNPC.InstallNpcTopDown();
        TopDownDrops.InstallDropsTopDown();
        Guard("furniture", TopDownFurniture.InstallFurniture);
        Guard("projectiles", TopDownProjectiles.InstallProjectiles);
        Guard("cave", TopDownCave.InstallCave);
        Log("v0.31.0 loaded. Options: pause > Mod Configuration.");
    }

    public override void Unload()
    {
        TopDownCombat.UninstallCombatHook();
        TopDownAnimation.UninstallAnimationHook();
        TopDownTreeDepth.UninstallTreeDepth();
        TopDownCactus.UninstallCactus();
        TopDownPlayer.UninstallNoStep();
        TopDownNoHeat.UninstallNoHeat();
        TopDownNPC.UninstallNpcTopDown();
        TopDownDrops.UninstallDropsTopDown();
        TopDownFurniture.UninstallFurniture();
        TopDownProjectiles.UninstallProjectiles();
        TopDownCave.UninstallCave();
        Log("Unloaded.");
    }

    static void Guard(string what, Action install)
    {
        try
        {
            install();
        }
        catch (Exception e)
        {
            Log(what + " (install error): " + e);
        }
    }
}
