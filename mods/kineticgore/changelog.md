## 2.0.0

Ported from JavaScript to C# on 2026-11-06, with the same physics, numbers and defaults. C# configs have no colored or aligned headers and no slider suffixes. The JavaScript wrapped most game calls in a catch that ignored failures, which the bridge needed: here only the texture reads keep it, so any other failure now goes to the log and the error panel instead of being silent. Corpses are kept in a Dictionary instead of an insertion-ordered Map, so when a ragdoll overlaps several others the one pushing it first may differ.

# What's new

## 2.8.2

- Translated into every Terraria language: German, French, Spanish, Italian,
  Russian, Polish and Chinese.
- The mod icon is now the original Workshop icon.

## 2.8.1

- Port of Physaria (Kinetic Gore) 2.8.1 to Bunny Loader.
- Ragdolls, dismemberment, sticky blood, gibs, head bob, slime jiggle, head
  banging and stuck arrows, with every option from the original.
- Bodies belong to each device: nothing goes over the network in multiplayer.
- New max corpses option, for mobile.
- Bodies face the way the enemy was facing, and use its size.
