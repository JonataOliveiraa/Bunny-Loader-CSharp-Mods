using System.Collections.Generic;
using DevToolMod.Content.UI;
using DevToolMod.Content.UI.Layers;
using DevToolMod.Core.Systems;
using DevToolMod.TL;
using Terraria.ModLoader;
using Terraria.UI;

namespace DevToolMod.Register;

public static class RegisterInterfaces
{
    public static void RegisterInterfaceLayers()
    {
        ModInterfaceLayer.Register(new MainLayer());
        ModInterfaceLayer.Register(new PostUILayer());
        ModInterfaceLayer.Register(new PhotoModeLayer());
    }

    public static void RegisterAllInterfaces()
    {
        ModInterface.Register(new DebugScreen());
        ModInterface.Register(new ToolBarUI_2());
        ModInterface.Register(new ToolBarUI());
        ModInterface.Register(new WeatherUI());
        ModInterface.Register(new CameraUI());
        ModInterface.Register(new EntityDespawnerUI());
        ModInterface.Register(new WorldIconUI());
    }
}

// What the JavaScript version did inside its Main.Initialize_AlmostEverything and
// LegacyGameInterfaceLayer.DrawSelf hooks. Textures only load once content is ready.
public class DevToolInterfaceSystem : ModSystem
{
    public override void PostSetupContent()
    {
        Textures.InitializeTextures();
        ModInterfaceLayer.SetupAllContent();
        ModInterface.SetupAllContent();
        WorldCameraHooks.Install();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) =>
        ModInterfaceLayer.SetupInterfaceLayers(layers);
}
