## 2.0.0

Ported from JavaScript to C#. The GlobalItem class is now CanPickupItem, because a C# method cannot share its class's name; nothing is saved under that name. As in the JavaScript version, Nebula Life and Nebula Mana boosters are still picked up: the range test in the original leaves them out, which contradicts the description.
