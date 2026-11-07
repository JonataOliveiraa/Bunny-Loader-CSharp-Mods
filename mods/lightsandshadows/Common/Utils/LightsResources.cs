using BunnyLoader;
using Terraria.ModLoader;

namespace lightsandshadows.Common.Utils;

// Loaded on first use, from the draw, where the game's graphics are ready.
public static class LightsResources
{
    static ScreenGraphics.Shader? downsample, rays, prefilter, kawase;
    static readonly ScreenGraphics.Texture?[] gradients = new ScreenGraphics.Texture?[3];

    public static ScreenGraphics.Shader Downsample(Mod mod) => downsample ??= ScreenGraphics.Shader.FromFile(mod, "Content/Effects/Passes/Downsample.frag");

    public static ScreenGraphics.Shader Rays(Mod mod) => rays ??= ScreenGraphics.Shader.FromFile(mod, "Content/Effects/Passes/Rays.frag");

    public static ScreenGraphics.Shader Prefilter(Mod mod) => prefilter ??= ScreenGraphics.Shader.FromFile(mod, "Content/Effects/Passes/Prefilter.frag");

    public static ScreenGraphics.Shader Kawase(Mod mod) => kawase ??= ScreenGraphics.Shader.FromFile(mod, "Content/Effects/Passes/Kawase.frag");

    public static ScreenGraphics.Texture Gradient(Mod mod, LightsGradient name)
    {
        return gradients[(int)name] ??= ScreenGraphics.Texture.Load(mod, name switch
        {
            LightsGradient.Alternate => "Assets/Textures/Lights/ColorTex_Alt.png",
            LightsGradient.Night => "Assets/Textures/Lights/ColorTex2.png",
            _ => "Assets/Textures/Lights/ColorTex.png",
        });
    }
}
