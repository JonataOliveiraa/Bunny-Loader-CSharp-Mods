using System.Collections.Generic;
using BunnyLoader.Interop;
using Microsoft.Xna.Framework;

namespace WikithisMod.Common.UI;

public abstract class ModInterface
{
    public static int Time;

    public static readonly List<ModInterface> Interfaces = new();

    // Keeps the layouts alive: the game's collector does not see C# fields.
    readonly List<GameHandle> layoutHandles = new();

    public virtual void SetupContent() { }

    public virtual void Clear() { }

    public virtual void Draw() { }

    protected Panel_Layout CreateLayout(int width = 0, int height = 0)
    {
        Panel_Layout layout = Panel_Layout.New();
        layoutHandles.Add(new GameHandle(layout.Pointer));
        layout.Location = new Vector2(0f, 0f);
        layout.Size = new Vector2(width, height);
        layout.Anchor = LayoutCalculator.AnchorType.TopLeft;
        return layout;
    }

    // Registers the panel so that a touch on it goes to the interface and not to the world.
    protected static void Recalculate(Panel_Layout layout, Rectangle rect)
    {
        layout.Size = new Vector2(rect.Width, rect.Height);
        layout.Location = new Vector2(rect.X + layout.Size.X * 0.5f, rect.Y + layout.Size.Y * 0.5f);
        GUIPanel.RegisterPickingRegion(layout);
    }

    public static void Register(ModInterface modInterface) => Interfaces.Add(modInterface);

    public static void SetupAll()
    {
        foreach (ModInterface modInterface in Interfaces)
            modInterface.SetupContent();
    }

    public static void ClearAll()
    {
        foreach (ModInterface modInterface in Interfaces)
            modInterface.Clear();
        Time = 0;
    }
}
