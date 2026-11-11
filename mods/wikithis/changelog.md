## 2.0.1

The split stack draw hook and the button layout use the typed Bunny Loader API instead of by-name bindings.

## 2.0.0

Ported from JavaScript to C#. The C# API does not expose GUIInventorySplitStack.Draw, InventorySplitStack_Layout or UnityEngine.Application.OpenURL, so they are bound by name at run time with BunnyLoader.Hook and GameInterop; if the hook or the layout read fails, the slot does not appear and the error is logged once. The URL table is built in ModSystem.SetStaticDefaults instead of a hook on Main.Initialize_AlmostEverything, so it is ready before any mod's PostSetupContent. The slot swaps the TextureAssets entries for one draw instead of writing Asset.Value, which has no setter in C#. Call takes the same arguments, now from C# mods, and throws ArgumentException where JavaScript threw Error; the description's examples are now in C#. Helpers the JavaScript version never used (ModButton, ModSlider, ModInterfaceLayer, Utils/Microsoft.js) were not ported.
