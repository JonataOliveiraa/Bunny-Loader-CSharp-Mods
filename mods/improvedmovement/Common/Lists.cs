using System.Collections.Generic;
using Terraria.ID;

namespace ImprovedMovementMod.Common;

public static class Lists
{
    public static readonly HashSet<int> MountBlacklist = new()
    {
        MountID.UFO, MountID.Drill, MountID.WitchBroom, MountID.PogoStick, MountID.LavaShark,
    };

    public static readonly HashSet<int> SlimesWhitelist = new()
    {
        NPCID.BlueSlime, NPCID.MotherSlime, NPCID.LavaSlime, NPCID.DungeonSlime, NPCID.CorruptSlime,
        NPCID.IlluminantSlime, NPCID.Gastropod, NPCID.ToxicSludge, NPCID.IceSlime, NPCID.Crimslime,
        NPCID.SpikedIceSlime, NPCID.SpikedJungleSlime, NPCID.UmbrellaSlime, NPCID.RainbowSlime,
        NPCID.SlimeMasked, NPCID.HoppinJack, NPCID.SlimeRibbonWhite, NPCID.SlimeRibbonGreen,
        NPCID.SlimeRibbonYellow, NPCID.SlimeRibbonRed, NPCID.SlimeSpiked, NPCID.SandSlime,
    };
}
