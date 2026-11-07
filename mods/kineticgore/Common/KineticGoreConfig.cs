using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace kineticgore.Common;

public class KineticGoreConfig : ModConfig
{
    public static KineticGoreConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("QualityOfLife")]
    [DefaultValue(false)]
    public bool ArrowsStickToEnemies;

    [DefaultValue(false)]
    public bool EnableHeadBanging;

    [Header("GeneralSettings")]
    // Seconds.
    [Range(1, 60)]
    [Increment(1)]
    [DefaultValue(20)]
    public int CorpseDuration;

    // Percent.
    [Range(0, 100)]
    [Increment(1)]
    [DefaultValue(30)]
    public int DismembermentChance;

    [DefaultValue(false)]
    public bool LegacyRagdollPhysics;

    [DefaultValue(true)]
    public bool EnableRagdollCollision;

    [Range(5, 150)]
    [Increment(5)]
    [DefaultValue(40)]
    public int MaxCorpses;

    [Header("BloodSettings")]
    [Range(0f, 5f)]
    [Increment(0.1f)]
    [DefaultValue(0.7f)]
    public float BloodAmountMultiplier;

    [Range(0f, 5f)]
    [Increment(0.1f)]
    [DefaultValue(0.4f)]
    public float GibAmountMultiplier;

    [Range(0.1f, 2f)]
    [Increment(0.1f)]
    [DefaultValue(0.7f)]
    public float BloodScale;

    [Range(0.1f, 3f)]
    [Increment(0.1f)]
    [DefaultValue(1.3f)]
    public float BloodVelocity;

    [DefaultValue(true)]
    public bool EnableStickyBlood;

    // Seconds.
    [Range(0.5f, 10f)]
    [Increment(0.5f)]
    [DefaultValue(1f)]
    public float StickyBloodDuration;

    // Seconds.
    [Range(0f, 10f)]
    [Increment(0.5f)]
    [DefaultValue(2.5f)]
    public float BleedDuration;

    [Header("PhysicsSettings")]
    [Range(0.05f, 2f)]
    [Increment(0.05f)]
    [DefaultValue(0.35f)]
    public float Gravity;

    [Range(0f, 1.5f)]
    [Increment(0.05f)]
    [DefaultValue(0.3f)]
    public float Bounciness;

    [Range(0.1f, 1f)]
    [Increment(0.02f)]
    [DefaultValue(0.8f)]
    public float Friction;

    [Range(0.1f, 5f)]
    [Increment(0.1f)]
    [DefaultValue(0.8f)]
    public float KnockbackMultiplier;

    [Header("HeadPhysics")]
    [Range(0f, 5f)]
    [Increment(0.1f)]
    [DefaultValue(1f)]
    public float HeadBobIntensity;

    [DefaultValue(true)]
    public bool EnableHeadFlinching;

    [DefaultValue(false)]
    public bool ReverseHeadFlinch;

    [Range(0.01f, 0.5f)]
    [Increment(0.01f)]
    [DefaultValue(0.08f)]
    public float HeadSpringStiffness;

    [Range(0.1f, 0.99f)]
    [Increment(0.01f)]
    [DefaultValue(0.85f)]
    public float HeadSpringDamping;

    [Header("SlimePhysics")]
    [Range(0f, 5f)]
    [Increment(0.1f)]
    [DefaultValue(1f)]
    public float SlimeJiggleIntensity;

    [Range(0.01f, 0.5f)]
    [Increment(0.01f)]
    [DefaultValue(0.1f)]
    public float SlimeSpringStiffness;

    [Range(0.1f, 0.99f)]
    [Increment(0.01f)]
    [DefaultValue(0.85f)]
    public float SlimeSpringDamping;

    [Header("VisualAdjustments")]
    [Range(-20, 20)]
    [Increment(1)]
    [DefaultValue(-4)]
    public int HumanoidVerticalOffset;
}
