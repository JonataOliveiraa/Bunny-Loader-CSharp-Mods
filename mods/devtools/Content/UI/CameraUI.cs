using System;
using System.Globalization;
using DevToolMod.Content.UI.Elements;
using DevToolMod.Core.Systems;
using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using static DevToolMod.TL.Utilities.UIScaleHelper;

namespace DevToolMod.Content.UI;

public class CameraUI : PopupUI
{
    sealed class Button : ModButton
    {
        readonly CameraUI Parent;
        readonly int Index;

        public Button(int idx, CameraUI parent)
        {
            Parent = parent;
            Index = idx;
        }

        public override Color GetColor() => Parent.ButtonColor;

        public override Texture2D GetIconTexture() => Textures.Get("CS_Icon_" + Index);

        public override Texture2D GetTexture()
        {
            string name = "TB_Button";
            if (Clicked) name += "_Pressed";
            else if (Hovered) name += "_Hovered";
            return Textures.Get(name);
        }

        public override void OnClick()
        {
            if (Index == 3)
                CameraSystem.PhotoMode = true;
            else if ((int)CameraSystem.CameraMode != Index)
                CameraSystem.SetCamera((CameraMode)Index);
        }
    }

    sealed class ZoomSlider : ModSlider
    {
        public ZoomSlider(double min, double max, double defaultValue) : base(min, max, defaultValue) { }

        public override Texture2D GetBackTexture() => Textures.Get("CS_Slider_Back");

        public override Texture2D GetButtonTexture() => Textures.Get("CS_Slider_Button");

        public override double GetButtonPosition() =>
            Math.Max(0, Math.Min(1, (CameraSystem.ZoomMultiplier - MinValue) / (MaxValue - MinValue)));

        public override void OnClick() => CameraSystem.ZoomMultiplier = (float)Value;
    }

    public static bool IsActive;

    Button[] Buttons = null!;
    Color ButtonColor;
    ZoomSlider Slider = null!;
    Panel_Layout PhotoModeLayout;
    double Alpha;
    (string name, string tooltip, int X, int Y)? pendingText;

    protected override void CheckActive() => Active = IsActive;

    public override void SetupContent()
    {
        Buttons = new Button[4];
        ButtonColor = new Color(255, 255, 255, 220);
        for (int i = 0; i < 4; i++)
            Buttons[i] = new Button(i, this);
        Slider = new ZoomSlider(CameraSystem.MinZoom, CameraSystem.MaxZoom, 1);
        RemoveButton = new RemoveButton(() => { IsActive = false; });

        PanelWidth = S(206);
        PanelHeight = S(112);
        PanelColor = new Color(43, 56, 101, 200);

        Layout = CreateLayout(PanelWidth, PanelHeight);
        PhotoModeLayout = CreateLayout();

        Alpha = 1.0;
    }

    protected override void ResizePanel()
    {
        PanelWidth = S(206);
        PanelHeight = S(112);
    }

    protected override void DrawSelf()
    {
        DrawSlider();
        DrawButtons();
        DrawText();
    }

    void DrawSlider()
    {
        ModSlider slider = Slider;
        Texture2D texture = slider.GetBackTexture();
        ref Rectangle rect = ref slider.Area;
        rect.X = (int)(Panel.X + S(16));
        rect.Y = (int)(Panel.Y + S(30));
        rect.Width = (int)(texture.Width * UIScale());
        rect.Height = (int)(texture.Height * UIScale());
        ModSlider.UpdateSlider(slider, rect);
        if (slider.Hovered) IsHoveredAnyElement = true;
        slider.Draw(rect);
    }

    void DrawButtons()
    {
        Texture2D texture = Textures.Get("TB_Button");
        int count = Buttons.Length;
        float buttonW = texture.Width * UIScale();
        float buttonH = texture.Height * UIScale();
        float spacing = 2 * UIScale();
        float startX = Panel.X + S(28);
        float startY = Panel.Y + S(58);
        int hovered = -1;
        for (int i = 0; i < count; i++)
        {
            Button button = Buttons[i];
            float x = startX + (i % 4) * (buttonW + spacing);
            float y = startY + (i / 4) * (buttonH + spacing);
            ref Rectangle rect = ref button.Area;
            rect.X = (int)x;
            rect.Y = (int)y;
            rect.Width = (int)buttonW;
            rect.Height = (int)buttonH;
            ModButton.UpdateButton(button, rect);
            if (button.Hovered)
            {
                IsHoveredAnyElement = true;
                hovered = i;
            }
            button.Draw(rect, button.GetIconTexture());
            if ((int)CameraSystem.CameraMode == i && i < 3)
                DrawHelper.Rectangle(Textures.Get("TB_Button_Highlight"), rect, new Color(255, 220, 0));
        }
        pendingText = null;
        if (hovered != -1 && Main.mouseLeft)
        {
            pendingText = (
                ModLocalization.Translate("ButtonName.CS_Button_" + hovered),
                ModLocalization.Translate("ButtonTooltip.CS_Button_" + hovered),
                Main.mouseX - 64,
                Main.mouseY - 78);
        }
    }

    void DrawText()
    {
        // Keeps the game's "Zoom" label and shows the multiplier after the colon.
        string zoom = Language.GetText("GameUI.GameZoom").Value ?? "";
        int colon = zoom.IndexOf(':');
        if (colon >= 0)
            zoom = zoom.Substring(0, colon) + ": x" + CameraSystem.ZoomMultiplier.ToString("F2", CultureInfo.InvariantCulture);
        DrawHelper.BorderString(
            DrawHelper.MouseTextFont(),
            zoom,
            new Vector2(Panel.X + S(16), Panel.Y + S(12)),
            Color.White,
            S(0.65f));

        if (pendingText is (string name, string tooltip, int x, int y))
        {
            DrawHelper.NewSpriteBatchForUI();
            DrawHelper.BuffTooltip(name, tooltip, x, y);
        }
    }

    public void DrawPhotoMode()
    {
        var rect = new Rectangle(
            (int)(Main.screenWidth * 0.5f - S(50)),
            (int)(Main.screenHeight - S(100)),
            (int)S(110),
            (int)S(65));
        double fade = Math.Max(0, Alpha);
        DrawHelper.Rectangle(Textures.Get("ScreenLocked"), rect, new Color((int)(255 * fade), (int)(255 * fade), (int)(255 * fade), (int)(180 * fade)));
        Recalculate(PhotoModeLayout, rect);

        if (Main.mouseLeft && Main.mouseLeftRelease && rect.Contains(Main.mouseX, Main.mouseY))
        {
            Alpha = 1.0;
            CameraSystem.PhotoMode = false;
        }
        else if (Alpha > 0)
        {
            Alpha -= 0.01;
        }
    }
}
