using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace NPCUnlockAnnouncerMod.Common.Configs;

public enum BannerStyle { Classic, Modern, Corruption, Crimson, Wood, Jungle }

public enum BannerSound { None, MagicDing, Coins, MaxMana, ResearchComplete, ChestUnlock, Crimson, Knock, Jungle, Corruption }

public enum AnnounceFrequency { FirstTimeOnly, EveryTime, SetAmount }

public class AnnouncerConfig : ModConfig
{
    public static AnnouncerConfig Instance = null!;

    public override ConfigScope Mode => ConfigScope.ClientSide;

    [Header("General")]
    [Dropdown]
    [DefaultValue(BannerStyle.Classic)]
    public BannerStyle Style;

    [Dropdown]
    [DefaultValue(BannerSound.MagicDing)]
    public BannerSound Sound;

    [Range(0, 100)]
    [Increment(5)]
    [DefaultValue(100)]
    public int SoundVolume;

    [Range(1, 15)]
    [Increment(1)]
    [DefaultValue(5)]
    public int DurationSeconds;

    [Cycle]
    [DefaultValue(AnnounceFrequency.FirstTimeOnly)]
    public AnnounceFrequency Frequency;

    // Only counts when Frequency is Set Amount.
    [Range(1, 100)]
    [Increment(1)]
    [DefaultValue(3)]
    public int MaxAnnouncementsPerNPC;
}
