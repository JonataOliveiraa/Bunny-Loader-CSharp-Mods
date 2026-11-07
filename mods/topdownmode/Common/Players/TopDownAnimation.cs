using System;
using Microsoft.Xna.Framework;
using Terraria;
using topdownmode.Common.Configs;

namespace topdownmode.Common.Players;

// The walk animation reads only horizontal speed, so it sees the total speed while it runs.
public static class TopDownAnimation
{
    static bool enabled, hooked;

    public static void InstallAnimationHook()
    {
        enabled = true;
        if (hooked)
            return;
        On_Player.PlayerFrame += Frame;
        hooked = true;
        TopDownMode.Log("walk animation hook installed");
    }

    public static void UninstallAnimationHook() => enabled = false;

    static void Frame(On_Player.orig_PlayerFrame orig, Player self)
    {
        double vx = 0, vy = 0;
        bool swapped = false;
        if (enabled && self.whoAmI == Main.myPlayer)
        {
            TopDownConfig cfg = TopDownConfig.Instance;
            if (cfg.Enabled && cfg.WalkAnimation && TopDownPlayer.On && !self.dead)
            {
                vx = self.velocity.X;
                vy = self.velocity.Y;
                double speed = Math.Sqrt(vx * vx + vy * vy);
                self.velocity = new Vector2(speed > 0.1 ? (float)speed : 0f, 0f);
                swapped = true;
            }
        }
        try
        {
            orig(self);
        }
        finally
        {
            if (swapped)
                self.velocity = new Vector2((float)vx, (float)vy);
        }
    }
}
