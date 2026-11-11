namespace BetterReforgeMod.Common.UI;

public static class ReforgeLayout
{
    public static bool RightPageController
    {
        get
        {
            Reforge_Layout layout = Reforge_Layout.Instance;
            return !layout.IsNull && layout.RightPageController;
        }
    }
}
