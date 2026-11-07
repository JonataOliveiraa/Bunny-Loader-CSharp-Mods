using System;
using BunnyLoader;
using lightsandshadows.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace lightsandshadows.Common.Utils;

public enum LightsGradient { Day, Alternate, Night }

// What the shaders need for one frame, worked out on the CPU so the passes stay cheap.
public class LightsFrame
{
    const double ReferenceHeight = 1080;
    const int DayRays = 30;
    const int NightRays = 20;
    const double DayRayStep = (1.0 / 3.0) * (0.014 + 3 * 0.01);
    const double NightRayStep = 0.01;
    const int SkyRefreshFrames = 10;
    public const int MaxTaps = 32;

    public bool Visible;
    public bool LightOn;
    public bool BloomOn;
    public Vector2 Sun = new(0.5f, 0.8f);
    public float Clock;
    public float LightIntensity;
    public LightsGradient Gradient = LightsGradient.Day;
    public float Threshold = 1;

    // Pairs of (inverse scale, weight), read by Rays.frag as vec4[16].
    public readonly float[] Taps = new float[MaxTaps * 2];
    public int TapCount;
    public float RaysScale = 1f / 3f;
    public float BloomThreshold = 0.7f;
    public float BloomGain = 1.02f;
    public float BloomStrength = 1;
    public int BloomPasses = 4;

    double sky;
    double desertShade;
    int skyTimer;

    public void Update(LightsConfig config)
    {
        LightOn = false;
        BloomOn = false;
        Visible = false;
        if (Main.gameMenu)
            return;

        Player player = Main.LocalPlayer;
        if (player.IsNull || !player.active)
            return;

        if (config.UseLight)
            UpdateLight(player, config);
        if (config.UseBloom)
            UpdateBloom(config);
        Visible = LightOn || BloomOn;
    }

    void UpdateLight(Player player, LightsConfig config)
    {
        double surface = Main.worldSurface;
        double centerY = player.Center.Y;
        if (centerY >= surface * 16 + 800)
            return;

        if (--skyTimer <= 0)
        {
            ReadSky(player);
            skyTimer = SkyRefreshFrames;
        }

        bool day = Main.dayTime;
        double time = Main.time;
        double moon = MoonFactor(Main.moonPhase);
        double sun = 0.8 * config.LightIntensity * (1 + sky * 0.4);
        double depth = centerY / 16 - (surface - 150);
        if (depth > 0)
        {
            moon *= 1 - depth / 200;
            sun *= 1 - depth / 600;
        }

        LightOn = true;
        LightIntensity = (float)(day ? sun : 0.8 * config.MoonLightIntensity * moon);
        Clock = (float)(day ? time / 54000 : time / 32400);
        Gradient = day ? (config.DayColor == DayColorMode.Alternate ? LightsGradient.Alternate : LightsGradient.Day) : LightsGradient.Night;
        Threshold = (float)(day ? 1 - desertShade : 0.02);
        RaysScale = config.Quality switch
        {
            ShadowQuality.Medium => 1f / 4f,
            ShadowQuality.Low => 1f / 6f,
            _ => 1f / 3f,
        };
        PlaceSun(day, time, surface, player.gravDir);
        BuildTaps(day, config.ShadowIntensity, sun, config.Quality == ShadowQuality.Low);
    }

    void UpdateBloom(LightsConfig config)
    {
        float intensity = config.BloomIntensity;
        int passes = Math.Max(1, config.BloomCounts);
        BloomOn = true;
        BloomThreshold = 0.71f - intensity * 0.01f;
        BloomGain = 1 + passes * 0.005f;
        BloomStrength = intensity;
        BloomPasses = passes;
    }

    void ReadSky(Player player)
    {
        Color color = Main.ColorOfTheSkies;
        double snowLayer = Main.bgAlphaFrontLayer[7];
        double desertLayer = Main.bgAlphaFrontLayer[2];
        int snow = Main.snowBG[0];
        int desert = Main.desertBG[0];
        bool crimson = player.ZoneCrimson;

        double value = (1 - 1.2 * color.R * 0.3 + color.G * 0.6 + color.B * 0.1) / 255;
        if (player.ZoneSnow && !crimson && !player.ZoneCorrupt)
            value -= snowLayer * 0.1;
        if (crimson)
            value += 0.3;
        if (snow == 263 || snow == 258 || snow == 267)
            value -= snowLayer;
        if (snow == 263)
            value -= snowLayer * 0.5;
        if (desert == 248)
            value -= desertLayer * 0.9;

        sky = value;
        desertShade = desert == 248 ? 0 : desertLayer * 0.09;
    }

    void PlaceSun(bool day, double time, double surface, float gravity)
    {
        double half = day ? 27000 : 16200;
        double lift = -Main.screenPosition.Y / (surface * 16 - 600) * 200;
        double k = 1 - time / half;
        double down = (lift + k * k * 250 + 180) / ReferenceHeight;
        Sun.X = (float)((1 - k) / 2 - 100 * k / (ReferenceHeight * ScreenUtils.Space.Aspect));
        Sun.Y = (float)(gravity == -1 ? down : 1 - down);
    }

    void BuildTaps(bool day, double shadow, double sun, bool merge)
    {
        int rays = day ? DayRays : NightRays;
        Span<double> scales = stackalloc double[rays];
        Span<double> weights = stackalloc double[rays];
        if (day)
        {
            double factor = (shadow + 0.1) * (sun * 0.5 - 0.1);
            for (int i = 0; i < rays; i++)
            {
                double alpha = Math.Truncate(255 * 0.55 * (rays - i) / 120 * factor);
                scales[i] = 1 + i * DayRayStep;
                weights[i] = Math.Clamp(alpha, 0, 255) / 255;
            }
        }
        else
        {
            for (int j = 0; j < rays; j++)
            {
                double tint = Math.Truncate(255.0 * (rays - j) / 190) / 255;
                scales[j] = 1 + j * NightRayStep;
                weights[j] = tint * tint;
            }
        }

        float[] taps = Taps;
        int count = 0;
        int stride = merge ? 2 : 1;
        for (int i = 0; i < rays && count < MaxTaps; i += stride)
        {
            double scale = scales[i];
            double weight = weights[i];
            if (merge && i + 1 < rays)
            {
                scale = (scale + scales[i + 1]) / 2;
                weight += weights[i + 1];
            }
            if (weight <= 0)
                continue;
            taps[count * 2] = (float)(1 / scale);
            taps[count * 2 + 1] = (float)weight;
            count++;
        }
        Array.Clear(taps, count * 2, taps.Length - count * 2);
        TapCount = count;
    }

    static double MoonFactor(int phase)
    {
        if (phase == 0)
            return 1.01;
        if (phase == 3 || phase == 5)
            return 0.9;
        if (phase == 4)
            return 0.6;
        return 1;
    }
}
