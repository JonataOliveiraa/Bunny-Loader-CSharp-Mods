using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ImprovedMovementMod.Common.Configs;

public enum LookingMode { Off, Standard, Clamped }

public enum FaceCursorMode { Off, Always, UsingItem }

public enum LeaningMode { Off, Standard, Alternate, Spin }

public enum NPCLeaningMode { Standard, Spin }

public class MovementConfig : ModConfig
{
    public static MovementConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("Player")]
    [DefaultValue(LookingMode.Standard)]
    public LookingMode LookingType;

    [DefaultValue(FaceCursorMode.Always)]
    public FaceCursorMode FaceCursor;

    [DefaultValue(LeaningMode.Standard)]
    public LeaningMode LeaningType;

    [Range(0.25f, 2f)]
    [Increment(0.05f)]
    [DefaultValue(1f)]
    public float LeanStrength;

    [DefaultValue(true)]
    public bool EnableMountLeaning;

    [DefaultValue(true)]
    public bool HideNeckWhenLooking;

    [DefaultValue(false)]
    public bool DisableRopeTurning;

    [DefaultValue(true)]
    public bool EnableFlickeringFix;

    [Header("Creatures")]
    [DefaultValue(NPCLeaningMode.Standard)]
    public NPCLeaningMode NPCLeaningType;

    [DefaultValue(true)]
    public bool EnableSlimeLeaning;

    [DefaultValue(true)]
    public bool EnableZombieLeaning;

    [DefaultValue(true)]
    public bool EnableCritterLeaning;

    [DefaultValue(true)]
    public bool EnableTownNPCLeaning;

    [DefaultValue(false)]
    public bool EnableAllNPCLeaning;
}
