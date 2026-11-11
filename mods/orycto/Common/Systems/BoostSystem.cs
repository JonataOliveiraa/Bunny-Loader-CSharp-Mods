using System.Collections.Generic;
using OryctoMod.Common.Configs;
using OryctoMod.Common.Lib;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace OryctoMod.Common.Systems;

public class BoostSystem : ModSystem
{
    public override void OnModLoad()
    {
        FrameLoop.Install();
        Savings.InstallLightingSkip();
        Profiler.Install();
        Savings.InstallCulling();
        Savings.InstallVisualSkips();
    }

    public override void OnWorldLoad() => FrameLoop.WorldLoaded();

    public override void OnWorldUnload() => FrameLoop.WorldUnloaded();

    public override void PostUpdateEverything()
    {
        Savings.ApplyDustCap();

        if (BoostConfig.Instance.NoParticles)
        {
            var dust = Main.dust;
            for (int i = 0; i < Main.maxDust; i++)
            {
                if (dust[i].active)
                    dust[i].active = false;
            }
        }
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index == -1)
            return;
        layers.Insert(index, new LegacyGameInterfaceLayer("Orycto: Profiler", delegate
        {
            Profiler.Frame();
            Profiler.Draw();
            return true;
        }, InterfaceScaleType.UI));
    }
}
