using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ColoredDamageTypesMod.Common.GlobalProjectiles;

public class ProjectileChanges : GlobalProjectile
{
    public override bool InstancePerEntity => true;

    // Only the type: a game object kept in a C# field could be collected by the game.
    public int SpawnWeaponType;

    public int? ParentKind;

    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        Player owner = Main.player[projectile.owner];
        if (!owner.IsNull && owner.active)
        {
            Item held = owner.HeldItem;
            SpawnWeaponType = held.IsNull ? 0 : held.type;
        }
    }
}
