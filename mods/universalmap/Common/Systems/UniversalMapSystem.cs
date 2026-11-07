using Terraria.ModLoader;

namespace UniversalMapMod.Common.Systems;

public class UniversalMapSystem : ModSystem
{
    public override void PostUpdateEverything() => Sync.Update();

    public override void OnWorldLoad()
    {
        Sync.Reset();
        MapAccess.ResetNames();
    }

    public override void OnWorldUnload() => Sync.Reset();
}
