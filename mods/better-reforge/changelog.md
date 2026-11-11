## 2.0.1

The panel's side is read from Reforge_Layout through the typed Bunny Loader API instead of a by-name binding.

## 2.0.0

Ported from JavaScript to C#. The C# API does not expose Reforge_Layout, so the panel's side is read from it by name at run time; if that read fails, the panel stays on the left page. The setup moved from Load to PostSetupContent, because the game has no items yet in a C# mod's Load. The preview image moved to Assets/preview.png, next to the textures. Helpers the JavaScript version never used (ModSlider, ModInterfaceLayer, ReforgeUI.ChangeTextures) were not ported.
