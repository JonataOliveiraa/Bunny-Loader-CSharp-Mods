using System.Collections.Generic;
using DevToolMod.Content.UI.Layers;
using DevToolMod.Core.Systems;
using Terraria.UI;

namespace DevToolMod.TL;

public class ModInterfaceLayer
{
    public string Name => GetType().Name;

    public virtual InterfaceScaleType ScaleType => InterfaceScaleType.None;

    public readonly GameInterfaceLayer Value;

    public ModInterfaceLayer()
    {
        Value = new LegacyGameInterfaceLayer("DevTool: " + Name, () =>
        {
            Draw();
            return true;
        }, ScaleType);
    }

    public virtual void SetupLayerList(List<GameInterfaceLayer> list) => list.Insert(list.Count, Value);

    public virtual void SetupContent() { }

    public virtual void Draw() { }

    public static readonly List<ModInterfaceLayer> Layers = new();

    public static void Register(ModInterfaceLayer layer) => Layers.Add(layer);

    // Runs every frame on the list the game is about to draw. Photo mode draws only its own
    // layer: every other layer, the game's included, is switched off for the frame.
    public static void SetupInterfaceLayers(List<GameInterfaceLayer> list)
    {
        bool photoMode = CameraSystem.PhotoMode;
        if (photoMode)
        {
            foreach (GameInterfaceLayer layer in list)
                layer.Active = false;
        }
        foreach (ModInterfaceLayer layer in Layers)
        {
            layer.Value.Active = !photoMode || layer is PhotoModeLayer;
            layer.SetupLayerList(list);
        }
    }

    public static void SetupAllContent()
    {
        foreach (ModInterfaceLayer layer in Layers)
            layer.SetupContent();
    }
}
