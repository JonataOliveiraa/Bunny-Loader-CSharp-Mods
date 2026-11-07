using BunnyLoader;
using lightsandshadows.Common.Configs;
using lightsandshadows.Common.Utils;

namespace lightsandshadows.Content.Effects;

// The whole look in one effect: a small copy of the screen feeds the shadow rays and the
// bloom, and LightsEffect.frag adds the sunlight and the bloom over the world.
public class LightsEffect : ModScreenEffect
{
    const float SmallScale = 1f / 3f;
    const float BloomPower = 4.5f;
    const int MaxBloomPasses = 10;

    public readonly LightsFrame State = new();

    // One set per pass, kept between frames: the commands copy what they read.
    readonly ScreenDrawOptions downsampleOptions = new();
    readonly ScreenDrawOptions raysOptions = new();
    readonly ScreenDrawOptions prefilterOptions = new();
    readonly ScreenDrawOptions finalOptions = new();
    readonly ScreenDrawOptions?[] kawaseOptions = new ScreenDrawOptions?[MaxBloomPasses];
    ScreenGraphics.UniformArray? taps;

    public override void SetDefaults()
    {
        Fullscreen = true;
        Key = "lightsandshadows:main";
        Order = -1000;
    }

    public override void Update() => State.Update(LightsConfig.Instance);

    public override bool ShouldDraw() => State.Visible;

    public override void Draw(ScreenCommands cmd)
    {
        LightsFrame state = State;
        ScreenGraphics.Target small = cmd.Target("lights.small", SmallScale);
        downsampleOptions.Uniforms["uThreshold"] = state.LightOn ? state.Threshold : -1f;
        cmd.Blit(ScreenGraphics.Sources.Screen, small, LightsResources.Downsample(Mod), downsampleOptions);

        ScreenGraphics.Target rays = state.LightOn ? DrawRays(cmd, small) : small;
        ScreenGraphics.Texture gradient = LightsResources.Gradient(Mod, state.Gradient);
        ScreenGraphics.Target bloom = state.BloomOn ? DrawBloom(cmd, small, rays, gradient) : small;

        finalOptions.Textures["uGradient"] = gradient;
        finalOptions.Textures["uRays"] = rays;
        finalOptions.Textures["uBloomTexture"] = bloom;
        finalOptions.Uniforms["uBloomStrength"] = state.BloomOn ? state.BloomStrength : 0f;
        finalOptions.Uniforms["uBloomPower"] = BloomPower;
        SetLight(finalOptions);
        cmd.DrawShader(Shader, finalOptions);
    }

    void SetLight(ScreenDrawOptions options)
    {
        options.Uniforms["uSun"] = State.Sun;
        options.Uniforms["uLightOn"] = State.LightOn ? 1f : 0f;
        options.Uniforms["uClock"] = State.Clock;
        options.Uniforms["uLightIntensity"] = State.LightIntensity;
    }

    ScreenGraphics.Target DrawRays(ScreenCommands cmd, ScreenGraphics.Target small)
    {
        ScreenGraphics.Target rays = cmd.Target("lights.rays", State.RaysScale);
        raysOptions.Uniforms["uSun"] = State.Sun;
        raysOptions.Uniforms["uTapCount"] = (float)State.TapCount;
        raysOptions.Uniforms["uTaps"] = taps ??= new ScreenGraphics.UniformArray(4, State.Taps);
        cmd.Blit(small, rays, LightsResources.Rays(Mod), raysOptions);
        return rays;
    }

    ScreenGraphics.Target DrawBloom(ScreenCommands cmd, ScreenGraphics.Target small, ScreenGraphics.Target rays, ScreenGraphics.Target gradient)
    {
        LightsFrame state = State;
        ScreenGraphics.Target from = cmd.Target("lights.bloom.a", SmallScale);
        ScreenGraphics.Target to = cmd.Target("lights.bloom.b", SmallScale);
        prefilterOptions.Textures["uGradient"] = gradient;
        prefilterOptions.Textures["uRays"] = rays;
        prefilterOptions.Uniforms["uThreshold"] = state.BloomThreshold;
        prefilterOptions.Uniforms["uGain"] = state.BloomGain;
        SetLight(prefilterOptions);
        cmd.Blit(small, from, LightsResources.Prefilter(Mod), prefilterOptions);

        ScreenGraphics.Shader kawase = LightsResources.Kawase(Mod);
        for (int i = 1; i < state.BloomPasses && i < MaxBloomPasses; i++)
        {
            ScreenDrawOptions options = kawaseOptions[i] ??= new ScreenDrawOptions();
            options.Uniforms["uDistance"] = (float)i;
            options.Uniforms["uGain"] = state.BloomGain;
            cmd.Blit(from, to, kawase, options);
            (from, to) = (to, from);
        }
        return from;
    }
}
