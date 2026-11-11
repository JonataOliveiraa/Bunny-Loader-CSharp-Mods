## 2.0.1

The connection state is read through the typed Bunny Loader API instead of a by-name binding.

## 2.0.0

Ported from JavaScript to C#. Refresh intervals and the health bar measure's expiry use real time instead of frame counts. The tracker learns which bar it is measuring from PreDrawResourceDisplay, because the C# draw context does not say. Other mods' screens are noticed by watching the interface drawn just before this one, because UserInterface.SetState cannot be hooked in C#, so only the last interface drawn before this mod's layer is noticed. The Ping refresh option has no seconds suffix, because C# configs have none.

# What's new

## 1.0.5

- The ping indicator sits next to the health bar, with the player board button
  beside it. Turn off **Next to the health bar** to drag it anywhere.
- The board and the settings never overlap, and they close when the inventory,
  the pause menu, another screen or another mod's interface opens.
- The ping interface draws above the rest of the interface, with full player
  names and readable text.
- Translated into every Terraria language.

## 1.0.1

- Port of Player Ping UI to Bunny Loader: drag the indicator and tap it for the
  settings, which are also in Mod Configuration.
