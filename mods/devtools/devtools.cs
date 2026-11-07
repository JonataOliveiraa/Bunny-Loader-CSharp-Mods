using DevToolMod.Content.UI;
using DevToolMod.Core.Systems;
using DevToolMod.Register;
using DevToolMod.TL;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Light;
using Terraria.ModLoader;

public class DevTool : Mod
{
    public static DevTool Instance = null!;

    public override void Load()
    {
        Instance = this;
        RegisterAll.Run();
        Logger.Info("DevTool: loaded (Bunny Loader)");

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

        On_Player.Update += (orig, self, i) =>
        {
            if (i != Main.myPlayer)
            {
                orig(self, i);
                return;
            }

            MagnetSystem.UpdatePlayer(self);

            Item item = self.inventory[self.selectedItem];
            int useTime = OmnipotentBuilding.CheckUseTime(item);
            orig(self, i);
            if (useTime != -1) item.useTime = useTime;
        };

        On_Player.ResetEffects += (orig, self) =>
        {
            orig(self);
            OmnipotentBuilding.UpdateTileRange();
        };

        On_Player.IsInTileInteractionRange += (orig, self, targetX, targetY, settings, tb) =>
        {
            bool result = orig(self, targetX, targetY, settings, tb);
            if (OmnipotentBuilding.IsActive) return true;
            return result;
        };

        On_Player.BordersMovement += (orig, self) =>
        {
            orig(self);
            if (CameraSystem.CameraMode == CameraMode.Free)
            {
                self.velocity = Vector2.Zero;
                self.position = self.oldPosition;
            }
        };

        On_Player.UpdateEquips += (orig, self, i) =>
        {
            orig(self, i);
            if (i == Main.myPlayer) PlayerSystem.Update(self);
        };

        On_Main.DoDraw_UpdateCameraPosition += orig =>
        {
            if (CameraSystem.CameraMode == CameraMode.Default)
                orig();
            CameraSystem.UpdateCamera(Main.LocalPlayer);
        };

        On_Main.DrawNPCs += (orig, self, behindTiles) =>
        {
            if (!behindTiles)
            {
                WorldIconUI.DrawIcons();
                MagnetSystem.DrawMagnet();
            }
            orig(self, behindTiles);
            HitboxSystem.DrawNPCs(behindTiles);
        };

        On_Main.DrawProjDirect += (orig, self, proj, player) =>
        {
            orig(self, proj, player);
            HitboxSystem.DrawProj(proj);
        };

        On_LightingEngine.UpdateLightDecay += (orig, self) =>
        {
            orig(self);
            if (LightingSystem.IsActive)
                LightingSystem.Update(self);
        };
    }
}
