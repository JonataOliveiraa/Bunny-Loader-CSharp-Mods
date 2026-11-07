using Terraria.ModLoader;

namespace ColoredDamageTypesMod.Common.Systems;

public class DamageNumbersSystem : ModSystem
{
    public override void PostSetupContent() => DamageTypes.LoadModdedDamageTypes();

    public override void PostUpdateProjectiles() => DamageNumbers.Flush();

    public override void PostUpdateEverything()
    {
        DamageNumbers.Flush();
        DamageTypes.SyncHeldClass();
    }

    public override void ClearWorld() => DamageNumbers.Clear();

    public override void OnWorldUnload() => DamageNumbers.Clear();
}
