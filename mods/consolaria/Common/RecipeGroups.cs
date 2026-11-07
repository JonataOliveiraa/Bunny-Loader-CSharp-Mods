using Consolaria.Common;
using Consolaria.Content.Items.Weapons.Melee;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Consolaria {
    public class RecipeGroups : ModSystem {
        public static RecipeGroup Titanium;

        public override void Unload()
            => Titanium = default;

        public override void AddRecipeGroups() {
            Titanium = RecipeGroup.Register("Consolaria:TitaniumRecipeGroup", () => "Adamantite or Titanium Bar", new int[] { ItemID.AdamantiteBar, ItemID.TitaniumBar });
        }

        public override void PostAddRecipes() {
            if (ModContent.GetInstance<ConsolariaConfig>().tizonaZenithIntegrationEnabled) {
                for (int i = 0; i < Recipe.numRecipes; i++) {
                    Recipe recipe = Main.recipe[i];

                    if (recipe.HasResult(ItemID.Zenith)) {
                        // On the phone, the empty slots of the game's own recipes are one shared object, so
                        // Recipe.AddIngredient would turn every empty slot of every recipe into this item.
                        // The ingredient gets an object of its own instead.
                        var slots = recipe.requiredItem;
                        int slot = 0;
                        while (slot < slots.Length && slots[slot].type > 0) slot++;
                        if (slot < slots.Length) slots[slot] = Item.New(ModContent.ItemType<Tizona>(), default);
                    }
                }
            }
        }
    }
}