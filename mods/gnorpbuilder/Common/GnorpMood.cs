using GnorpBuilderMod.Content.NPCs;
using Terraria;
using Terraria.GameContent;
using Terraria.Localization;
using Terraria.ModLoader;

namespace GnorpBuilderMod.Common;

// The game looks a mood line up under its own key, and the C# loader does not point that key at a
// mod's texts as the JavaScript loader did. The substitutes keep working, as {BiomeName} and {NPCName}.
public static class GnorpMood
{
    const string Category = Util.KeyPrefix + "NPCs.Gnorp.TownNPCMood.";

    public static void Install()
    {
        On_ShopHelper.AddHappinessReportText += (orig, self, textKeyInCategory, substitutes) =>
        {
            NPC npc = self._currentNPCBeingTalkedTo;
            if (npc.IsNull || npc.type != ModContent.NPCType<Gnorp>())
            {
                orig(self, textKeyInCategory, substitutes);
                return;
            }

            // A line the mod has no text for comes back as its key, and goes to the game as before.
            string key = Category + textKeyInCategory;
            string? text = Language.GetTextValueWith(key, substitutes);
            if (text == null || text == key) orig(self, textKeyInCategory, substitutes);
            else self._currentHappiness = self._currentHappiness + text + " ";
        };
    }
}
