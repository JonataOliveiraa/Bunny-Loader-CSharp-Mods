using System;
using System.Collections.Generic;
using GnorpBuilderMod.Common;
using GnorpBuilderMod.Content.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GnorpBuilderMod.Content.NPCs;

[AutoloadHead]
public class Gnorp : ModNPC
{
    const int GnorpDust = 227;

    static readonly string[] PageShop = { "Main", "Module", "Utility", "Holiday", "Bases" };
    static readonly string[] PageChat = { "Gnorp!", "Prong!", "Vorp!", "Gorp!", "Virp!" };

    // The outfit is chosen once per game start, like the textures it decides.
    static readonly string TexturePath = Util.ModName + "/Content/NPCs/Gnorp" + OutfitSuffix();

    static Profiles.StackedNPCProfile profile = null!;

    int shopPage;

    public override string Texture => TexturePath;

    static bool IsEaster(DateTime now)
    {
        int year = now.Year;
        int a = year % 19, b = year / 100, c = year % 100, d = b / 4, e = b % 4;
        int f = (b + 8) / 25, g = (b - f + 1) / 3;
        int h = (19 * a + b - d - g + 15) % 30, i = c / 4, k = c % 4;
        int l = (32 + 2 * e + 2 * i - h - k) % 7, m = (a + 11 * h + 22 * l) / 451;
        int month = (h + l - 7 * m + 114) / 31, day = (h + l - 7 * m + 114) % 31 + 1;
        return now.Month == month && now.Day == day;
    }

    public static string OutfitSuffix()
    {
        DateTime now = DateTime.Now;
        if (now.Month == 4 && now.Day == 1) return "_Fools";
        if (now.Month == 2 && now.Day == 14) return "_Valentine";
        if (IsEaster(now)) return "_Easter";
        return "";
    }

    public override void Load()
    {
        Mod.AddNPCHeadTexture(Type, Texture + "_Shimmer_Head");
    }

    public override bool UsesPartyHat() => false;

    public override void SetStaticDefaults()
    {
        Main.npcFrameCount[Type] = 25;
        NPCID.Sets.ExtraFramesCount[Type] = 9;
        NPCID.Sets.AttackFrameCount[Type] = 4;
        NPCID.Sets.DangerDetectRange[Type] = 700;
        NPCID.Sets.AttackType[Type] = 0;
        NPCID.Sets.AttackTime[Type] = 90;
        NPCID.Sets.AttackAverageChance[Type] = 30;
        NPCID.Sets.HatOffsetY[Type] = -2;
        NPCID.Sets.ShimmerTownTransform[Type] = true;
        Main.npcCatchable[Type] = true;

        NPC.Happiness
            .SetBiomeAffection<DesertBiome>(AffectionLevel.Love)
            .SetBiomeAffection<ForestBiome>(AffectionLevel.Love)
            .SetBiomeAffection<HallowBiome>(AffectionLevel.Love)
            .SetBiomeAffection<JungleBiome>(AffectionLevel.Love)
            .SetBiomeAffection<MushroomBiome>(AffectionLevel.Love)
            .SetBiomeAffection<OceanBiome>(AffectionLevel.Love)
            .SetBiomeAffection<SnowBiome>(AffectionLevel.Love)
            .SetBiomeAffection<UndergroundBiome>(AffectionLevel.Love);

        // The JavaScript loader took these by file name. In C# they are declared.
        profile = new Profiles.StackedNPCProfile(
            new Profiles.DefaultNPCProfile(Texture, NPCHeadLoader.GetHeadSlot(HeadTexture), Texture + "_Party"),
            new Profiles.DefaultNPCProfile(Texture + "_Shimmer", NPCHeadLoader.GetHeadSlot(Texture + "_Shimmer_Head"), Texture + "_Shimmer_Party"));

        NPCID.Sets.NPCPortraits.Add(Type, NPCID.Sets.PrioritizedPortrait()
            .With(NPCID.Sets.ShimmeredPortraitCondition, NPCID.Sets.BasicPortrait(Texture + "_Shimmer_Portrait"))
            .Default(NPCID.Sets.BasicPortrait(Texture + "_Portrait")));
    }

    public override ITownNPCProfile TownNPCProfile() => profile;

    public override void SetDefaults()
    {
        NPC.townNPC = true;
        NPC.friendly = true;
        NPC.width = 18;
        NPC.height = 40;
        NPC.aiStyle = 7;
        NPC.damage = 0;
        NPC.defense = 15;
        NPC.lifeMax = 500;
        NPC.HitSound = SoundID.NPCHit39;
        NPC.DeathSound = SoundID.NPCDeath57;
        NPC.knockBackResist = 0.5f;
        NPC.catchItem = (short)ModContent.ItemType<GnorpsPilot>();
        AnimationType = NPCID.Merchant;
    }

    public override List<string> SetNPCNameList() => new() { "Gnorp" };

    public override bool CanTownNPCSpawn(int numTownNPCs) => true;

    public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
    {
        bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement(Util.KeyPrefix + "Bestiary.Gnorp"));
    }

    static string GorePrefix()
    {
        switch (OutfitSuffix())
        {
            case "_Fools": return "Gore_Fools_Gnorp";
            case "_Valentine": return "Gore_Valentine_Gnorp";
            case "_Easter": return "Gore_Easter_Gnorp";
            default: return "Gore_Gnorp";
        }
    }

    public override void HitEffect(NPC.HitInfo hit)
    {
        for (int i = 0; i < 2; i++)
            Dust.NewDust(NPC.position, NPC.width, NPC.height, GnorpDust, hit.HitDirection, -1f, 0, Color.White, 1f);
        if (NPC.life > 0) return;

        if (Main.netMode != NetmodeID.Server)
        {
            string prefix = GorePrefix();
            for (int j = 1; j <= 3; j++)
            {
                var velocity = new Vector2(NPC.velocity.X + (Main.rand.NextFloat() * 4f - 2f), NPC.velocity.Y + (Main.rand.NextFloat() * 4f - 2f));
                Gore.NewGore(NPC.position, velocity, Mod.Find<ModGore>(prefix + j).Type, NPC.scale);
            }
        }
        for (int k = 0; k < 20; k++)
            Dust.NewDust(NPC.position, NPC.width, NPC.height, GnorpDust, Main.rand.NextFloat() * 4f - 2f, Main.rand.NextFloat() * 4f - 2f, 100, Color.White, 1.5f);
    }

    public override void ModifyNPCLoot(NPCLoot npcLoot)
    {
        npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<GnorpsPilot>(), 1, 1, 1));
    }

    public override string GetChat()
    {
        switch (Main.rand.Next(3))
        {
            case 0: return "Norp!";
            case 1: return "Gnorp.";
            default: return "Fnorp?";
        }
    }

    public override void SetChatButtons(ref string button, ref string button2)
    {
        button = Language.GetTextValue("LegacyInterface.28") ?? "";
        button2 = Util.T("ShopUI.ChangeFormat", Util.T("NPCs.Gnorp.Shops." + ShopSelectState.Pages[shopPage]));
    }

    public override void OnChatButtonClicked(bool firstButton, ref string shopName)
    {
        if (firstButton)
        {
            shopName = PageShop[shopPage];
            return;
        }
        ShopSelectSystem.Instance.Show(shopPage, page =>
        {
            shopPage = page;
            Main.npcChatText = PageChat[page];
        });
    }

    public override void AddShops()
    {
        Condition Hardmode = Condition.Hardmode;
        Condition DownedMoonLord = Condition.DownedMoonLord;

        new NPCShop(Type, "Main")
            .Add<WoodenLampPostGlobe>()
            .Add<OldCabinGlobe>()
            .Add<OldMediumCabinGlobe>()
            .Add<OldLargeCabinGlobe>()
            .Add<TundraLampPostGlobe>()
            .Add<SmallTundraHouseGlobe>()
            .Add<MediumTundraHouseGlobe>()
            .Add<LargeTundraHouseGlobe>()
            .Add<SandstoneLampPostGlobe>()
            .Add<SmallSandstoneHouseGlobe>()
            .Add<MediumSandstoneHouseGlobe>()
            .Add<LargeSandstoneHouseGlobe>()
            .Add<JungleLampPostGlobe>()
            .Add<SmallJungleHouseGlobe>()
            .Add<MediumJungleHouseGlobe>()
            .Add<LargeJungleHouseGlobe>()
            .Add<OceanLampPostGlobe>()
            .Add<SmallOceanHouseGlobe>()
            .Add<MediumOceanHouseGlobe>()
            .Add<LargeOceanHouseGlobe>()
            .Add<MushroomLampPostGlobe>()
            .Add<SmallMushroomHouseGlobe>()
            .Add<MediumMushroomHouseGlobe>()
            .Add<LargeMushroomHouseGlobe>()
            .Add<HallowLampPostGlobe>(Hardmode)
            .Add<SmallHallowHouseGlobe>(Hardmode)
            .Add<MediumHallowHouseGlobe>(Hardmode)
            .Add<LargeHallowHouseGlobe>(Hardmode)
            .Add<BiomeLampPostGlobe>()
            .Add<SmallBiomeHouseGlobe>()
            .Add<MediumBiomeHouseGlobe>()
            .Add<LargeBiomeHouseGlobe>()
            .Register();

        new NPCShop(Type, "Utility")
            .Add<PrisonCellGlobe>()
            .Add<BasementNPCPrisonGlobe>()
            .Add<BasementNPCRoomGlobe>()
            .Add<BasementHallwayNPCRoomGlobe>()
            .Add<BasementTortureRoomGlobe>()
            .Add<BasementGardenGlobe>()
            .Add<BasementArmoryGlobe>()
            .Add<BasementBarRoomGlobe>()
            .Add<BasementLibraryRoomGlobe>()
            .Add<BasementMusicRoomGlobe>()
            .Add<BasementThroneRoomFFGlobe>()
            .Add<BasementEnterGlobe>()
            .Add<BasementEnterBigDownFFFGlobe>()
            .Add<BasementEnterDownFGlobe>()
            .Add<BasementShortHallwayGlobe>()
            .Add<BasementHallwayGlobe>()
            .Add<BasementLongHallwayGlobe>()
            .Add<BasementShortDownHallwayGlobe>()
            .Add<BasementLeftStaircaseGlobe>()
            .Add<BasementRightStaircaseGlobe>()
            .Add<BasementPlusGlobe>()
            .Add<BasementTUpGlobe>()
            .Add<BasementTDownGlobe>()
            .Add<BasementTLeftGlobe>()
            .Add<BasementTRightGlobe>()
            .Add<BasementPlusStaircaseGlobe>()
            .Add<BasementTUpStaircaseGlobe>()
            .Add<BasementTDownStaircaseGlobe>()
            .Add<BasementLeftUpCornerGlobe>()
            .Add<BasementLeftDownCornerGlobe>()
            .Add<BasementRightUpCornerGlobe>()
            .Add<BasementRightDownCornerGlobe>()
            .Register();

        new NPCShop(Type, "Holiday")
            .Add<SmallSimpleArenaGlobe>()
            .Add<MediumSimpleArenaGlobe>()
            .Add<LargeSimpleArenaGlobe>()
            .Add<SmallAdvancedArenaGlobe>()
            .Add<MediumAdvancedArenaGlobe>()
            .Add<LargeAdvancedArenaGlobe>()
            .Add<SimpleWaterLakeGlobe>()
            .Add<SimpleLavaLakeGlobe>()
            .Add<SimpleHoneyLakeGlobe>()
            .Add<SimpleShimmerLakeGlobe>(DownedMoonLord)
            .Add<ForestElevatorGlobe>()
            .Add<TundraElevatorGlobe>()
            .Add<DesertElevatorGlobe>()
            .Add<JungleElevatorGlobe>()
            .Add<OceanElevatorGlobe>()
            .Add<MushroomElevatorGlobe>()
            .Add<HallowElevatorGlobe>(Hardmode)
            .Add<BiomeElevatorGlobe>()
            .Add<ElevatorFullMinecartTrackGlobe>()
            .Add<ElevatorMinecartTrackPlusGlobe>()
            .Add<ShortElevatorMinecartTrackGlobe>()
            .Add<ElevatorMinecartTrackGlobe>()
            .Add<ElevatorLeftRampMinecartTrackGlobe>()
            .Add<ElevatorRightRampMinecartTrackGlobe>()
            .Add<ElevatorMinecartTrackStopGlobe>()
            .Add<ForestPylonGlobe>()
            .Add<CavernPylonGlobe>()
            .Add<TundraPylonGlobe>()
            .Add<DesertPylonGlobe>()
            .Add<JunglePylonGlobe>()
            .Add<OceanPylonGlobe>()
            .Add<MushroomPylonGlobe>()
            .Add<HallowPylonGlobe>(Hardmode)
            .Add<BiomePylonGlobe>()
            .Register();

        new NPCShop(Type, "Bases")
            .Add<SmallValentineHouseGlobe>()
            .Add<MediumValentineHouseGlobe>()
            .Add<LargeValentineHouseGlobe>()
            .Add<ValentineLampPostGlobe>()
            .Add<HeartHouseGlobe>()
            .Add<SmallPooHouseGlobe>()
            .Add<MediumPooHouseGlobe>()
            .Add<LargePooHouseGlobe>()
            .Add<EchoNPCHouseGlobe>()
            .Register();

        new NPCShop(Type, "Module")
            .Add<SmallCastleGlobe>()
            .Add<MediumCastleGlobe>()
            .Add<LargeCastleGlobe>()
            .Add<HugeCastleGlobe>()
            .Add<SmallTreeBaseGlobe>()
            .Add<MediumTreeBaseGlobe>()
            .Add<LargeTreeBaseGlobe>()
            .Add<HugeTreeBaseGlobe>()
            .Register();
    }
}
