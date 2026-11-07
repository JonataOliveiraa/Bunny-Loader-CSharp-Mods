using ChestPeekMod.Lib;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ChestPeekMod.Common.Systems;

// Every frame, in the update step: reads the camera and the size of the world's screen. There
// Main.screenWidth/Height follow the pinch zoom; during the draw they are the fixed interface size.
public class ChestPeekSystem : ModSystem
{
    public override void UpdateUI(GameTime gameTime)
    {
        if (Main.gameMenu)
            return;
        Vector2 cam = Main.screenPosition;
        ChestPeekPanel.ViewX = cam.X;
        ChestPeekPanel.ViewY = cam.Y;
        ChestPeekPanel.ViewW = Main.screenWidth;
        ChestPeekPanel.ViewH = Main.screenHeight;
        ChestPeekPanel.ViewOk = true;
    }
}
