using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace BossChecklistMod.Common.UI;

public class UIMessageBox : UIPanel
{
    string text;
    float height;
    RichLayout? layout;
    float layoutWidth = -1;
    float view;
    float? dragFrom;
    float dragView;

    public UIMessageBox(string text)
    {
        this.text = text ?? "";
        OverflowHidden = true;
    }

    static float Scale => View.K;

    float ViewPosition
    {
        get => view;
        set
        {
            float max = Math.Max(0, height - GetInnerDimensions().Height);
            view = Math.Max(0, Math.Min(max, value));
        }
    }

    RichLayout? CurrentLayout()
    {
        CalculatedStyle space = GetInnerDimensions();
        if (space.Width <= 0)
            return null;
        if (layout == null || layoutWidth != space.Width)
        {
            layout = Rich.Layout(text, space.Width, Scale);
            layoutWidth = space.Width;
            height = layout.height;
        }
        return layout;
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (!Main.mouseLeft)
        {
            dragFrom = null;
            return;
        }
        if (dragFrom == null)
        {
            if (!Gesture.pressed || !ContainsPoint(new Vector2(Gesture.startX, Gesture.startY)))
                return;
            dragFrom = Main.mouseY;
            dragView = ViewPosition;
            return;
        }
        if (Gesture.moved)
            ViewPosition = dragView - (Main.mouseY - dragFrom.Value);
    }

    protected override void DrawChildren(SpriteBatch spriteBatch)
    {
        base.DrawChildren(spriteBatch);
        RichLayout? current = CurrentLayout();
        if (current == null)
            return;
        CalculatedStyle space = GetInnerDimensions();
        float top = space.Y - ViewPosition;
        int first = Math.Max(0, (int)Math.Floor((space.Y - top) / current.lineH) - 1);
        int count = (int)Math.Ceiling(space.Height / current.lineH) + 2;
        Rich.Draw(current, space.X, top + first * current.lineH, Scale, true, first, count, space.Y, space.Y + space.Height);
    }
}
