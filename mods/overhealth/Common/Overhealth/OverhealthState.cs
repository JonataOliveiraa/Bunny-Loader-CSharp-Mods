using System;
using OverhealthMod.Common.Configs;
using Terraria;

namespace OverhealthMod.Common.Overhealth;

// Overhealth lives in its own variable instead of statLife > statLifeMax2, so the game's
// own health bar, regeneration and caps keep working untouched.
public static class OverhealthState
{
    public static int Over;             // current overhealth, on top of statLifeMax2
    public static int Healed;           // healing reported by HealEffect since the last tick
    public static float Shown;          // smoothed value for the HUD
    static int lastLife = -1;           // statLife at the end of the previous tick
    static int decayCounter;

    public static int MaxOverhealth(OverhealthConfig config, int maxLife) =>
        Math.Max(1, maxLife * config.MaximumOverhealth / 100);

    // Per-second decay: a constant part plus a part that grows with overhealth.
    static int DecayPerSecond(OverhealthConfig config, int maxLife)
    {
        int constant = (int)MathF.Floor(maxLife * (config.ConstantDecay / 10f) / 100f);
        float progressive = Over * config.ProgressiveDecay / 100f;
        float raw = config.DecayMultiplier / 10f * (constant + progressive);
        return Math.Max(1, (int)MathF.Round(raw));
    }

    public static void Tick(Player player, OverhealthConfig config)
    {
        int maxLife = player.statLifeMax2;
        if (player.dead || !config.Enabled)
        {
            Over = 0;
            Healed = 0;
            decayCounter = 0;
            lastLife = player.statLife;
            return;
        }
        if (lastLife < 0)
            lastLife = player.statLife;

        // Healing that did not fit under max life becomes overhealth.
        if (Healed > 0)
        {
            Over += Math.Max(0, lastLife + Healed - maxLife);
            Healed = 0;
        }

        Over = Math.Min(Over, MaxOverhealth(config, maxLife));

        if (Over > 0)
        {
            decayCounter += DecayPerSecond(config, maxLife);
            if (decayCounter >= 60)
            {
                Over -= Math.Min(decayCounter / 60, Over);
                decayCounter %= 60;
            }
        }
        else
        {
            decayCounter = 0;
        }
        lastLife = player.statLife;
    }

    public static void TakeBackAfterHurt(Player player)
    {
        int maxLife = player.statLifeMax2;
        if (player.statLife > maxLife)
        {
            Over = player.statLife - maxLife;
            player.statLife = maxLife;
        }
        else
        {
            Over = 0;
        }
        lastLife = player.statLife;
    }
}
