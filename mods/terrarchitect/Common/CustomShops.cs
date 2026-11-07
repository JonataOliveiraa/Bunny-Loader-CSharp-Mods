using terrarchitect.Content.Items;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace terrarchitect.Common;

public class CustomShops : GlobalNPC
{
    private static Item Priced(int type, int price) => new Item(type) { shopCustomPrice = price };

    public override void ModifyShop(NPCShop shop)
    {
        switch (shop.NpcType)
        {
            case NPCID.Merchant:
                shop.Add(Priced(ModContent.ItemType<CashRegisterItem>(), Item.buyPrice(0, 1)));
                shop.Add(Priced(ModContent.ItemType<DogHouseItem>(), Item.buyPrice(0, 1))); // needs to be zoologist
                shop.Add(Priced(ModContent.ItemType<FoodBowlItem>(), Item.buyPrice(0, 0, 1))); // needs to be zoologist
                shop.Add(Priced(ModContent.ItemType<GoldenBellItem>(), Item.buyPrice(0, 0, 10)));
                shop.Add(Priced(ModContent.ItemType<ScalesItem>(), Item.buyPrice(0, 0, 1)));
                shop.Add(Priced(ModContent.ItemType<ScratchTreeItem>(), Item.buyPrice(0, 1))); // needs to be zoologist
                shop.Add(Priced(ModContent.ItemType<ServiceBellItem>(), Item.buyPrice(0, 0, 50)));
                shop.Add(Priced(ModContent.ItemType<StampItem>(), Item.buyPrice(0, 0, 0, 10)));
                shop.Add(Priced(ModContent.ItemType<WaterBowlItem>(), Item.buyPrice(0, 0, 1))); // needs to be zoologist
                break;
            case NPCID.Dryad:
                shop.Add(Priced(ModContent.ItemType<GardenHoseItem>(), Item.buyPrice(0, 0, 50)));
                shop.Add(Priced(ModContent.ItemType<PottedAloeVeraItem>(), Item.buyPrice(0, 0, 10)));
                shop.Add(Priced(ModContent.ItemType<PottedCactusItem>(), Item.buyPrice(0, 0, 10)));
                shop.Add(Priced(ModContent.ItemType<PottedSeedlingItem>(), Item.buyPrice(0, 0, 10)));
                shop.Add(Priced(ModContent.ItemType<WaterBowlItem>(), Item.buyPrice(0, 0, 1)));
                shop.Add(Priced(ModContent.ItemType<BirdNestItem>(), Item.buyPrice(0, 0, 1)));
                shop.Add(Priced(ModContent.ItemType<BeeNestItem>(), Item.buyPrice(0, 0, 1)));
                break;
            case NPCID.Pirate:
                shop.Add(Priced(ModContent.ItemType<ShipCannonItem>(), Item.buyPrice(0, 1)));
                shop.Add(Priced(ModContent.ItemType<AnchorWallMountItem>(), Item.buyPrice(0, 0, 50)));
                shop.Add(Priced(ModContent.ItemType<BigChestItem>(), Item.buyPrice(0, 1)));
                break;
            case NPCID.Wizard:
                shop.Add(Priced(ModContent.ItemType<SmallCrystalBallItem>(), Item.buyPrice(0, 0, 50)));
                break;
            case NPCID.Mechanic:
                shop.Add(Priced(ModContent.ItemType<ToolboxItem>(), Item.buyPrice(0, 0, 10)));
                break;
            case NPCID.DD2Bartender:
                shop.Add(Priced(ModContent.ItemType<BeerMugItem>(), Item.buyPrice(0, 0, 50)));
                break;
            case NPCID.Clothier:
                shop.Add(Priced(ModContent.ItemType<DressingScreenItem>(), Item.buyPrice(0, 0, 50)));
                shop.Add(Priced(ModContent.ItemType<SewingMachineItem>(), Item.buyPrice(0, 1)));
                break;
            case NPCID.Painter:
                shop.Add(Priced(ModContent.ItemType<EaselItem>(), Item.buyPrice(0, 0, 20)));
                shop.Add(Priced(ModContent.ItemType<SmallFramedPictureItem>(), Item.buyPrice(0, 0, 10)));
                // Wallpapers
                shop.Add(ModContent.ItemType<VerticalStripesWallpaperItem>());
                shop.Add(ModContent.ItemType<VintageFlowerWallpaperItem>());
                shop.Add(ModContent.ItemType<VintageVineWallpaperItem>());
                shop.Add(ModContent.ItemType<WavyLinesWallpaperItem>());
                shop.Add(ModContent.ItemType<RoyalWallpaperItem>());
                shop.Add(ModContent.ItemType<PolkaDotsWallpaperItem>());
                shop.Add(ModContent.ItemType<HorizontalStripesWallpaperItem>());
                break;
            case NPCID.WitchDoctor:
                shop.Add(Priced(ModContent.ItemType<LongTikiMaskItem>(), Item.buyPrice(0, 0, 10)));
                shop.Add(Priced(ModContent.ItemType<TikiMaskItem>(), Item.buyPrice(0, 0, 10)));
                break;
            case NPCID.Demolitionist:
                shop.Add(Priced(ModContent.ItemType<HazardWarningBlockItem>(), Item.buyPrice(0, 0, 1)));
                break;
        }
    }

    public override void SetupTravelShop(int[] shop, ref int nextSlot)
    {
        int r = Main.rand.Next(1, 26);
        switch (r)
        {
            case 1:
                shop[nextSlot] = ModContent.ItemType<DeerHuntingTrophyItem>();
                nextSlot++;
                break;
            case 2:
                shop[nextSlot] = ModContent.ItemType<GongItem>();
                nextSlot++;
                break;
            case 3:
                shop[nextSlot] = ModContent.ItemType<GlobeItem>();
                nextSlot++;
                break;
            case 4:
                shop[nextSlot] = ModContent.ItemType<TelescopeItem>();
                nextSlot++;
                break;
            case 5:
                shop[nextSlot] = ModContent.ItemType<AntelopeHuntingTrophyItem>();
                nextSlot++;
                break;
            case 6:
                shop[nextSlot] = ModContent.ItemType<DeerSkullTrophyItem>();
                nextSlot++;
                break;
            case 7:
                shop[nextSlot] = ModContent.ItemType<LargeGlobeItem>();
                nextSlot++;
                break;
            case 8:
                shop[nextSlot] = ModContent.ItemType<RhinoHuntingTrophyItem>();
                nextSlot++;
                break;
        }
    }
}
