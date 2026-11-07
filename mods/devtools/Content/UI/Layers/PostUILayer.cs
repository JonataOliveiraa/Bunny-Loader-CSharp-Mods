using DevToolMod.TL;

namespace DevToolMod.Content.UI.Layers;

public class PostUILayer : ModInterfaceLayer
{
    ModInterface DebugScreen = null!;

    public override void SetupContent()
    {
        DebugScreen = ModInterface.GetByName("DebugScreen");
    }

    public override void Draw() => DebugScreen.Draw();
}
