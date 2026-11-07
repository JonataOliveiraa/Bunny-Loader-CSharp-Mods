// Every pet item drops from a boss, in any mode, 1 in 4 (the chance of the
// Master Mode boss pets). Some bosses drop one of two or three.
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using TouhouPetsMod.Content.Items.PetItems;

namespace TouhouPetsMod.Common;

public class PetGlobalLoot : GlobalNPC
{
    static IItemDropRule Common<T>() where T : ModItem => ItemDropRule.Common(ModContent.ItemType<T>(), 4, 1, 1);

    static IItemDropRule OneOf(params int[] items) => ItemDropRule.OneFromOptions(4, items);

    // A rule that only tries `rule` when `condition` holds.
    static IItemDropRule When(IItemDropRuleCondition condition, IItemDropRule rule)
    {
        var lead = new LeadingConditionRule(condition);
        Chains.OnSuccess(lead, rule, false);
        return lead;
    }

    static IItemDropRule? RuleFor(int type)
    {
        switch (type)
        {
            case NPCID.KingSlime:
                return Common<DaiyouseiBomb>();
            case NPCID.EyeofCthulhu:
                return Common<KogasaUmbrella>();
            // Each segment of the Eater of Worlds can be the last one: the drop goes
            // to the one that is the boss when it dies.
            case NPCID.EaterofWorldsHead:
            case NPCID.EaterofWorldsBody:
            case NPCID.EaterofWorldsTail:
                return When(new Conditions.LegacyHack_IsABoss(), OneOf(ModContent.ItemType<RemiliaRedTea>(), ModContent.ItemType<FlandrePudding>()));
            case NPCID.BrainofCthulhu:
                return OneOf(ModContent.ItemType<RemiliaRedTea>(), ModContent.ItemType<FlandrePudding>());
            case NPCID.SkeletronHead:
                return Common<HinaDoll>();
            case NPCID.Deerclops:
                return Common<CirnoIceShard>();
            case NPCID.WallofFlesh:
                return Common<UtsuhoEye>();
            case NPCID.QueenSlimeBoss:
                return Common<PatchouliMoon>();
            // From the twin that dies last. The original chained the rule to the
            // condition but added the drop itself, so each twin rolled on its own.
            case NPCID.Retinazer:
            case NPCID.Spazmatism:
                return When(new Conditions.MissingTwin(), OneOf(ModContent.ItemType<SatoriSlippers>(), ModContent.ItemType<KoishiTelephone>()));
            case NPCID.Golem:
                return Common<SekibankiBow>();
            case NPCID.HallowBoss:
                return Common<TenshiKeyStone>();
            case NPCID.DukeFishron:
                return Common<IkuOarfish>();
            case NPCID.CultistBoss:
                return OneOf(ModContent.ItemType<ReimuYinyangOrb>(), ModContent.ItemType<MarisaHakkero>(), ModContent.ItemType<SanaeCoin>());
            case NPCID.MoonLordCore:
                return OneOf(ModContent.ItemType<HecatiaPlanet>(), ModContent.ItemType<JunkoMooncake>());
            default:
                return null;
        }
    }

    public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
    {
        if (RuleFor(npc.type) is IItemDropRule rule)
            npcLoot.Add(rule);
    }
}
