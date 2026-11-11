## 2.0.1

The zoom hooks and Journey's time freeze and time speed use the typed XNAWorldCameraSettings, CreativePowerManager and CreativePowers API instead of the loader's low-level interop.

## 2.0.0

Ported from JavaScript to C#, on 2026-11-06, with the same tools, numbers and defaults. The interface now draws through ModSystem.ModifyInterfaceLayers, and photo mode switches the other layers off instead of blocking their draw. The zoom hooks and Journey's time freeze and time speed use the loader's low-level interop, because the C# API has no members for XNAWorldCameraSettings, CreativePowerManager or CreativePowers. The UI Scale slider no longer shows its x suffix, which the C# config has no attribute for. The never-registered WorldSettingsUI (spawn rate and difficulty sliders) and other unreachable code were not ported.
