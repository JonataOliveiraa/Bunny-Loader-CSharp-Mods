using kineticgore.Content.Projectiles;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace kineticgore.Common;

public class GoreSystem : ModSystem
{
    const int ProjectileSync = 27;
    const int ProjectileKill = 29;

    int corpseType;
    int arrowType;

    public override void PostSetupContent()
    {
        corpseType = ModContent.ProjectileType<PhysicsCorpse>();
        arrowType = ModContent.ProjectileType<StuckArrow>();
    }

    public override void PreUpdateProjectiles()
    {
        if (!GoreShared.Visual)
            return;
        bool corpses = GoreShared.Corpses.Count > 0;
        if (!corpses && !GoreShared.Config.EnableHeadBanging)
            return;
        GoreShared.CollectHitters(corpseType, arrowType, corpses);
    }

    public override void PostUpdateDusts()
    {
        if (GoreShared.Visual)
            GoreShared.UpdateBlood();
    }

    // Corpses and stuck arrows are local: their sync and kill messages never leave the device.
    public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
    {
        int index;
        if (msgType == ProjectileSync)
            index = number;
        else if (msgType == ProjectileKill)
            index = (int)(((uint)number >> 8) & 0x3ff);
        else
            return false;
        if (index < 0 || index >= Main.maxProjectiles)
            return false;
        int type = Main.projectile[index].type;
        return type == corpseType || type == arrowType;
    }

    public override void OnWorldUnload()
    {
        GoreShared.Corpses.Clear();
        GoreShared.ClearBlood();
    }
}
