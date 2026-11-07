using OverhealthMod.Common.Configs;
using OverhealthMod.Common.Overhealth;
using Terraria;
using Terraria.ModLoader;

public class Overhealth : Mod
{
    public override void Load()
    {
        // Once per tick: turn reported healing into overhealth, then decay it.
        On_Player.Update += (orig, self, i) =>
        {
            orig(self, i);
            if (self.whoAmI == Main.myPlayer)
                OverhealthState.Tick(self, OverhealthConfig.Instance);
        };

        // Every kind of healing reports through HealEffect: potions, hearts, lifesteal...
        On_Player.HealEffect += (orig, self, healAmount, broadcast) =>
        {
            if (self.whoAmI == Main.myPlayer && healAmount > 0)
                OverhealthState.Healed += healAmount;
            orig(self, healAmount, broadcast);
        };

        // Damage is taken from overhealth first. The hook lends the overhealth to statLife,
        // so the game's own damage, death and immunity logic see the real total.
        On_Player.Hurt += (orig, self, damageSource, damage, hitDirection, pvp, quiet, crit, cooldownCounter, dodgeable) =>
        {
            if (self.whoAmI != Main.myPlayer || OverhealthState.Over <= 0)
                return orig(self, damageSource, damage, hitDirection, pvp, quiet, crit, cooldownCounter, dodgeable);

            self.statLife += OverhealthState.Over;
            try
            {
                return orig(self, damageSource, damage, hitDirection, pvp, quiet, crit, cooldownCounter, dodgeable);
            }
            finally
            {
                OverhealthState.TakeBackAfterHurt(self);
            }
        };

        On_Main.DrawInterface += (orig, self, gameTime) =>
        {
            orig(self, gameTime);
            OverhealthHud.Draw(OverhealthConfig.Instance);
        };
    }
}
