using Terraria.ModLoader;
using WikithisMod.Common.UI;

namespace WikithisMod.Common.Systems;

public class WikiSystem : ModSystem
{
    // Runs before any mod's PostSetupContent, where other mods add their wikis with Call,
    // so building the table here does not erase their entries.
    public override void SetStaticDefaults() => WikiUI.InitializeItemUrl();
}
