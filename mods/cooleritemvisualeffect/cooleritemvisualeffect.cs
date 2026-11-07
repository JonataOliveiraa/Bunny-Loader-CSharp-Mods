using CoolerItemVisualEffectMod.Common.Players;
using Terraria;
using Terraria.ModLoader;

public class CoolerItemVisualEffectBunny : Mod
{
    public override void Load()
    {
        CoolerItemCore.AttackHookEnabled = true;

        // Runs after the whole player update, so the pose wins over the game's own swing.
        On_Player.Update += (orig, self, i) =>
        {
            orig(self, i);
            if (CoolerItemCore.AttackHookEnabled)
                CoolerItemCore.ApplyCustomAttackPose(self, i);
        };
    }

    public override void Unload()
    {
        CoolerItemCore.AttackHookEnabled = false;
        CoolerItemCore.RestoreAllNoMelee();
    }
}
