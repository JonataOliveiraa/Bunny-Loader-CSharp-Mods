using DevToolMod.Core.Systems;
using DevToolMod.TL;

namespace DevToolMod.Content.UI.Layers;

public class PhotoModeLayer : ModInterfaceLayer
{
    CameraUI CameraUI = null!;

    public override void SetupContent()
    {
        CameraUI = (CameraUI)ModInterface.GetByName("CameraUI");
    }

    public override void Draw()
    {
        if (!CameraSystem.PhotoMode) return;
        CameraUI.DrawPhotoMode();
    }
}
