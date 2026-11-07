using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace GnorpBuilderMod.Common;

// The small panel that picks which of Gnorp's five shops the chat button opens.
public class PageButton : UIPanel
{
    public readonly UIText Label;

    public PageButton(int index, Action<int> onPick)
    {
        Width.Set(100f, 0f);
        Height.Set(40f, 0f);
        Top.Set(10 + index * 55, 0f);
        BackgroundColor = Color.Transparent;
        BorderColor = Color.Transparent;
        Label = new UIText("", 1f);
        Label.HAlign = 0.5f;
        Label.VAlign = 0.5f;
        Label.IgnoresMouseInteraction = true;
        Append(Label);
        OnLeftClick += (evt, element) =>
        {
            SoundEngine.PlaySound(SoundID.MenuTick);
            onPick(index);
        };
    }
}

public class ShopSelectState : UIState
{
    public static readonly string[] Pages = { "Main", "Bases", "Module", "Utility", "Holiday" };

    UIPanel panel = null!;
    readonly PageButton[] buttons = new PageButton[Pages.Length];

    public override void OnInitialize()
    {
        panel = new UIPanel();
        panel.SetPadding(10f);
        panel.Width.Set(120f, 0f);
        panel.Height.Set(300f, 0f);
        panel.HAlign = 0.5f;
        panel.VAlign = 0.5f;
        panel.BackgroundColor = new Color(50, 50, 120, 210);
        Append(panel);
        for (int i = 0; i < Pages.Length; i++)
        {
            buttons[i] = new PageButton(i, page => ShopSelectSystem.Instance.Pick(page));
            panel.Append(buttons[i]);
        }
    }

    public void Refresh(int current)
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Label.SetText(Util.T("NPCs.Gnorp.Shops." + Pages[i]));
            buttons[i].Label.TextColor = i == current ? Color.Yellow : Color.White;
        }
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        if (panel.ContainsPoint(Main.MouseScreen)) Main.LocalPlayer.mouseInterface = true;
        if (Main.LocalPlayer.talkNPC < 0) ShopSelectSystem.Instance.Hide();
    }
}

public class ShopSelectSystem : ModSystem
{
    public static ShopSelectSystem Instance = null!;

    UserInterface? ui;
    ShopSelectState state = null!;
    Action<int>? onPick;

    public override void PostSetupContent()
    {
        Instance = this;
        ui = new UserInterface();
        state = new ShopSelectState();
        state.Activate();
    }

    public void Show(int current, Action<int> pick)
    {
        if (ui == null) return;
        onPick = pick;
        state.Refresh(current);
        ui.SetState(state);
    }

    public void Pick(int page)
    {
        Action<int>? pick = onPick;
        Hide();
        pick?.Invoke(page);
    }

    public void Hide()
    {
        onPick = null;
        ui?.SetState(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ui?.CurrentState != null) ui.Update(gameTime);
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        int index = layers.FindIndex(layer => layer.Name == "Vanilla: Mouse Text");
        if (index == -1) return;
        layers.Insert(index, new LegacyGameInterfaceLayer("GnorpBuilder: Shop UI", delegate
        {
            if (ui?.CurrentState != null) ui.Draw(Main.spriteBatch, new GameTime());
            return true;
        }, InterfaceScaleType.UI));
    }
}
