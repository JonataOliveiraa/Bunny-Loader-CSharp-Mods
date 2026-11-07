using Consolaria.Common.ModSystems;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria.Common {
    class VanillaNPCShop : GlobalNPC {
        public static Condition thanksgivingCondition = new Condition("Mods.consolaria.Conditions.SellingDuringThanksgiving", () => (SeasonalEvents.configEnabled && SeasonalEvents.IsThanksgiving()) || !SeasonalEvents.configEnabled);
        public static Condition oktoberfestCondition = new Condition("Mods.consolaria.Conditions.SellingDuringOktoberfest", () => (SeasonalEvents.configEnabled && SeasonalEvents.IsOktoberfest()) || (!SeasonalEvents.configEnabled && Main.LocalPlayer.HeldItem.type == ItemID.Ale));
        public static Condition valentineDayCondition = new Condition("Mods.consolaria.Conditions.SellingDuringValentineDay", () => (SeasonalEvents.configEnabled && SeasonalEvents.IsValentineDay()) || (!SeasonalEvents.configEnabled && Main.LocalPlayer.HasBuff(BuffID.Lovestruck)));
        public static Condition isMaleCondition = new Condition("Mods.consolaria.Conditions.SellingIfMale", () => Main.LocalPlayer.Male & Main.bloodMoon);
        public static Condition womanMoment = new Condition("Mods.consolaria.Conditions.SellingWhenWoman", () => !Main.LocalPlayer.Male & Main.bloodMoon);
        public static Condition wishboneCooldown = new Condition("Mods.consolaria.Conditions.SellingDuringWishboneCooldown", () => !WishbonePlayer.purchasedWishbone);
        public static Condition ocramDownedCondition = new Condition("Mods.consolaria.Conditions.OcramDowned", () => DownedBossSystem.downedOcram);
        public static Condition turkorDownedCondition = new Condition("Mods.consolaria.Conditions.TurkorDowned", () => DownedBossSystem.downedTurkor);

        public override void ModifyShop(NPCShop shop) {
            if (shop.NpcType == NPCID.Merchant) {
                shop.Add(ModContent.ItemType<Content.Items.Pets.TurkeyFeather>(), thanksgivingCondition);
                shop.Add(ModContent.ItemType<Content.Items.Consumables.Wiesnbrau>(), oktoberfestCondition);
                shop.Add(ModContent.ItemType<Content.Items.Accessories.ValentineRing>(), valentineDayCondition);
                shop.Add(ModContent.ItemType<Content.Items.Weapons.Ammo.HeartArrow>(), valentineDayCondition);
                shop.Add(ModContent.ItemType<Content.Items.Weapons.Ranged.SpicySauce>(), turkorDownedCondition);
            }
            if (shop.NpcType == NPCID.TravellingMerchant) {
                shop.Add(ModContent.ItemType<Content.Items.Vanity.HornedGodMask>(), ConsolariaConditions.DownedMechBossAny, ConsolariaConditions.MoonPhaseFull);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.HornedGodRobe>(), ConsolariaConditions.DownedMechBossAny, ConsolariaConditions.MoonPhaseFull);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.HornedGodBoots>(), ConsolariaConditions.DownedMechBossAny, ConsolariaConditions.MoonPhaseFull);
            }
            if (shop.NpcType == NPCID.Clothier) {
                shop.Add(ModContent.ItemType<Content.Items.Pets.OldWalkingStick>());
                shop.Add(ModContent.ItemType<Content.Items.Vanity.FestiveTopHat>(), ConsolariaConditions.Christmas);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.ShirenShirt>(), ConsolariaConditions.MoonPhaseNew);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.ShirenPants>(), ConsolariaConditions.MoonPhaseNew);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.TorosHead>(), ConsolariaConditions.BloodMoon);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.TorosBody>(), ConsolariaConditions.BloodMoon);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.TorosLegs>(), ConsolariaConditions.BloodMoon);

                if (ModContent.GetInstance<ConsolariaConfig>().genderRestrictShopEnabled) {
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.GeorgesHat>(), isMaleCondition);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.GeorgesTuxedoShirt>(), isMaleCondition);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.GeorgesTuxedoPants>(), isMaleCondition);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.FabulousRibbon>(), womanMoment);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.FabulousDress>(), womanMoment);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.FabulousSlippers>(), womanMoment);
                }
                else {
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.GeorgesHat>(), ConsolariaConditions.BloodMoon);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.GeorgesTuxedoShirt>(), ConsolariaConditions.BloodMoon);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.GeorgesTuxedoPants>(), ConsolariaConditions.BloodMoon);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.FabulousRibbon>(), ConsolariaConditions.BloodMoon);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.FabulousDress>(), ConsolariaConditions.BloodMoon);
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.FabulousSlippers>(), ConsolariaConditions.BloodMoon);
                }

                shop.Add(ModContent.ItemType<Content.Items.Vanity.AlpineHat>(), oktoberfestCondition);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.Lederweste>(), oktoberfestCondition);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.Lederhosen>(), oktoberfestCondition);

                if (ModContent.GetInstance<ConsolariaConfig>().oktoberLocksEnabled) {
                    shop.Add(ModContent.ItemType<Content.Items.Vanity.OktoberLocks>(), oktoberfestCondition);
                }

                shop.Add(ModContent.ItemType<Content.Items.Vanity.DirndlBlouse>(), oktoberfestCondition);
                shop.Add(ModContent.ItemType<Content.Items.Vanity.DirndlSkirt>(), oktoberfestCondition);
            }
            if (shop.NpcType == NPCID.SantaClaus)
                shop.Add(ModContent.ItemType<Content.Items.Placeable.StarTopper4>());
            if (shop.NpcType == NPCID.Demolitionist)
                shop.Add(ModContent.ItemType<Content.Items.Weapons.Magic.RomanCandle>(), ConsolariaConditions.Christmas);
            if (shop.NpcType == NPCID.Cyborg)
                shop.Add(ModContent.ItemType<Content.Items.Pets.ShinyBlackSlab>());
            if (shop.NpcType == NPCID.Mechanic)
                shop.Add(ModContent.ItemType<Content.Items.Pets.MysteriousPackage>(), ConsolariaConditions.DownedMechBossAny);
            if (shop.NpcType == NPCID.SkeletonMerchant) {
                int wishBone = ModContent.ItemType<Content.Items.Consumables.Wishbone>();
                shop.Add(wishBone, wishboneCooldown);
            }
            if (shop.NpcType == NPCID.ArmsDealer) {
                shop.Add(ModContent.ItemType<Content.Items.Weapons.Ammo.SpectralArrow>(), ocramDownedCondition);
            }
        }
    }
}