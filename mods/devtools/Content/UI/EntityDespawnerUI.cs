using DevToolMod.Content.UI.Elements;
using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using static DevToolMod.TL.Utilities.UIScaleHelper;

namespace DevToolMod.Content.UI;

public class EntityDespawnerUI : PopupUI
{
    sealed class Button : ModButton
    {
        readonly EntityDespawnerUI Parent;
        readonly int Index;

        public Button(int idx, EntityDespawnerUI parent)
        {
            Parent = parent;
            Index = idx;
        }

        public override Color GetColor() => Parent.ButtonColor;

        public override Texture2D GetIconTexture() => Textures.Get("ED_Icon_" + Index);

        public override Texture2D GetTexture()
        {
            string name = "TB_Button";
            if (Clicked) name += "_Pressed";
            else if (Hovered) name += "_Hovered";
            return Textures.Get(name);
        }

        static void ClearNPCs()
        {
            for (int i = 0; i < 200; i++)
            {
                NPC entity = Main.npc[i];
                if (entity.active) entity.active = false;
            }
        }

        static void ClearItems()
        {
            for (int i = 0; i < 400; i++)
            {
                WorldItem entity = Main.item[i];
                // WorldItem.active is read only (a getter and no setter), and its own way of
                // clearing itself is TurnToAir. The other four entity kinds have a writable field.
                if (entity.active) entity.TurnToAir();
            }
        }

        static void ClearProjectiles()
        {
            for (int i = 0; i < 1000; i++)
            {
                Projectile entity = Main.projectile[i];
                if (entity.active) entity.active = false;
            }
        }

        static void ClearDusts()
        {
            for (int i = 0; i < 6000; i++)
            {
                Dust entity = Main.dust[i];
                if (entity.active) entity.active = false;
            }
        }

        static void ClearGores()
        {
            for (int i = 0; i < 600; i++)
            {
                Gore entity = Main.gore[i];
                if (entity.active) entity.active = false;
            }
        }

        public override void OnClick()
        {
            switch (Index)
            {
                case 0: ClearNPCs(); break;
                case 1: ClearItems(); break;
                case 2: ClearProjectiles(); break;
                case 3: ClearGores(); break;
                case 4: ClearDusts(); break;
            }
        }
    }

    public static bool IsActive;

    Button[] Buttons = null!;
    Color ButtonColor;
    (string name, int X, int Y)? pendingText;

    protected override void CheckActive() => Active = IsActive;

    public override void SetupContent()
    {
        Buttons = new Button[5];
        ButtonColor = new Color(255, 255, 255, 220);
        for (int i = 0; i < 5; i++)
            Buttons[i] = new Button(i, this);
        RemoveButton = new RemoveButton(() => { IsActive = false; });

        PanelWidth = S(204);
        PanelHeight = S(76);
        PanelColor = new Color(43, 56, 101, 200);

        Layout = CreateLayout(PanelWidth, PanelHeight);
    }

    protected override void ResizePanel()
    {
        PanelWidth = S(204);
        PanelHeight = S(76);
    }

    protected override void DrawSelf()
    {
        DrawButtons();
        DrawText();
    }

    void DrawButtons()
    {
        Texture2D texture = Textures.Get("TB_Button");
        int count = Buttons.Length;
        float buttonW = texture.Width * UIScale();
        float buttonH = texture.Height * UIScale();
        float spacing = 2 * UIScale();
        float startX = Panel.X + S(8);
        int hovered = -1;
        for (int i = 0; i < count; i++)
        {
            Button button = Buttons[i];
            float x = startX + (i % 5) * (buttonW + spacing);
            float y = Panel.Y + S(32);
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
        }
        pendingText = null;
        if (hovered != -1 && Main.mouseLeft)
            pendingText = (ModLocalization.Translate("ButtonName.ED_Button_" + hovered), Main.mouseX - 64, Main.mouseY - 72);
    }

    void DrawText()
    {
        if (pendingText is (string name, int x, int y))
        {
            DrawHelper.NewSpriteBatchForUI();
            DrawHelper.BuffTooltip(name, "", x, y);
        }
    }
}
