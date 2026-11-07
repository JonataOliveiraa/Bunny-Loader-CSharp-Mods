using System;
using umamusumevanities.Common.Configs;

namespace umamusumevanities.Common.Effects;

public sealed class VFXOptions
{
    public int level = 2;
    public int amount = 100;
    public bool glow = true;
}

public static class UmamusumeVFXSettings
{
    static readonly VFXOptions current = new();

    public static VFXOptions GetVFX()
    {
        UmamusumeEffectsConfig? cfg = UmamusumeEffectsConfig.Instance;
        if (cfg == null)
        {
            current.level = 2;
            current.amount = 100;
            current.glow = true;
            return current;
        }
        current.level = (int)cfg.Quality;
        current.amount = Math.Clamp(cfg.Amount, 0, 200);
        current.glow = cfg.ExtraGlow && cfg.Quality != EffectQuality.Economy;
        return current;
    }

    static int Round(double value) => (int)Math.Floor(value + 0.5);

    public static int DensityInterval(int baseValue, int amount) => Math.Max(1, Round(baseValue * 100.0 / Math.Max(25, amount)));

    public static int DensityCount(int baseValue, int amount, int cap)
    {
        if (amount <= 0)
            return 0;
        return Math.Min(cap, Math.Max(1, Round(baseValue * amount / 100.0)));
    }
}
