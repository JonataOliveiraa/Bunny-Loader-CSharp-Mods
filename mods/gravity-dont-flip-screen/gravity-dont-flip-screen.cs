using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Renderers;
using Terraria.ModLoader;

public class GravityDontFlipScreen : Mod
{
    static float gravDir = 1;
    static float? gravDir2;

    public override void Load()
    {
        On_Player.ChestChangeEvents += (orig, self) =>
        {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1)
            {
                orig(self);
                return;
            }

            self.gravDir = 1;
            orig(self);
            gravDir2 = -1;
        };

        // As in the JavaScript version, the game's method does not run for the local player.
        On_Player.Update_AdjustTileTargetForDisplayJars += (orig, self, i) =>
        {
            if (i != Main.myPlayer)
            {
                orig(self, i);
                return;
            }

            if (gravDir2 != null)
            {
                self.gravDir = gravDir2.Value;
                gravDir2 = null;
            }
        };

        On_Player.SmartInteractLookup += (orig, self) =>
        {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1)
            {
                orig(self);
                return;
            }

            self.gravDir = 1;
            orig(self);
            self.gravDir = -1;
        };

        On_SmartCursorHelper.SmartCursorLookup += (orig, player) =>
        {
            if (player.whoAmI != Main.myPlayer || player.gravDir != -1)
            {
                orig(player);
                return;
            }

            player.gravDir = 1;
            orig(player);
            player.gravDir = -1;
        };

        On_Player.ItemCheck += (orig, self) =>
        {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1)
            {
                orig(self);
                return;
            }

            int mouseY = Main.mouseY;
            Main.mouseY = Main.screenHeight - Main.mouseY;
            orig(self);
            Main.mouseY = mouseY;
        };

        On_Player.QuickGrapple += (orig, self) =>
        {
            if (self.whoAmI != Main.myPlayer || self.gravDir != -1)
            {
                orig(self);
                return;
            }

            self.gravDir = 1;
            orig(self);
            self.gravDir = -1;
        };

        On_LegacyPlayerRenderer.DrawPlayerFull += (orig, self, camera, player) =>
        {
            if (player.whoAmI != Main.myPlayer)
            {
                orig(self, camera, player);
                return;
            }

            player.gravDir = gravDir;
            orig(self, camera, player);
            player.gravDir = 1;
        };

        On_Main.DoDraw += (orig, self, gameTime) =>
        {
            if (Main.gameMenu)
            {
                orig(self, gameTime);
                return;
            }

            Player player = Main.LocalPlayer;
            gravDir = player.gravDir;
            player.gravDir = 1;
            orig(self, gameTime);
            player.gravDir = gravDir;
        };
    }
}
