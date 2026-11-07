using DevToolMod.Core.Systems;
using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Localization;
using static DevToolMod.TL.Utilities.UIScaleHelper;

namespace DevToolMod.Content.UI;

public class ToolBarUI : ModInterface
{
    sealed class Button : ModButton
    {
        readonly ToolBarUI Parent;
        readonly int Index;

        public Button(int idx, ToolBarUI parent)
        {
            Parent = parent;
            Index = idx;
        }

        public override Color GetColor() => Parent.ButtonColor;

        public override Texture2D GetIconTexture()
        {
            string name = "TB_Icon_" + Index;

            switch (Index)
            {
                case 0:
                    name += "_" + Main.GameMode;
                    break;
                case 2:
                    switch (HitboxSystem.HitboxMode)
                    {
                        case HitboxMode.NPCs:
                            name += "_Red";
                            break;
                        case HitboxMode.Projectiles:
                            name += "_Blue";
                            break;
                        case HitboxMode.NPCsAndProjectiles:
                            name += "_Both";
                            break;
                    }
                    break;
                case 6:
                    name += !DebugScreen.IsActive ? "_Show" : "_Hide";
                    break;
            }

            return Textures.Get(name);
        }

        public override Texture2D GetTexture()
        {
            string name = "TB_Button";
            if (Clicked) name += "_Pressed";
            else if (Hovered) name += "_Hovered";
            return Textures.Get(name);
        }

        public override int? ClickSound()
        {
            if (Index == 6)
                return !DebugScreen.IsActive ? 10 : 11;
            return base.ClickSound();
        }

        public override void OnClick()
        {
            switch (Index)
            {
                case 0:
                    Main.ActiveWorldFileData.GameMode = Main.GameMode >= 3 ? 0 : Main.GameMode + 1;
                    break;
                case 1:
                    EntityDespawnerUI.IsActive = !EntityDespawnerUI.IsActive;
                    break;
                case 2:
                    HitboxSystem.HitboxMode++;
                    if (HitboxSystem.HitboxMode >= HitboxMode.Count)
                        HitboxSystem.HitboxMode = HitboxMode.None;
                    break;
                case 3:
                    ToolBarUI_2.ToggleUI();
                    break;
                case 4:
                    WeatherUI.IsActive = !WeatherUI.IsActive;
                    break;
                case 5:
                    CameraUI.IsActive = !CameraUI.IsActive;
                    break;
                case 6:
                    DebugScreen.IsActive = !DebugScreen.IsActive;
                    break;
            }
        }
    }

    sealed class CollapseButtonElement : ModButton
    {
        readonly ToolBarUI Parent;

        public Panel_Layout Layout;

        public CollapseButtonElement(ToolBarUI parent)
        {
            Parent = parent;
        }

        public override Color GetColor() => Parent.PanelColor;

        public override Texture2D GetTexture() => Textures.Get(Parent.Collapsed ? "TB_CollapseButton_0" : "TB_CollapseButton_1");

        public override int? ClickSound() => Parent.Collapsed ? 10 : 11;

        public override void OnClick()
        {
            Parent.Collapsed = !Parent.Collapsed;
            if (!ToolBarUI_2.Collapsed) ToolBarUI_2.ToggleUI();
        }
    }

    bool Active;
    Texture2D PanelBG;
    Rectangle Panel = new(0, 0, 1, 1);
    Button[] Buttons = null!;
    CollapseButtonElement CollapseButton = null!;
    Color PanelColor;
    Color ButtonColor;
    bool Collapsed;
    Panel_Layout Layout;

    void CheckActive() => Active = true;

    public override void SetupContent()
    {
        // Panel
        PanelBG = Textures.Get("TB_Panel");
        // Buttons
        Buttons = new Button[7];
        for (int i = 0; i < 7; i++)
            Buttons[i] = new Button(i, this);
        CollapseButton = new CollapseButtonElement(this);
        // Colors
        PanelColor = new Color(255, 255, 255, 180);
        ButtonColor = new Color(255, 255, 255, 220);
        // Properties
        Collapsed = true;

        Layout = CreateLayout(S(PanelBG.Width), S(PanelBG.Height));
        CollapseButton.Layout = CreateLayout(S(52), S(14));
    }

    public override void Clear()
    {
        Collapsed = true;
        EntityDespawnerUI.IsActive = false;
        HitboxSystem.HitboxMode = HitboxMode.None;
        CameraSystem.Clear();
        CameraUI.IsActive = false;
        WeatherUI.IsActive = false;
        DebugScreen.IsActive = false;
    }

    void GetPanelRect(ref Rectangle rect)
    {
        if (Collapsed)
        {
            CalculateCollapseButton(ref rect);
        }
        else
        {
            float w = S(PanelBG.Width), h = S(PanelBG.Height);
            float x = Main.screenWidth * 0.5f - w * 0.5f;
            float y = Main.screenHeight - h;
            rect.X = (int)x;
            rect.Y = (int)y;
            rect.Width = (int)w;
            rect.Height = (int)h;
        }
    }

    void CalculateCollapseButton(ref Rectangle rect)
    {
        Texture2D texture = Textures.Get("TB_CollapseButton_0");
        float width = S(texture.Width);
        float height = S(texture.Height);
        float x = Main.screenWidth * 0.5f - width * 0.5f;
        float y = Main.screenHeight - height - (!Collapsed ? S(PanelBG.Height) * 0.95f : height);
        if (!ToolBarUI_2.Collapsed) y -= S(PanelBG.Height) - S(4);
        rect.X = (int)x;
        rect.Y = (int)y;
        rect.Width = (int)width;
        rect.Height = (int)height;
    }

    public override void Draw()
    {
        CheckActive();
        if (Active)
        {
            if (ToolBarUI_2.Collapsed) DrawCollapseButton();
            if (!Collapsed)
            {
                DrawPanelBG();
                DrawButtons();
            }
        }
    }

    void DrawPanelBG()
    {
        GetPanelRect(ref Panel);
        Recalculate(Layout, Panel);
        DrawHelper.Rectangle(PanelBG, Panel, PanelColor);
    }

    public void DrawCollapseButton()
    {
        CollapseButtonElement button = CollapseButton;
        ref Rectangle rect = ref button.Area;
        CalculateCollapseButton(ref rect);
        Recalculate(button.Layout, rect);
        ModButton.UpdateButton(button, rect);
        button.Draw(rect);
    }

    void DrawButtons()
    {
        Texture2D texture = Textures.Get("TB_Button");
        int count = Buttons.Length;
        float buttonW = texture.Width * UIScale();
        float buttonH = texture.Height * UIScale();
        float spacing = 4 * UIScale();
        float totalWidth = buttonW * count + spacing * (count - 1);
        float startX = Panel.X + (Panel.Width - totalWidth) * 0.5f;
        float y = Panel.Y + (Panel.Height - buttonH) * 0.5f;

        int hovered = -1;
        for (int i = 0; i < count; i++)
        {
            Button button = Buttons[i];
            float x = startX + i * (buttonW + spacing);
            ref Rectangle rect = ref button.Area;
            rect.X = (int)x;
            rect.Y = (int)y;
            rect.Width = (int)buttonW;
            rect.Height = (int)buttonH;
            ModButton.UpdateButton(button, rect);
            button.Draw(rect, button.GetIconTexture());
            if (button.Hovered) hovered = i;
        }
        if (hovered != -1 && Main.mouseLeft)
        {
            string buffTooltip = ModLocalization.Translate("ButtonTooltip.TB_Button_" + hovered);
            if (hovered == 0)
            {
                string diffText = "UI.Normal";
                switch (Main.GameMode)
                {
                    case 3:
                        diffText = "UI.Creative";
                        break;
                    case 2:
                        diffText = "UI.Master";
                        break;
                    case 1:
                        diffText = "UI.Expert";
                        break;
                }
                buffTooltip = buffTooltip.Replace("{0}", Language.GetText(diffText).Value);
            }
            DrawHelper.BuffTooltip(
                ModLocalization.Translate("ButtonName.TB_Button_" + hovered),
                buffTooltip,
                Main.mouseX - 64,
                Main.mouseY - 78);
        }
    }
}
