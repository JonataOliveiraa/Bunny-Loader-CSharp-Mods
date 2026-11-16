## 2.0.1

Which creatures are unlocked is read through the typed Bunny Loader API instead of a by-name binding per provider class.

## 2.0.0

Ported from JavaScript to C#. Settings travel as binary instead of JSON text. Holograms are found through a list the entity class keeps, because the loader's table of entities is not public. The creature list reads the bestiary's table by id instead of each entry's info elements. The list's glide after a drag follows real time instead of counting frames. The loader keeps C# tile entities apart from JavaScript ones, so a projector placed with the JavaScript version loses its settings and does not open its menu: break it and place it again.

# Changelog

## 1.1.0

- New projector menu built with the game's own mobile interface: real buttons
  that respond to a direct tap, a live preview of the creature, ON/OFF toggles
  and - / + controls.
- The creature list now finds every creature unlocked in the Bestiary (it was
  empty before).
- Scroll the creature list by dragging it; tap a creature to show it.
- Rotation wraps around instead of stopping at 0 and 360.

## 1.0.0

- Port of Bestiary Displays to Bunny Loader: the three Hologram Projectors,
  the creature picker with search, animation, hologram effects, scale, offset,
  rotation and invisible block settings, saved with the world and synced in
  multiplayer.
- The menu works with touch: tap the projector to open it, tap a value to type
  it.
- Names and menu in 12 languages.
