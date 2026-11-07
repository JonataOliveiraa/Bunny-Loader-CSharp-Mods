using DevToolMod.Core.Systems;
using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Light;
using static DevToolMod.TL.Utilities.UIScaleHelper;

namespace DevToolMod.Content.UI;

public class ToolBarUI_2 : ModInterface
{
    sealed class Button : ModButton
    {
        readonly ToolBarUI_2 Parent;
        readonly int Index;

        public Button(int idx, ToolBarUI_2 parent)
        {
            Parent = parent;
            Index = idx;
        }

        public override Color GetColor() => Parent.ButtonColor;

        public override Texture2D GetIconTexture() => Textures.Get("TB2_Icon_" + Index);

        public override Texture2D GetTexture()
        {
            string name = "TB_Button";
            if (Clicked) name += "_Pressed";
            else if (Hovered) name += "_Hovered";
            return Textures.Get(name);
        }

        public override void OnClick()
        {
            switch (Index)
            {
                case 0:
                    OmnipotentBuilding.IsActive = !OmnipotentBuilding.IsActive;
                    break;
                case 1:
                    MagnetSystem.IsActive = !MagnetSystem.IsActive;
                    break;
                case 2:
                    PlayerSystem.InfiniteMinions = !PlayerSystem.InfiniteMinions;
                    break;
                case 3:
                    WorldIconUI.IsActive = !WorldIconUI.IsActive;
                    break;
                case 4:
                    LightingSystem.IsActive = !LightingSystem.IsActive;
                    Lighting.Mode = LightMode.Color;
                    break;
            }
        }

        public override void Draw(Rectangle rect, Texture2D icon = default)
        {
            base.Draw(rect, icon);
            bool flag = false;
            if (Index == 0 && OmnipotentBuilding.IsActive)
                flag = true;
            else if (Index == 1 && MagnetSystem.IsActive)
                flag = true;
            else if (Index == 2 && PlayerSystem.InfiniteMinions)
                flag = true;
            else if (Index == 3 && WorldIconUI.IsActive)
                flag = true;
            else if (Index == 4 && LightingSystem.IsActive)
                flag = true;
            if (flag) DrawHelper.Rectangle(Textures.Get("TB_Button_Highlight"), rect, new Color(255, 255, 0));
        }
    }

    public static ToolBarUI_2 Instance = null!;

    public static void ToggleUI() => Instance.collapsed = !Instance.collapsed;

    public static bool Collapsed => Instance.collapsed;

    bool Active;
    Texture2D PanelBG;
    Rectangle Panel = new(0, 0, 1, 1);
    Button[] Buttons = null!;
    Color PanelColor;
    Color ButtonColor;
    bool collapsed = true;
    Panel_Layout Layout;

    public ToolBarUI_2()
    {
        Instance = this;
    }

    // Collapsing the main toolbar also collapses this one (ToolBarUI's collapse button does it),
    // so this bar's own state is enough.
    void CheckActive() => Active = !collapsed;

    public override void SetupContent()
    {
        // Panel
        PanelBG = Textures.Get("TB2_Panel");
        // Buttons
        Buttons = new Button[5];
        for (int i = 0; i < 5; i++)
            Buttons[i] = new Button(i, this);
        // Colors
        PanelColor = new Color(255, 255, 255, 180);
        ButtonColor = new Color(255, 255, 255, 220);
        // Properties
        collapsed = true;

        Layout = CreateLayout(S(PanelBG.Width), S(PanelBG.Height));
    }

    public override void Clear()
    {
        collapsed = true;

        OmnipotentBuilding.IsActive = false;
        MagnetSystem.IsActive = false;
        LightingSystem.IsActive = false;
        PlayerSystem.InfiniteMinions = false;
        WorldIconUI.IsActive = false;
    }

    void GetPanelRect(ref Rectangle rect)
    {
        float w = S(PanelBG.Width), h = S(PanelBG.Height);
        float x = Main.screenWidth * 0.5f - w * 0.5f;
        float y = Main.screenHeight - h * 2 + S(4);
        rect.X = (int)x;
        rect.Y = (int)y;
        rect.Width = (int)w;
        rect.Height = (int)h;
    }

    public override void Draw()
    {
        CheckActive();
        if (Active)
        {
            DrawPanelBG();
            ((ToolBarUI)GetByName("ToolBarUI")).DrawCollapseButton();
            DrawButtons();
        }
    }

    void DrawPanelBG()
    {
        GetPanelRect(ref Panel);
        Recalculate(Layout, Panel);
        DrawHelper.Rectangle(PanelBG, Panel, PanelColor);
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
            DrawHelper.BuffTooltip(
                ModLocalization.Translate("ButtonName.TB2_Button_" + hovered),
                ModLocalization.Translate("ButtonTooltip.TB2_Button_" + hovered),
                Main.mouseX - 64,
                Main.mouseY - 78);
        }
    }
}
