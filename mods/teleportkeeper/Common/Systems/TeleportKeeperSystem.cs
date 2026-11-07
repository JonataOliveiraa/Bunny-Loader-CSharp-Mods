using System.Collections.Generic;
using Microsoft.Xna.Framework;
using TeleportKeeperMod.Common.UI;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.UI;

namespace TeleportKeeperMod.Common.Systems;

public class TeleportKeeperSystem : ModSystem
{
    enum Pending { None, Create, List }

    public static TeleportKeeperSystem Instance = null!;

    public UserInterface? ui;
    public EditorUI? editor;
    public ListUI? list;

    GameInterfaceLayer? layer;
    Pending pending;
    bool waiting;

    static bool InWorld() => !Main.gameMenu && !Util.Local.IsNull;

    public override void Load()
    {
        Instance = this;
        ModTouchButton create = TouchButtonLoader.RegisterButton(Mod, "CreateTeleport");
        create.Icon = "Assets/TouchButtons/CreateTeleport";
        create.Enabled = InWorld;
        create.OnPress = _ => pending = Pending.Create;
        ModTouchButton teleports = TouchButtonLoader.RegisterButton(Mod, "TeleportList");
        teleports.Icon = "Assets/TouchButtons/TeleportList";
        teleports.Enabled = InWorld;
        teleports.OnPress = _ => pending = Pending.List;
    }

    public override void OnModLoad()
    {
        if (!Main.dedServ)
            Shield.Install();
    }

    public override void PostSetupContent()
    {
        if (Main.dedServ)
            return;
        ui = new UserInterface();
        editor = new EditorUI(this);
        list = new ListUI(this);
    }

    public bool Open => ui?.CurrentState != null;

    public void OpenCreate(bool fromList = false)
    {
        editor!.Begin(null, Icons.BiomeIcon(Util.Local), fromList ? list : null);
        Show(editor);
    }

    public void OpenEdit(TeleportPoint point)
    {
        editor!.Begin(point, point.icon, list);
        Show(editor);
    }

    public void OpenList(bool reset = true)
    {
        if (reset || list!.list == null)
            list!.Begin();
        Show(list!);
    }

    void Show(UIState state)
    {
        Screen.CloseInventory();
        waiting = true;
        ui!.SetState(state);
    }

    public void Close()
    {
        if (ui == null)
            return;
        editor?.nameBox?.Blur();
        list?.search?.Blur();
        ui.SetState(null);
    }

    public override void UpdateUI(GameTime gameTime)
    {
        if (ui == null)
            return;
        Pending now = pending;
        pending = Pending.None;
        if (now == Pending.Create)
        {
            if (ui.CurrentState == editor)
                Close();
            else
                OpenCreate();
        }
        else if (now == Pending.List)
        {
            if (ui.CurrentState == list)
                Close();
            else
                OpenList();
        }
        if (Open)
        {
            bool busy = Screen.GameUIBusy();
            if (busy && !waiting)
                Close();
            else if (!busy)
                waiting = false;
        }
        if (ui.CurrentState != null)
            ui.Update(gameTime);
        if (ui.CurrentState is TeleportState { panel: not null } state)
            Shield.Set(state.panel.GetDimensions());
        else
            Shield.Clear();
    }

    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
    {
        if (ui == null)
            return;
        layer ??= new LegacyGameInterfaceLayer("TeleportKeeper: Teleports", () =>
        {
            if (ui.CurrentState != null)
                Screen.Ordered(() => ui.Draw(Main.spriteBatch, default));
            return true;
        }, InterfaceScaleType.UI);
        int index = layers.FindIndex(static l => l.Name == "Vanilla: Mouse Text");
        layers.Insert(index == -1 ? layers.Count : index, layer);
    }

    public override void ClearWorld() => Teleports.Reset();

    public override void SaveWorldData(TagCompound tag) => Util.Safe("save", () => Teleports.SaveTo(tag));

    public override void LoadWorldData(TagCompound tag) => Util.Safe("load", () => Teleports.LoadFrom(tag));

    public override void OnWorldUnload()
    {
        Close();
        Teleports.Reset();
    }
}
