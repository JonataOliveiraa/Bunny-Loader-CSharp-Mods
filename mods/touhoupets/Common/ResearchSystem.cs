// One of each pet item unlocks it in Journey mode (SacrificeCountNeeded of
// the original). The game keeps no number for mod items. Set from the items'
// SetStaticDefaults, the number did not stay in the phone game's catalog: it
// goes in once everything is loaded, and again on each world.
using Terraria.ModLoader;
using TouhouPetsMod.Content.Items;

namespace TouhouPetsMod.Common;

public class ResearchSystem : ModSystem
{
    public override void PostSetupContent() => SetResearchCounts();

    public override void OnWorldLoad() => SetResearchCounts();

    void SetResearchCounts()
    {
        foreach (ModItem item in Mod.GetContent<ModItem>())
        {
            if (item is BasicPetItem && !item.Item.IsNull)
                item.Item.ResearchUnlockCount = 1;
        }
    }
}
