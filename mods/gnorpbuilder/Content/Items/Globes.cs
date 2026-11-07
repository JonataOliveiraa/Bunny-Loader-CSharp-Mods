using GnorpBuilderMod.Common;
using Terraria.Audio;
using Terraria.ID;

namespace GnorpBuilderMod.Content.Items;

// One class per globe: saved items are keyed by the class name, so none of these may be renamed.

public class BasementArmoryGlobe : Globe
{
    public override int Value => 50000;
    public override string? Build => "BasementArmory";
}

public class BasementBarRoomGlobe : Globe
{
    public override int Value => 50000;
    public override string? Build => "BasementBarRoom";
}

public class BasementEnterBigDownFFFGlobe : Globe
{
    public override int Value => 10000;
    public override string? Build => "BasementEnterBigDownFFF";
}

public class BasementEnterDownFGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementEnterDownF";
}

public class BasementEnterGlobe : Globe
{
    public override int Value => 10000;
    public override string? Build => "BasementEnter";
}

public class BasementGardenGlobe : Globe
{
    public override int Value => 50000;
    public override string? Build => "BasementGarden";
}

public class BasementHallwayGlobe : Globe
{
    public override int Value => 10000;
    public override string? Build => "BasementHallway";
}

public class BasementHallwayNPCRoomGlobe : Globe
{
    public override int Value => 30000;
    public override string? Build => "BasementHallwayNPCRoom";
}

public class BasementLeftDownCornerGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementLeftDownCorner";
}

public class BasementLeftStaircaseGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementLeftStaircase";
}

public class BasementLeftUpCornerGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementLeftUpCorner";
}

public class BasementLibraryRoomGlobe : Globe
{
    public override int Value => 100000;
    public override string? Build => "BasementLibraryRoom";
}

public class BasementLongHallwayGlobe : Globe
{
    public override int Value => 15000;
    public override string? Build => "BasementHallwayLong";
}

public class BasementMusicRoomGlobe : Globe
{
    public override int Value => 500000;
    public override string? Build => "BasementMusicRoom";
}

public class BasementNPCPrisonGlobe : Globe
{
    public override int Value => 10000;
    public override string? Build => "BasementNPCPrison";
}

public class BasementNPCRoomGlobe : Globe
{
    public override int Value => 20000;
    public override string? Build => "BasementNPCRoom";
}

public class BasementPlusGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementPlus";
}

public class BasementPlusStaircaseGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementPlusStaircase";
}

public class BasementRightDownCornerGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementRightDownCorner";
}

public class BasementRightStaircaseGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementRightStaircase";
}

public class BasementRightUpCornerGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementRightUpCorner";
}

public class BasementShortDownHallwayGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementHallwayDownShort";
}

public class BasementShortHallwayGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementHallwayShort";
}

public class BasementTDownGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementTDown";
}

public class BasementTDownStaircaseGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementTDownStaircase";
}

public class BasementTLeftGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementTLeft";
}

public class BasementTRightGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementTRight";
}

public class BasementTUpGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementTUp";
}

public class BasementTUpStaircaseGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "BasementTUpStaircase";
}

public class BasementThroneRoomFFGlobe : Globe
{
    public override int Value => 2000000;
    public override string? Build => "BasementThroneRoomFF";
}

public class BasementTortureRoomGlobe : Globe
{
    public override int Value => 50000;
    public override string? Build => "BasementTortureRoom";
}

public class BiomeElevatorGlobe : Globe
{
    public override int Value => 750000;
    public override string? Elevator => "Biome";
}

public class BiomeLampPostGlobe : Globe
{
    public override int Value => 3000;
    public override string? Biome => "LampPost";
}

public class BiomePylonGlobe : Globe
{
    public override int Value => 200000;
    public override string? Biome => "Pylon";
}

public class BuilderWand : Globe
{
    public override int Value => 1;
    public override string? Build => "test";
}

public class CavernPylonGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "CavernPylon";
}

public class DesertElevatorGlobe : Globe
{
    public override int Value => 500000;
    public override string? Elevator => "Desert";
}

public class DesertPylonGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "DesertPylon";
}

public class EchoNPCHouseGlobe : Globe
{
    public override int Value => 149999;
    public override string? Build => "EchoNPCHouse";
}

public class ElevatorFullMinecartTrackGlobe : Globe
{
    public override int Value => 500000;
    public override bool Track => true;
}

public class ElevatorLeftRampMinecartTrackGlobe : Globe
{
    public override int Value => 1000;
    public override string? Build => "ElevatorLeftRampMinecartTrack";
}

public class ElevatorMinecartTrackGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "ElevatorMinecartTrack";
}

public class ElevatorMinecartTrackPlusGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "ElevatorMinecartTrackPlus";
}

public class ElevatorMinecartTrackStopGlobe : Globe
{
    public override int Value => 10000;
    public override string? Build => "ElevatorMinecartTrackStop";
}

public class ElevatorRightRampMinecartTrackGlobe : Globe
{
    public override int Value => 1000;
    public override string? Build => "ElevatorRightRampMinecartTrack";
}

public class ForestElevatorGlobe : Globe
{
    public override int Value => 500000;
    public override string? Elevator => "Forest";
}

public class ForestPylonGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "ForestPylon";
}

public class HallowElevatorGlobe : Globe
{
    public override int Value => 500000;
    public override string? Elevator => "Hallow";
}

public class HallowLampPostGlobe : Globe
{
    public override string? Build => "HallowLampPost";
}

public class HallowPylonGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "HallowPylon";
}

public class HeartHouseGlobe : Globe
{
    public override int Value => 250000;
    protected override LegacySoundStyle? PlaceSound => SoundID.NPCHit9;
    public override string? Build => "HeartHouse";
}

public class HugeCastleGlobe : Globe
{
    public override int Value => 200000;
    public override string? Build => "HugeCastle";
}

public class HugeTreeBaseGlobe : Globe
{
    public override int Value => 80000;
    public override string? Build => "HugeTreeBase";
}

public class JungleElevatorGlobe : Globe
{
    public override int Value => 500000;
    public override string? Elevator => "Jungle";
}

public class JungleLampPostGlobe : Globe
{
    public override string? Build => "JungleLampPost";
}

public class JunglePylonGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "JunglePylon";
}

public class LargeAdvancedArenaGlobe : Globe
{
    public override int Value => 450000;
    public override string? Arena => "Advanced";
}

public class LargeBiomeHouseGlobe : Globe
{
    public override int Value => 100000;
    public override string? Biome => "Large";
}

public class LargeCastleGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "LargeCastle";
}

public class LargeHallowHouseGlobe : Globe
{
    public override int Value => 90000;
    public override string? Build => "LargeHallowHouse";
}

public class LargeJungleHouseGlobe : Globe
{
    public override int Value => 90000;
    public override string? Build => "LargeJungleHouse";
}

public class LargeMushroomHouseGlobe : Globe
{
    public override int Value => 90000;
    public override string? Build => "LargeMushroomHouse";
}

public class LargeOceanHouseGlobe : Globe
{
    public override int Value => 90000;
    public override string? Build => "LargeOceanHouseF";
}

public class LargePooHouseGlobe : Globe
{
    public override int Value => 299999;
    protected override LegacySoundStyle? PlaceSound => SoundID.Item177;
    public override string? Build => "LargePooHouse";
}

public class LargeSandstoneHouseGlobe : Globe
{
    public override int Value => 90000;
    public override string? Build => "LargeSandstoneHouse";
}

public class LargeSimpleArenaGlobe : Globe
{
    public override int Value => 60000;
    public override string? Arena => "Simple";
}

public class LargeTreeBaseGlobe : Globe
{
    public override int Value => 60000;
    public override string? Build => "LargeTreeBase";
}

public class LargeTundraHouseGlobe : Globe
{
    public override int Value => 90000;
    public override string? Build => "LargeTundraHouse";
}

public class LargeValentineHouseGlobe : Globe
{
    public override int Value => 300000;
    public override string? Build => "LargeValentineHouse";
}

public class MediumAdvancedArenaGlobe : Globe
{
    public override int Value => 300000;
    public override string? Build => "MediumAdvancedArena";
}

public class MediumBiomeHouseGlobe : Globe
{
    public override int Value => 75000;
    public override string? Biome => "Medium";
}

public class MediumCastleGlobe : Globe
{
    public override int Value => 100000;
    public override string? Build => "MediumCastle";
}

public class MediumHallowHouseGlobe : Globe
{
    public override int Value => 60000;
    public override string? Build => "MediumHallowHouse";
}

public class MediumJungleHouseGlobe : Globe
{
    public override int Value => 60000;
    public override string? Build => "MediumJungleHouse";
}

public class MediumMushroomHouseGlobe : Globe
{
    public override int Value => 60000;
    public override string? Build => "MediumMushroomHouse";
}

public class MediumOceanHouseGlobe : Globe
{
    public override int Value => 60000;
    public override string? Build => "MediumOceanHouse";
}

public class MediumPooHouseGlobe : Globe
{
    public override int Value => 199999;
    protected override LegacySoundStyle? PlaceSound => SoundID.Item177;
    public override string? Build => "MediumPooHouse";
}

public class MediumSandstoneHouseGlobe : Globe
{
    public override int Value => 60000;
    public override string? Build => "MediumSandstoneHouse";
}

public class MediumSimpleArenaGlobe : Globe
{
    public override int Value => 40000;
    public override string? Build => "MediumSimpleArena";
}

public class MediumTreeBaseGlobe : Globe
{
    public override int Value => 40000;
    public override string? Build => "MediumTreeBase";
}

public class MediumTundraHouseGlobe : Globe
{
    public override int Value => 60000;
    public override string? Build => "MediumTundraHouse";
}

public class MediumValentineHouseGlobe : Globe
{
    public override int Value => 200000;
    public override string? Build => "MediumValentineHouse";
}

public class MushroomElevatorGlobe : Globe
{
    public override int Value => 500000;
    public override string? Elevator => "Mushroom";
}

public class MushroomLampPostGlobe : Globe
{
    public override string? Build => "MushroomLampPost";
}

public class MushroomPylonGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "MushroomPylon";
}

public class OceanElevatorGlobe : Globe
{
    public override int Value => 500000;
    public override string? Elevator => "Ocean";
}

public class OceanLampPostGlobe : Globe
{
    public override string? Build => "OceanLampPost";
}

public class OceanPylonGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "OceanPylon";
}

public class OldCabinGlobe : Globe
{
    public override int Value => 30000;
    public override string? Build => "OldCabin";
}

public class OldLargeCabinGlobe : Globe
{
    public override int Value => 90000;
    public override string? Build => "OldCabinBig";
}

public class OldMediumCabinGlobe : Globe
{
    public override int Value => 60000;
    public override string? Build => "OldCabinMedium";
}

public class PrisonCellGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "PrisonCell";
}

public class SandstoneLampPostGlobe : Globe
{
    public override string? Build => "SandstoneLampPost";
}

public class ShortElevatorMinecartTrackGlobe : Globe
{
    public override int Value => 1000;
    public override string? Build => "ShortElevatorMinecartTrack";
}

public class SimpleHoneyLakeGlobe : Globe
{
    public override int Value => 20000;
    public override string? Build => "SimpleHoneyLake";
}

public class SimpleLavaLakeGlobe : Globe
{
    public override int Value => 20000;
    public override string? Build => "SimpleLavaLake";
}

public class SimpleShimmerLakeGlobe : Globe
{
    public override int Value => 100000;
    public override string? Build => "SimpleShimmerLake";
}

public class SimpleWaterLakeGlobe : Globe
{
    public override int Value => 10000;
    public override string? Build => "SimpleWaterLake";
}

public class SmallAdvancedArenaGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "SmallAdvancedArena";
}

public class SmallBiomeHouseGlobe : Globe
{
    public override int Value => 50000;
    public override string? Biome => "Small";
}

public class SmallCastleGlobe : Globe
{
    public override int Value => 50000;
    public override string? Build => "SmallCastle";
}

public class SmallHallowHouseGlobe : Globe
{
    public override int Value => 30000;
    public override string? Build => "SmallHallowHouse";
}

public class SmallJungleHouseGlobe : Globe
{
    public override int Value => 30000;
    public override string? Build => "SmallJungleHouse";
}

public class SmallMushroomHouseGlobe : Globe
{
    public override int Value => 30000;
    public override string? Build => "SmallMushroomHouse";
}

public class SmallOceanHouseGlobe : Globe
{
    public override int Value => 30000;
    public override string? Build => "SmallOceanHouse";
}

public class SmallPooHouseGlobe : Globe
{
    public override int Value => 99999;
    protected override LegacySoundStyle? PlaceSound => SoundID.Item177;
    public override string? Build => "SmallPooHouse";
}

public class SmallSandstoneHouseGlobe : Globe
{
    public override int Value => 30000;
    public override string? Build => "SmallSandstoneHouse";
}

public class SmallSimpleArenaGlobe : Globe
{
    public override int Value => 20000;
    public override string? Build => "SmallSimpleArena";
}

public class SmallTreeBaseGlobe : Globe
{
    public override int Value => 20000;
    public override string? Build => "SmallTreeBase";
}

public class SmallTundraHouseGlobe : Globe
{
    public override int Value => 30000;
    public override string? Build => "SmallTundraHouse";
}

public class SmallValentineHouseGlobe : Globe
{
    public override int Value => 100000;
    public override string? Build => "SmallValentineHouse";
}

public class TundraElevatorGlobe : Globe
{
    public override int Value => 500000;
    public override string? Elevator => "Tundra";
}

public class TundraLampPostGlobe : Globe
{
    public override string? Build => "TundraLampPost";
}

public class TundraPylonGlobe : Globe
{
    public override int Value => 150000;
    public override string? Build => "TundraPylon";
}

public class ValentineLampPostGlobe : Globe
{
    public override int Value => 5000;
    public override string? Build => "ValentineLampPost";
}

public class WoodenLampPostGlobe : Globe
{
    public override string? Build => "WoodenLampPost";
}
