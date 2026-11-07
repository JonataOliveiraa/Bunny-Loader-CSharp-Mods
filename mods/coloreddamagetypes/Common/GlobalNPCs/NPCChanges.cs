using ColoredDamageTypesMod.Common.GlobalProjectiles;
using Terraria;
using Terraria.ModLoader;

namespace ColoredDamageTypesMod.Common.GlobalNPCs;

public class NPCChanges : GlobalNPC
{
    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        if (player.whoAmI != Main.myPlayer)
            return;
        DamageNumbers.OnHit(npc, DamageTypes.GetItemType(item), item.sentry, damageDone);
    }

    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        if (projectile.owner != Main.myPlayer)
            return;
        ProjectileChanges? global = projectile.type > 0 ? projectile.GetGlobalProjectile<ProjectileChanges>() : null;
        DamageNumbers.OnHit(npc, DamageTypes.GetProjectileType(projectile, global), projectile.sentry, null);
    }
}
