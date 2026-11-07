using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;

namespace DevToolMod.TL;

public class ModInterface
{
    // Layouts are game objects kept between frames: the handles stop the game's collector
    // from taking them.
    static readonly List<GameHandle> LayoutHandles = new();

    public virtual void SetupContent() { }

    protected static Panel_Layout CreateLayout(float width = 0, float height = 0)
    {
        Panel_Layout layout = Panel_Layout.New();
        LayoutHandles.Add(new GameHandle(layout.Pointer));
        layout.Location = new Vector2(0, 0);
        layout.Size = new Vector2(width, height);
        layout.Anchor = LayoutCalculator.AnchorType.TopLeft;
        return layout;
    }

    protected static void Recalculate(Panel_Layout layout, Rectangle rect)
    {
        layout.Size = new Vector2(rect.Width, rect.Height);
        layout.Location = new Vector2(rect.X + rect.Width * 0.5f, rect.Y + rect.Height * 0.5f);
        GUIPanel.RegisterPickingRegion(layout);
        // RegisterPickingRegion is only the UI system's own hit test. To take the touch away
        // from the world (or tapping a mod button also digs tiles and uses the item), it also
        // has to go to the input system, as Bunny's own menus do. The input system may not be
        // ready yet (early in the main menu): the next frame registers again.
        GUIInputRegionManager input = GUIInputRegionManager.Instance;
        if (!input.IsNull) input.RegisterInputRegion(rect);
    }

    // Grabs the UI cursor drag on press, so a finger sliding on a mod panel does not turn the
    // game's view. Same source as above: Bunny's GUI calls it on touch down.
    public static void CaptureDrag(int dragFromAxis = -1)
    {
        XNAUIInputLayer input = XNAUIInputLayer.Instance;
        if (!input.IsNull) input.CaptureUICrusorDrag(dragFromAxis);
    }

    public virtual void Clear() { }

    public virtual void Update() { }

    public virtual void Draw() { }

    public static int Time;

    public static readonly List<ModInterface> Interfaces = new();

    public static void Register(ModInterface _interface) => Interfaces.Add(_interface);

    public static ModInterface GetByName(string name) => Interfaces.Find(i => i.GetType().Name == name)!;

    public static void SetupAllContent()
    {
        foreach (ModInterface _interface in Interfaces)
            _interface.SetupContent();
    }

    public static void UpdateAll()
    {
        Time++;
        ModButton.UpdateAll();
        foreach (ModInterface _interface in Interfaces)
            _interface.Update();
    }

    public static void ClearAll()
    {
        foreach (ModInterface _interface in Interfaces)
            _interface.Clear();
        Time = 0;
        ModButton.ClearAll();
    }
}
