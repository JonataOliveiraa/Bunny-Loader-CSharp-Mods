## 2.0.0

Ported from JavaScript to C#. Detects a break through Player.PickTile, because the C# API does not expose HitTile.AddDamage. The player's list moved to a text file in the save folder, because the phone has no System.Text.Json, so lists from the JavaScript version are not carried over. The config has no clear button, because C# configs have no buttons: use /vein clear.
