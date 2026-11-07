// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
using System;

namespace WildAndHornedMod.Common.Pets;

public static class Logic
{
    public static readonly int[] IDLE_LOOP = { 0, 1, 0, 2 };
    public static readonly (int X, int Y)[] SPACING_STEPS = { (0, 0), (0, -48), (44, 0), (44, -48), (88, -24) };

    public static (int Frame, bool Done) AnimationFrame((int Frame, int Ticks)[] frames, int timer)
    {
        foreach ((int frame, int ticks) in frames)
        {
            if (timer < ticks)
                return (frame, false);
            timer -= ticks;
        }
        return (frames[frames.Length - 1].Frame, true);
    }

    public static int WeightedIndex(float[] weights, float random)
    {
        float total = 0;
        foreach (float weight in weights)
            total += Math.Max(0, weight);
        float point = random * total;
        for (int i = 0; i < weights.Length; i++)
        {
            point -= Math.Max(0, weights[i]);
            if (point < 0)
                return i;
        }
        return Math.Max(0, weights.Length - 1);
    }

    public static int KasenBodyFrame(int frame, bool oni, int step)
    {
        int sway = IDLE_LOOP[step % 4];
        if (!oni)
            return frame == 0 ? sway : frame;
        return frame == 3 ? 11 : frame == 4 ? 12 : 8 + sway;
    }

    public static bool HidesLayers(string name, int frame) =>
        (name == "Mamizou" && frame >= 11) ||
        (name == "Suwako" && frame >= 1 && frame <= 3) ||
        (name == "Shion" && frame >= 4 && frame <= 6);

    static (float R, float G, float B) Blend((double R, double G, double B) a, (double R, double G, double B) b, double t, double pulse = 1) => (
        (float)((a.R + (b.R - a.R) * t) * pulse),
        (float)((a.G + (b.G - a.G) * t) * pulse),
        (float)((a.B + (b.B - a.B) * t) * pulse));

    static (float R, float G, float B) Scaled(double r, double g, double b, double k) => ((float)(r * k), (float)(g * k), (float)(b * k));

    public static (float R, float G, float B) LightColor(string name, uint tick, int alert = 0, int aura = 0, int glow = 0, bool day = true)
    {
        switch (name)
        {
            case "Aunn":
            {
                double t = alert / 30.0;
                return Blend((2, 1.8, 1.3), (2.3, 1.2, 0.6), t, 1 - 0.18 * t * (0.5 + 0.5 * Math.Sin(tick * 0.2)));
            }
            case "Okina":
                return Scaled(2, 1.75, 0.9, (0.9 + 0.1 * Math.Sin(tick * 0.05)) * (day ? 1 : 1.15));
            case "Kosuzu":
                return Blend((2, 1.7, 1.1), (1.6, 1.2, 2.1), aura / 40.0);
            case "Nazrin":
            {
                double t = glow / 30.0;
                return Blend((1, 1.4, 2.1), (2.2, 1.8, 0.7), t, 1 - 0.15 * t * (0.5 + 0.5 * Math.Sin(tick * 0.15)));
            }
            case "Suwako":
                return Scaled(1.6, 1.8, 0.9, 0.92 + 0.08 * Math.Sin(tick * 0.07));
            case "InuSakuya":
                return (1.6f, 1.7f, 2f);
            case "Joon":
                return Scaled(1.9, 1.55, 0.7, 0.9 + 0.1 * Math.Sin(tick * 0.11));
            default:
                return (0f, 0f, 0f);
        }
    }
}
