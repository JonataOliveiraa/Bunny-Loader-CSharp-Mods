using BetterReforgeMod.Common.UI;
using Terraria;
using Terraria.ModLoader;

public class BetterReforge : Mod
{
    public override void Load()
    {
        ModInterface.Register(new ReforgeUI());

        On_Main.UpdateUIStates += (orig, gameTime) =>
        {
            orig(gameTime);
            ModInterface.UpdateAll();
        };

        On_WorldGen.SaveAndQuit += orig =>
        {
            ModInterface.ClearAll();
            orig();
        };

        // The game's reforge page is replaced: neither method calls the original.
        On_GUIReforge.DrawReforge += (orig, self) => { };
        On_GUIReforge.Draw += (orig, self) => ReforgeUI.Instance.Draw();

        // Leaving the page gives the item in the slot back to the player.
        On_GUIPageIcons.Close += (orig, self, hideChat) =>
        {
            orig(self, hideChat);
            if (!ReforgeUI.Slot.IsNull && !ReforgeUI.Slot[0].IsAir)
                ModInterface.ClearAll();
        };
    }

    // The JavaScript version set this up in Load. In C# the game has no items that early.
    public override void PostSetupContent() => ModInterface.SetupAll();
}
