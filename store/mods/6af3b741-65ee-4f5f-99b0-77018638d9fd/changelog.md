## 2.0.0

Ported from JavaScript to C#. No behavior changes. The description and changelog are now in English; the Portuguese originals are kept as translations.

# What's new

## 1.0 - 2026-10-04

**Current stable release.**

- Spread block applied directly in the tile update paths used by Bunny Loader.
- Real spread test validated: with the protection active, no block was converted; with spreading allowed, it went back to happening normally.
- `/spread` kept for toggling the state per world.
- Per-world persistence and multiplayer sync kept.
- Warning when entering a world with spreading allowed kept.

## 0.1 - 2026-10-04

**First build of the Bunny Loader port.**

- Natural spread of Corruption, Crimson and Hallow disabled by default.
- `/spread` added to allow or block spreading.
- State saved separately per world.
- Multiplayer sync added.
- Original warning when entering a world with spreading enabled.
