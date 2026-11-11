## 2.0.1

XNA's frame timing fields are read and written through the typed Bunny Loader API instead of by name. The Minimap and Liquid throttle options no longer try to hook methods the game does not have; they still have no effect.

## 2.0.0

Ported from JavaScript to C#. Unity's Application.targetFrameRate and Input, XNA's Game.FrameTickTime, FrameClampTickTime and FrameIgnoreTime, Liquid.Update and WorldMap.Update have no C# API, so they are reached by name through BunnyLoader.Interop; a member that is missing is logged and that part does nothing. Far Entities, Critters and Wiring throttling never worked in the JavaScript version, because the methods it hooked (Main.UpdateNPC, Wiring.Update) do not exist in the game, and they still have no effect. If the frame timing fields cannot be written, the update gate stays off instead of slowing the world down. Hooks are installed in OnModLoad instead of PostSetupContent.
