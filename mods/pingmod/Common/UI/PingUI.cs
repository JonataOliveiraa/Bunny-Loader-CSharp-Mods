using System;
using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using static PingModPort.Common.UI.Layout;

namespace PingModPort.Common.UI;

public class PingState : UIState
{
    public const float BOARD_SIZE = 22;
    public const float BOARD_GAP = 6;
    const float INDICATOR_WIDTH = 50;
    const float INDICATOR_HEIGHT = 56;
    const float INDICATOR_GAP = 6;
    const float ICON_SIZE = 32;

    public DraggablePanel Indicator = null!;
    UIImage icon = null!;
    UIText text = null!;
    UIImageButton board = null!;
    int lastPing = -1, lastStyle = -1;
    bool shown = true;
    float? lastLeft, lastTop;
    float? boardLeft, boardTop;

    public override void OnInitialize()
    {
        DraggablePanel panel = Indicator = new DraggablePanel();
        panel.SetPadding(0);
        panel.Width.Set(INDICATOR_WIDTH, 0);
        panel.Height.Set(INDICATOR_HEIGHT, 0);
        panel.Left.Set(-25, 0.5f);
        panel.Top.Set(0, 0.1f);
        panel.BackgroundColor = Color.Transparent;
        panel.BorderColor = Color.Transparent;
        panel.onTap = () => PingUISystem.Instance.ToggleSettings();
        Append(panel);

        icon = new UIImage(PingConfig.PingTexture(PingConfig.StyleIndex, 0));
        icon.HAlign = 0.5f;
        icon.Top.Set(2, 0);
        icon.IgnoresMouseInteraction = true;
        panel.Append(icon);

        text = new UIText("0", 0.9f);
        text.HAlign = 0.5f;
        text.Top.Set(36, 0);
        text.IgnoresMouseInteraction = true;
        panel.Append(text);

        board = new UIImageButton(ModContent.Request<Texture2D>("pingmod/UI/Sprites/PlayerboardIcon"));
        board.Width.Set(BOARD_SIZE, 0);
        board.Height.Set(BOARD_SIZE, 0);
        board.Left.Set(-BOARD_SIZE / 2, 0.5f);
        board.Top.Set(4, 0);
        board.OnLeftClick += (evt, element) => PingUISystem.Instance.ToggleBoard();
        Append(board);
    }

    public override void Update(GameTime gameTime)
    {
        PingConfig config = PingConfig.Instance;
        bool show = config == null || config.ShowIndicator;
        if (show != shown)
        {
            shown = show;
            if (show)
                Append(Indicator);
            else
                RemoveChild(Indicator);
        }
        if (show)
            FollowLifeBar(config);
        PlaceBoardButton();
        base.Update(gameTime);
        int ping = Ping.MyPing();
        int style = PingConfig.StyleIndex;
        if (ping == lastPing && style == lastStyle)
            return;
        lastPing = ping;
        lastStyle = style;
        icon.SetImage(PingConfig.PingTexture(style, ping));
        text.SetText(ping.ToString());
        text.TextColor = PingConfig.PingColor(ping);
    }

    static void LifeBarRect(out float x, out float y, out float h)
    {
        if (LifeBarTracker.MeasuredLifeBar(out x, out y, out h))
            return;
        int rows = Main.LocalPlayer.statLifeMax2 > 200 ? 2 : 1;
        x = Main.screenWidth - 296;
        y = 15;
        h = 28 * (rows - 1) + 26;
    }

    void FollowLifeBar(PingConfig? config)
    {
        DraggablePanel panel = Indicator;
        bool attach = config == null || config.AttachToHealthBar;
        panel.locked = attach;
        if (!attach)
        {
            lastLeft = lastTop = null;
            return;
        }
        LifeBarRect(out float x, out float y, out float h);
        float left = x - INDICATOR_GAP - INDICATOR_WIDTH;
        float top = y + h / 2 - INDICATOR_HEIGHT / 2;
        if (left == lastLeft && top == lastTop)
            return;
        lastLeft = left;
        lastTop = top;
        panel.HAlign = 0;
        panel.VAlign = 0;
        panel.Left.Set(left, 0);
        panel.Top.Set(Math.Max(0, top), 0);
        panel.Recalculate();
    }

    void PlaceBoardButton()
    {
        float left = Main.screenWidth / 2f - BOARD_SIZE / 2, top = 4;
        if (shown)
        {
            CalculatedStyle d = Indicator.GetDimensions();
            if (d.Width > 0)
            {
                left = d.X - BOARD_GAP - BOARD_SIZE;
                top = d.Y + Math.Min(d.Height, ICON_SIZE + 4) / 2 - BOARD_SIZE / 2;
            }
        }
        left = Ping.Round(Math.Min(Math.Max(0, left), Main.screenWidth - BOARD_SIZE));
        top = Ping.Round(Math.Max(0, top));
        if (left == boardLeft && top == boardTop)
            return;
        boardLeft = left;
        boardTop = top;
        board.HAlign = 0;
        board.Left.Set(left, 0);
        board.Top.Set(top, 0);
        board.Recalculate();
    }

    public Vector2 BoardAnchor()
    {
        float bottom = (boardTop ?? 4) + BOARD_SIZE;
        float y = bottom;
        if (shown)
        {
            CalculatedStyle d = Indicator.GetDimensions();
            if (d.Height > 0)
                y = Math.Max(bottom, d.Y + d.Height);
        }
        return new Vector2(boardLeft ?? Main.screenWidth / 2f, y);
    }
}

public class SettingsState : UIState
{
    UIImage icon = null!;
    UIText styleText = null!;
    UIText timerText = null!;
    Asset<Texture2D>? shownIcon;

    public override void OnInitialize()
    {
        DraggablePanel panel = new DraggablePanel();
        panel.SetPadding(0);
        panel.Width.Set(300, 0);
        panel.Height.Set(170, 0);
        panel.Left.Set(-150, 0.5f);
        panel.Top.Set(-85, 0.5f);
        panel.BackgroundColor = DraggablePanel.PANEL_COLOR;
        panel.BorderColor = Color.Black;
        Append(panel);

        UIImageButton close = Place(new UIImageButton(ModContent.Request<Texture2D>("pingmod/UI/Sprites/CloseIcon")), 270, 6, 24, 24);
        close.OnLeftClick += (evt, element) =>
        {
            SoundEngine.PlaySound(SoundID.MenuClose, -1, -1, 1, 1f, 0f);
            PingUISystem.Instance.HideSettings();
        };
        panel.Append(close);

        var title = new UIText(Ping.T("Text.IconConfigText"), 1.1f);
        title.HAlign = 0.5f;
        title.Top.Set(12, 0);
        title.IgnoresMouseInteraction = true;
        panel.Append(title);

        panel.Append(Place(new TextButton("<", 26, 26, () => CycleStyle(-1)), 20, 58));
        icon = Place(new UIImage(PingConfig.PingTexture(PingConfig.StyleIndex, 0)), 52, 55, 32, 32);
        icon.IgnoresMouseInteraction = true;
        panel.Append(icon);
        panel.Append(Place(new TextButton(">", 26, 26, () => CycleStyle(1)), 90, 58));
        styleText = Place(new UIText("", 1f), 130, 62);
        styleText.IgnoresMouseInteraction = true;
        panel.Append(styleText);

        panel.Append(Place(new TextButton("<", 26, 26, () => ChangeTimer(-1)), 20, 112));
        timerText = Place(new UIText("", 1f), 52, 116);
        timerText.Width.Set(32, 0);
        timerText.IgnoresMouseInteraction = true;
        panel.Append(timerText);
        panel.Append(Place(new TextButton(">", 26, 26, () => ChangeTimer(1)), 90, 112));
        UIText timerLabel = Place(new UIText(Ping.T("Text.PingText"), 1f), 130, 116);
        timerLabel.IgnoresMouseInteraction = true;
        panel.Append(timerLabel);
        Refresh();
    }

    static void Save(Action<PingConfig> change)
    {
        var pending = (PingConfig)PingConfig.Instance.Clone();
        change(pending);
        PingConfig.Instance.SaveChanges(pending);
    }

    void CycleStyle(int dir)
    {
        int count = PingConfig.StyleCount;
        int next = (PingConfig.StyleIndex + dir + count) % count;
        Save(config => config.IconStyle = (PingIconStyle)next);
        Ping.SendSync();
        Refresh();
    }

    void ChangeTimer(int dir)
    {
        int value = Math.Min(10, Math.Max(1, PingConfig.Instance.PingSyncTimer + dir));
        Save(config => config.PingSyncTimer = value);
        Refresh();
    }

    void ShowIcon()
    {
        Asset<Texture2D> asset = PingConfig.PingTexture(PingConfig.StyleIndex, Ping.MyPing());
        if (ReferenceEquals(asset, shownIcon))
            return;
        shownIcon = asset;
        icon.SetImage(asset);
    }

    public void Refresh()
    {
        ShowIcon();
        styleText.SetText(Ping.T("Configs.PingIconStyle." + PingConfig.Instance.IconStyle + ".Label"));
        timerText.SetText(PingConfig.Instance.PingSyncTimer.ToString());
    }

    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
        ShowIcon();
    }
}

public class ServerPanel : UIPanel
{
    const float ROW_HEIGHT = 35;

    readonly List<int> rows = new();
    float? lastLeft, lastTop;

    public ServerPanel()
    {
        SetPadding(0);
        Width.Set(400, 0);
        Height.Set(53, 0);
        Left.Set(-200, 0.5f);
        Top.Set(0, 0.03f);
        BackgroundColor = DraggablePanel.PANEL_COLOR;
        BorderColor = Color.Black;
    }

    void Follow()
    {
        Vector2 at = PingUISystem.Instance.pingState.BoardAnchor();
        float width = Width.Pixels;
        float left = Ping.Round(Math.Min(Math.Max(0, at.X + PingState.BOARD_SIZE - width), Main.screenWidth - width));
        float top = Ping.Round(at.Y + PingState.BOARD_GAP);
        if (left == lastLeft && top == lastTop)
            return;
        lastLeft = left;
        lastTop = top;
        HAlign = 0;
        Left.Set(left, 0);
        Top.Set(top, 0);
        Recalculate();
    }

    public override void Update(GameTime gameTime)
    {
        Follow();
        base.Update(gameTime);
        if (ContainsPoint(Main.MouseScreen))
            Main.LocalPlayer.mouseInterface = true;
        int before = rows.Count;
        rows.Clear();
        for (int i = 0; i < 255; i++)
        {
            Player player = Main.player[i];
            if (!player.IsNull && player.active)
                rows.Add(i);
        }
        if (rows.Count != before)
        {
            Height.Set(53 + ROW_HEIGHT * Math.Max(0, rows.Count - 1), 0);
            Recalculate();
        }
    }

    // Drawing a head changes the batch's clipping, so the original puts it back afterwards.
    static void DrawHead(Camera camera, Player player, Vector2 position, Color color)
    {
        bool enabled = SpriteBatchItem.clippingEnabled;
        bool geometry = SpriteBatchItem.geometryScaleEnabled;
        bool vertical = SpriteBatchItem.clippingVertical;
        bool horizontal = SpriteBatchItem.clippingHorzontal;
        Rectangle inner = SpriteBatchItem.clippingInner;
        Rectangle outer = SpriteBatchItem.clippingOuter;
        SpriteBatch batch = SpriteBatchItem.clippingBatch;
        try
        {
            Main.PlayerRenderer.DrawPlayerHead(camera, player, position, 1f, 1f, color);
        }
        catch (GameException)
        {
        }
        finally
        {
            SpriteBatchItem.clippingEnabled = enabled;
            SpriteBatchItem.geometryScaleEnabled = geometry;
            SpriteBatchItem.clippingVertical = vertical;
            SpriteBatchItem.clippingHorzontal = horizontal;
            SpriteBatchItem.clippingInner = inner;
            SpriteBatchItem.clippingOuter = outer;
            SpriteBatchItem.clippingBatch = batch;
        }
    }

    protected override void DrawSelf(SpriteBatch spriteBatch)
    {
        base.DrawSelf(spriteBatch);
        CalculatedStyle inner = GetInnerDimensions();
        for (int row = 0; row < rows.Count; row++)
        {
            int index = rows[row];
            Player player = Main.player[index];
            PingModPlayer? state = Ping.StateOf(index);
            int ping = index == Main.myPlayer ? Ping.MyPing() : state?.currentPing ?? 0;
            int style = index == Main.myPlayer ? PingConfig.StyleIndex : state?.currentIconStyle ?? 0;
            float y = inner.Y + 15 + ROW_HEIGHT * row;
            Color color = Main.GetPlayerHeadBordersColor(player, false);
            string name = player.name ?? "";
            if (name.Length > 22)
                name = name.Substring(0, 22);
            Utils.DrawBorderString(spriteBatch, name, new Vector2(inner.X + inner.Width * 0.15f, y), color, 1f, 0f, 0f, -1);
            Utils.DrawBorderString(spriteBatch, ping.ToString(), new Vector2(inner.X + inner.Width * 0.725f, y), PingConfig.PingColor(ping), 1f, 0f, 0f, -1);
            spriteBatch.Draw(PingConfig.PingTexture(style, ping).Value, new Vector2(inner.X + inner.Width * 0.85f, y - 6), default(GameNullable<Rectangle>), Color.White,
                0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
        }
        // Heads last: they go through the game's player renderer and its own batch state.
        Camera camera = Main.Camera;
        if (camera.IsNull)
            return;
        for (int row = 0; row < rows.Count; row++)
        {
            Player player = Main.player[rows[row]];
            float y = inner.Y + 15 + ROW_HEIGHT * row;
            DrawHead(camera, player, new Vector2(inner.X + 30, y + 10), Main.GetPlayerHeadBordersColor(player, false));
        }
    }
}

public class BoardState : UIState
{
    public override void OnInitialize() => Append(new ServerPanel());
}

public class PingUISystem : ModSystem
{
    public static PingUISystem Instance = null!;

    public UserInterface pingUI = null!;
    public UserInterface settingsUI = null!;
    public UserInterface boardUI = null!;
    public PingState pingState = null!;
    SettingsState settingsState = null!;
    BoardState boardState = null!;
    GameInterfaceLayer? layer;
    UserInterface? otherUI;
    UIState? otherState;

    public override void PostSetupContent()
    {
        Instance = this;
        pingUI = new UserInterface();
        settingsUI = new UserInterface();
        boardUI = new UserInterface();
        pingState = new PingState();
        settingsState = new SettingsState();
        boardState = new BoardState();
        pingState.Activate();
        settingsState.Activate();
        boardState.Activate();
    }

    public override void OnWorldLoad()
    {
        Ping.Reset();
        if (pingUI == null)
            return;
        settingsUI.SetState(null);
        boardUI.SetState(null);
        pingUI.SetState(Ping.IsPlayingOnline ? pingState : null);
        if (Ping.IsPlayingOnline)
            Ping.SendSync();
    }

    public override void OnWorldUnload()
    {
        Ping.Reset();
        if (pingUI == null)
            return;
        pingUI.SetState(null);
        settingsUI.SetState(null);
        boardUI.SetState(null);
    }

    static bool OtherScreenOpen()
    {
        Player player = Main.LocalPlayer;
        return Main.playerInventory || Main.ingameOptionsWindow || Main.inFancyUI || Main.mapFullscreen || Main.gameMenu
            || player.talkNPC >= 0 || player.chest != -1 || player.sign >= 0;
    }

    public void ToggleSettings()
    {
        if (settingsUI.CurrentState != null)
        {
            HideSettings();
            return;
        }
        if (OtherScreenOpen())
            return;
        HideBoard();
        SoundEngine.PlaySound(SoundID.MenuOpen, -1, -1, 1, 1f, 0f);
        settingsState.Refresh();
        settingsUI.SetState(settingsState);
    }

    public void HideSettings()
    {
        if (settingsUI.CurrentState != null)
            settingsUI.SetState(null);
    }

    public void ToggleBoard()
    {
        if (boardUI.CurrentState != null)
        {
            HideBoard();
            return;
        }
        if (OtherScreenOpen())
            return;
        HideSettings();
        boardUI.SetState(boardState);
    }

    public void HideBoard()
    {
        if (boardUI.CurrentState != null)
            boardUI.SetState(null);
    }

    public void HidePanels()
    {
        HideSettings();
        HideBoard();
    }

    public override void PostUpdateEverything() => Ping.Tick();

    public override void UpdateUI(GameTime gameTime)
    {
        if (!Ping.IsPlayingOnline || pingUI == null)
            return;
        // Not every path into a multiplayer world has the netMode set by OnWorldLoad.
        if (pingUI.CurrentState == null)
            pingUI.SetState(pingState);
        if ((settingsUI.CurrentState != null || boardUI.CurrentState != null) && OtherScreenOpen())
            HidePanels();
        if (boardUI.CurrentState != null)
            boardUI.Update(gameTime);
        if (settingsUI.CurrentState != null)
            settingsUI.Update(gameTime);
        if (pingUI.CurrentState != null)
            pingUI.Update(gameTime);
    }

    // The original wrapped UserInterface.SetState to notice other mods opening a screen. In C#
    // that method cannot be hooked, so this looks at the interface drawn just before ours.
    void WatchOtherInterfaces()
    {
        UserInterface active = UserInterface.ActiveInstance;
        if (active == pingUI || active == settingsUI || active == boardUI)
            return;
        UIState? state = active.CurrentState;
        if (active == otherUI && state == otherState)
            return;
        otherUI = active;
        otherState = state;
        if (state != null)
            HidePanels();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (!Ping.IsPlayingOnline || pingUI == null)
            return;
        layer ??= new LegacyGameInterfaceLayer("PingMod: Ping UI", delegate
        {
            WatchOtherInterfaces();
            LifeBarTracker.CommitLifeBar();
            if (pingUI.CurrentState != null)
                pingUI.Draw(Main.spriteBatch, default);
            if (settingsUI.CurrentState != null)
                settingsUI.Draw(Main.spriteBatch, default);
            if (boardUI.CurrentState != null)
                boardUI.Draw(Main.spriteBatch, default);
            return true;
        }, InterfaceScaleType.UI);
        layers.Add(layer);
    }
}
