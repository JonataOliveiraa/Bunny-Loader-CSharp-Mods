using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace OryctoMod.Common.Configs;

public class BoostConfig : ModConfig
{
    public static BoostConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("FrameRate")]
    [DefaultValue(true)]
    public bool UnlockFps;

    [DefaultValue(false)]
    public bool UnlockMenu;

    [OptionStrings(new[] { "60", "90", "120", "144" })]
    [DefaultValue("120")]
    public string Fps = "120";

    [DefaultValue(true)]
    public bool Smooth;

    [DefaultValue(true)]
    public bool Enemies;

    [DefaultValue(false)]
    public bool Projectiles;

    [Header("Performance")]
    [DefaultValue(true)]
    public bool SkipLighting;

    [DefaultValue(true)]
    public bool CullEntities;

    // No effect, as in the JavaScript version: it hooked Main.UpdateNPC, which the game
    // does not have. See the note in Savings.
    [DefaultValue(true)]
    public bool ThrottleFarEntities;

    // No effect, for the same reason as ThrottleFarEntities.
    [DefaultValue(true)]
    public bool ThrottleCritters;

    [DefaultValue(true)]
    public bool ThrottleMinimap;

    [Range(500, 6000)]
    [Increment(250)]
    [DefaultValue(6000)]
    public int DustCap;

    [DefaultValue(false)]
    public bool ThrottleLiquid;

    // No effect, as in the JavaScript version: it hooked Wiring.Update, which the game
    // does not have. See the note in Savings.
    [DefaultValue(false)]
    public bool ThrottleWiring;

    [Header("Visuals")]
    [DefaultValue(false)]
    public bool NoRain;

    [DefaultValue(false)]
    public bool NoParticles;

    [Header("Debug")]
    [DefaultValue(false)]
    public bool Profiler;
}
