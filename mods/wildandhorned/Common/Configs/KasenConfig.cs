// SPDX-License-Identifier: GPL-3.0-only
// Modified on 2026-11-06: ported from JavaScript to C#.
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace WildAndHornedMod.Common.Configs;

public class KasenConfig : ModConfig
{
    public static KasenConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(true)]
    public bool BloodMoonOniForm;

    [DefaultValue(true)]
    public bool SpontaneousShifts;

    [Range(1, 60)]
    [Increment(1)]
    [DefaultValue(8)]
    public int MinutesBetweenShifts;

    [Range(10, 300)]
    [Increment(5)]
    [DefaultValue(45)]
    public int ShiftDurationSeconds;
}
