using BunnyLoader;
using lightsandshadows.Content.Effects;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace lightsandshadows.Common.Systems;

public class LightsSystem : ModSystem
{
    static LightsEffect? effect;

    public override void PostUpdateEverything()
    {
        if (Main.netMode == NetmodeID.Server)
            return;

        if (effect == null || !effect.Active)
            effect = ScreenEffects.Spawn<LightsEffect>();
    }

    public override void OnWorldUnload()
    {
        if (effect != null && effect.Active)
            effect.Kill();
        effect = null;
    }
}
