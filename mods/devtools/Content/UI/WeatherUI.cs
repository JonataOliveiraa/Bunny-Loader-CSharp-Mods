using System;
using DevToolMod.Content.UI.Elements;
using DevToolMod.Core.Systems;
using DevToolMod.TL;
using DevToolMod.TL.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.Localization;
using static DevToolMod.TL.Utilities.UIScaleHelper;

namespace DevToolMod.Content.UI;

public class WeatherUI : PopupUI
{
    sealed class Button : ModButton
    {
        readonly WeatherUI Parent;
        readonly int Index;

        public Button(int idx, WeatherUI parent)
        {
            Parent = parent;
            Index = idx;
        }

        public override Color GetColor() => Parent.ButtonColor;

        public override Texture2D GetIconTexture()
        {
            string name = "WUI_Icon_" + Index;
            if (Index == 4)
                name += CreativePowerAccess.TimeFrozen ? "_1" : "_0";
            if (Index == 7)
                name += "_" + (int)Math.Floor(Main.maxRaining * 4 + 0.5);
            return Textures.Get(name);
        }

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
                    Main.SkipToTime(5400, true);
                    break;
                case 1:
                    Main.SkipToTime(27000, true);
                    break;
                case 2:
                    Main.SkipToTime(48600, true);
                    break;
                case 3:
                    Main.SkipToTime(16200, false);
                    break;
                case 4:
                    CreativePowerAccess.SetTimeFrozen(!CreativePowerAccess.TimeFrozen);
                    break;
                case 5:
                    Main.windSpeedCurrent = Main.windSpeedTarget = (float)Math.Max((double)Main.windSpeedTarget - 0.2, -0.8);
                    break;
                case 6:
                    Main.windSpeedCurrent = Main.windSpeedTarget = (float)Math.Min((double)Main.windSpeedTarget + 0.2, 0.8);
                    break;
                case 7:
                    if (Main.maxRaining >= 1)
                    {
                        Main.StopRain(true);
                    }
                    else
                    {
                        Main.raining = true;
                        Main.rainTime = 86400;
                        Main.maxRaining = (float)(Math.Ceiling(((double)Main.maxRaining + 0.001) * 4) / 4);
                        Main.cloudAlpha = Main.maxRaining;
                    }
                    break;
            }
        }
    }

    sealed class TimeSlider : ModSlider
    {
        public TimeSlider(double min, double max) : base(min, max) { }

        public override Texture2D GetBackTexture() => Textures.Get("WUI_Slider_Back");

        public override Texture2D GetButtonTexture() => Textures.Get("WUI_Slider_Button");

        public override double GetButtonPosition()
        {
            double time = Main.dayTime ? Main.time : Main.time + 54000;
            return Math.Max(0, Math.Min(time / 86399, 1));
        }

        public override void OnClick()
        {
            double time = Math.Floor(Value * 86399);
            if (time < 54000)
            {
                Main.dayTime = true;
                Main.time = time;
            }
            else
            {
                Main.dayTime = false;
                Main.time = time - 54000;
            }
        }
    }

    sealed class FastSliderElement : ModSlider
    {
        public FastSliderElement(double min, double max) : base(min, max) { }

        public override Texture2D GetBackTexture() => Textures.Get("WUI_Slider2_Back");

        public override Texture2D GetButtonTexture() => Textures.Get("WUI_Slider2_Button");

        public override double GetButtonPosition() =>
            Math.Max(0, Math.Min(CreativePowerAccess.TargetTimeRate / 100.0, 1));

        public override void OnClick() =>
            CreativePowerAccess.TargetTimeRate = (int)Math.Max(1, Math.Min(Math.Floor(Value * 100), 100));
    }

    public static bool IsActive;

    Button[] Buttons = null!;
    Color ButtonColor;
    TimeSlider Slider = null!;
    FastSliderElement FastSlider = null!;
    string weatherText = "";
    string windText = "";
    (string name, string tooltip, int X, int Y)? pendingText;

    protected override void CheckActive() => Active = IsActive;

    public override void SetupContent()
    {
        Buttons = new Button[8];
        ButtonColor = new Color(255, 255, 255, 220);
        for (int i = 0; i < 8; i++)
            Buttons[i] = new Button(i, this);
        Slider = new TimeSlider(0, 1);
        FastSlider = new FastSliderElement(0, 1);
        RemoveButton = new RemoveButton(() => { IsActive = false; });

        PanelWidth = S(178);
        PanelHeight = S(212);
        PanelColor = new Color(43, 56, 101, 200);

        Layout = CreateLayout(PanelWidth, PanelHeight);
    }

    protected override void ResizePanel()
    {
        PanelWidth = S(178);
        PanelHeight = S(212);
    }

    static string GetTimeText()
    {
        double time = Main.dayTime ? Main.time : Main.time + 54000;
        double hours = time / 86400 * 24 - 19.5;
        if (hours < 0) hours += 24;
        int minutes = (int)Math.Floor(hours % 1 * 60);
        int wholeHours = (int)Math.Floor(hours);
        string? period = Language.GetText(wholeHours >= 12 ? "GameUI.TimePastMorning" : "GameUI.TimeAtMorning").Value;
        if (wholeHours > 12) wholeHours -= 12;
        if (wholeHours == 0) wholeHours = 12;
        return $"{wholeHours}:{minutes.ToString().PadLeft(2, '0')} {period}";
    }

    static string GetTimeSpeedText()
    {
        if (CreativePowerAccess.TimeFrozen) return Language.GetText("CreativePowers.FreezeTime_Enabled").Value ?? "";
        return ModLocalization.Translate("GameUI.TargetTimeRate").Replace("{0}", CreativePowerAccess.TargetTimeRate.ToString());
    }

    void GetWeatherText()
    {
        // In a sandstorm the texts keep their previous values, as in the JavaScript version:
        // it returned the sandstorm text here, and its caller ignored the return value.
        if (Sandstorm.Happening) return;
        string text;
        if (Main.IsItStorming)
            text = "GameUI.Storm";
        else if (Main.maxRaining > 0.6)
            text = "GameUI.HeavyRain";
        else if (Main.maxRaining >= 0.2)
            text = "GameUI.Rain";
        else if (Main.maxRaining > 0)
            text = "GameUI.LightRain";
        else if (Main.cloudBGActive > 0)
            text = "GameUI.Overcast";
        else if (Main.numClouds > 90)
            text = "GameUI.MostlyCloudy";
        else if (Main.numClouds > 55)
            text = "GameUI.Cloudy";
        else if (Main.numClouds > 15)
            text = "GameUI.PartlyCloudy";
        else
            text = "GameUI.Clear";
        int wind = (int)Math.Floor(Main.windSpeedCurrent * 50.0);
        string text2 = "";
        if (wind < 0) text2 += (Language.GetText("GameUI.EastWind").Value ?? "").Replace("{0}", Math.Abs(wind).ToString());
        else if (wind > 0) text2 += (Language.GetText("GameUI.WestWind").Value ?? "").Replace("{0}", wind.ToString());
        weatherText = Language.GetText(text).Value ?? "";
        windText = text2.TrimStart();
    }

    protected override void DrawSelf()
    {
        GetWeatherText();
        DrawSlider(Slider, 60);
        DrawSlider(FastSlider, 102);
        DrawButtons();
        DrawText();
    }

    void DrawSlider(ModSlider slider, float offsetY)
    {
        Texture2D texture = slider.GetBackTexture();
        ref Rectangle rect = ref slider.Area;
        rect.X = (int)(Panel.X + S(14));
        rect.Y = (int)(Panel.Y + S(offsetY));
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
        float startX = Panel.X + S(14);
        float startY = Panel.Y + S(124);
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
        }
        pendingText = null;
        if (hovered != -1 && Main.mouseLeft)
        {
            string buttonTooltip = ModLocalization.Translate("ButtonTooltip.WUI_Button_" + hovered);
            switch (hovered)
            {
                case 5:
                case 6:
                    buttonTooltip = buttonTooltip.Replace("{0}", windText);
                    break;
                case 7:
                    buttonTooltip = buttonTooltip.Replace("{0}", weatherText);
                    break;
            }
            pendingText = (
                ModLocalization.Translate("ButtonName.WUI_Button_" + hovered),
                buttonTooltip,
                Main.mouseX - 64,
                Main.mouseY - 78);
        }
    }

    void DrawText()
    {
        SpriteFont font = DrawHelper.MouseTextFont();
        var pos = new Vector2(Panel.X + S(14), Panel.Y + S(10));
        DrawHelper.BorderString(font, $"{GetTimeText()}\n{weatherText}\n{windText}", pos, Color.White, S(0.65f));
        pos.Y = Panel.Y + S(86);
        DrawHelper.BorderString(font, GetTimeSpeedText(), pos, Color.White, S(0.65f));

        if (pendingText is (string name, string tooltip, int x, int y))
        {
            DrawHelper.NewSpriteBatchForUI();
            DrawHelper.BuffTooltip(name, tooltip, x, y);
        }
    }
}
