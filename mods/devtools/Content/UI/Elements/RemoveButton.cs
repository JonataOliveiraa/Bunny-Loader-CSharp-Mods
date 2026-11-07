using System;
using DevToolMod.TL;
using Microsoft.Xna.Framework.Graphics;

namespace DevToolMod.Content.UI.Elements;

public class RemoveButton : ModButton
{
    readonly Action callback;

    public RemoveButton(Action callback)
    {
        this.callback = callback;
    }

    public override Texture2D GetTexture() => Textures.Get("RemoveIcon");

    public override Color GetColor() => Pressed ? Color.DarkGray : Color.White;

    public override int? ClickSound() => 11;

    public override void OnClick() => callback();
}
