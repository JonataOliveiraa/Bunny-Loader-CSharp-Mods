using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace topdownmode.Common.Configs;

public enum JumpButtonMode { Sprint, Up, None }

public enum EvilBiomeMode { Auto, Corruption, Crimson, None }

public enum WorldShapeMode { Square, Wide }

public enum TreesMode { Terraria, Blob, None }

public enum AimModeKind { Lock, Follow }

public class TopDownConfig : ModConfig
{
    public static TopDownConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("SectionGeneral")]
    [DefaultValue(true)]
    public bool Enabled;

    [Range(1f, 12f)]
    [Increment(0.5f)]
    [DefaultValue(4f)]
    public float Speed;

    [Range(0.05f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(0.3f)]
    public float Smoothing;

    [DefaultValue(JumpButtonMode.Sprint)]
    public JumpButtonMode JumpButton;

    [DefaultValue(true)]
    public bool WalkAnimation;

    [DefaultValue(true)]
    public bool NpcTopDown;

    [DefaultValue(true)]
    public bool DropsTopDown;

    [DefaultValue(true)]
    public bool FurnitureTopDown;

    [DefaultValue(true)]
    public bool ProjectilesTopDown;

    [Range(1f, 3f)]
    [Increment(0.1f)]
    [DefaultValue(1.7f)]
    public float SprintMultiplier;

    [Header("SectionWorld")]
    [DefaultValue(true)]
    public bool GenerateTopDownWorld;

    [DefaultValue(true)]
    public bool SkipVanillaWorldGen;

    [DefaultValue(EvilBiomeMode.Auto)]
    public EvilBiomeMode EvilBiome;

    [Range(0.5f, 4f)]
    [Increment(0.1f)]
    [DefaultValue(1.5f)]
    public float EvilSize;

    [DefaultValue(WorldShapeMode.Square)]
    public WorldShapeMode WorldShape;

    [DefaultValue(TreesMode.Terraria)]
    public TreesMode Trees;

    [DefaultValue(true)]
    public bool Rivers;

    [Range(0.3f, 3f)]
    [Increment(0.1f)]
    [DefaultValue(1f)]
    public float RiverDensity;

    [Range(0.5f, 2.5f)]
    [Increment(0.1f)]
    [DefaultValue(1f)]
    public float RiverWidth;

    [DefaultValue(true)]
    public bool Lakes;

    [DefaultValue(true)]
    public bool Cactus;

    [Range(0.2f, 3f)]
    [Increment(0.1f)]
    [DefaultValue(1f)]
    public float CactusDensity;

    [DefaultValue(true)]
    public bool SoftEdges;

    [Range(0.5f, 3f)]
    [Increment(0.1f)]
    [DefaultValue(1.6f)]
    public float TreeDensity;

    [DefaultValue(true)]
    public bool Decor;

    [Range(0.2f, 3f)]
    [Increment(0.1f)]
    [DefaultValue(1f)]
    public float DecorDensity;

    [DefaultValue(true)]
    public bool TreeDepth;

    [DefaultValue(true)]
    public bool NpcDepth;

    [DefaultValue(true)]
    public bool GrassDepth;

    [DefaultValue(true)]
    public bool FurnitureDepth;

    [DefaultValue(true)]
    public bool CactusDepth;

    [DefaultValue(true)]
    public bool Swim;

    [DefaultValue(true)]
    public bool WaterShimmer;

    [DefaultValue(true)]
    public bool Atmosphere;

    [DefaultValue(true)]
    public bool Shadows;

    [Range(0.2f, 2f)]
    [Increment(0.1f)]
    [DefaultValue(1f)]
    public float ShadowStrength;

    [Range(0.2f, 2f)]
    [Increment(0.1f)]
    [DefaultValue(1f)]
    public float AtmosphereStrength;

    [DefaultValue(true)]
    public bool HideHellHeat;

    [Range(0.5f, 6f)]
    [Increment(0.5f)]
    [DefaultValue(2.5f)]
    public float WaterShimmerStrength;

    [Range(0, 255)]
    [Increment(5)]
    [DefaultValue(215)]
    public int AmbientLight;

    [DefaultValue(true)]
    public bool DayNight;

    [Range(0, 255)]
    [Increment(5)]
    [DefaultValue(70)]
    public int NightLight;

    [Range(0.5f, 2.5f)]
    [Increment(0.1f)]
    [DefaultValue(1.2f)]
    public float Brightness;

    [DefaultValue(false)]
    public bool OnlyInTopDownWorlds;

    [Header("SectionDebug")]
    [DefaultValue(false)]
    public bool Noclip;

    [DefaultValue(true)]
    public bool Debug;

    [Header("SectionCombat")]
    [DefaultValue(true)]
    public bool MouseAim;

    [DefaultValue(AimModeKind.Lock)]
    public AimModeKind AimMode;

    [Range(60, 270)]
    [Increment(10)]
    [DefaultValue(150)]
    public int SwingArc;

    [DefaultValue(true)]
    public bool ArmPose;

    [Range(0.2f, 1f)]
    [Increment(0.05f)]
    [DefaultValue(0.6f)]
    public float AttackMoveMultiplier;

    [DefaultValue(true)]
    public bool CustomHitbox;

    [Range(0.25f, 2f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float HitboxDamageScale;

    [DefaultValue(false)]
    public bool ShowHitbox;
}
