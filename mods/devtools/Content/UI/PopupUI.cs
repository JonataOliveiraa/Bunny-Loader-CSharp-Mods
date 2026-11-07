using System;
using DevToolMod.Content.UI.Elements;
using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static DevToolMod.TL.Utilities.UIScaleHelper;

namespace DevToolMod.Content.UI;

public abstract class PopupUI : ModInterface
{
    protected bool Active;
    protected RemoveButton RemoveButton = null!;
    protected bool Dragging;
    protected int DragOffsetX;
    protected int DragOffsetY;
    protected Rectangle Panel = new(0, 0, 1, 1);
    protected float? PanelX;
    protected float? PanelY;
    protected float PanelWidth;
    protected float PanelHeight;
    protected Color PanelColor;
    protected Panel_Layout Layout;
    protected bool IsHoveredAnyElement;

    protected abstract void CheckActive();

    protected abstract void DrawSelf();

    public override void Clear()
    {
        Dragging = false;
        DragOffsetX = 0;
        DragOffsetY = 0;
        PanelX = null;
        PanelY = null;
        IsHoveredAnyElement = false;
    }

    protected virtual void ResizePanel() { }

    void GetPanelRect(ref Rectangle rect)
    {
        float width = PanelWidth;
        float height = PanelHeight;
        float x = PanelX ?? (Main.screenWidth * 0.5f - width * 0.5f);
        float y = PanelY ?? (Main.screenHeight * 0.5f - height * 0.5f);
        x = Math.Max(0, Math.Min(x, Main.screenWidth - width));
        y = Math.Max(0, Math.Min(y, Main.screenHeight - height));
        rect.X = (int)x;
        rect.Y = (int)y;
        rect.Width = (int)width;
        rect.Height = (int)height;
    }

    public override void Draw()
    {
        IsHoveredAnyElement = false;
        CheckActive();
        if (Active)
        {
            ResizePanel();
            GetPanelRect(ref Panel);
            Recalculate(Layout, Panel);
            DrawPanelBG();
            DrawRemoveButton();
            DrawSelf();
            UpdateDrag();
        }
    }

    // The border scales too. The game's DrawInvBG hard-codes a nine-slice border of 10 pixels,
    // so on a smaller panel only the middle would stretch; here the 10 pixels follow the scale.
    void DrawPanelBG() => DrawHelper.InventoryPanel(Panel, PanelColor, 10 * MenuScale());

    void DrawRemoveButton()
    {
        RemoveButton button = RemoveButton;
        ref Rectangle rect = ref button.Area;
        rect.X = (int)(Panel.X + Panel.Width - rect.Width - S(8));
        rect.Y = (int)(Panel.Y + S(8));
        rect.Width = (int)S(16);
        rect.Height = (int)S(16);
        ModButton.UpdateButton(button, rect);
        if (button.Hovered) IsHoveredAnyElement = true;
        DrawHelper.Rectangle(button.GetTexture(), rect, button.GetColor());
    }

    void UpdateDrag()
    {
        if (!IsHoveredAnyElement && Main.mouseLeft && Main.mouseLeftRelease && !Dragging && Panel.Contains(Main.mouseX, Main.mouseY))
        {
            Dragging = true;
            DragOffsetX = Main.mouseX - Panel.X;
            DragOffsetY = Main.mouseY - Panel.Y;
        }
        if (Dragging)
        {
            PanelX = Main.mouseX - DragOffsetX;
            PanelY = Main.mouseY - DragOffsetY;
            if (!Main.mouseLeft) Dragging = false;
        }
    }
}
