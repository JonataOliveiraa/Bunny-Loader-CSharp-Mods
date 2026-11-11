## 2.0.1

TorchColor uses the On_TorchID hook instead of a raw hook by name.

## 2.0.0

Ported from JavaScript to C#. TorchID has no C# API, so TorchColor uses a raw hook by name. The flicker picks its value with .NET's thread-safe random instead of the game's, with the same three values and odds, because TorchColor may run away from the main thread.
