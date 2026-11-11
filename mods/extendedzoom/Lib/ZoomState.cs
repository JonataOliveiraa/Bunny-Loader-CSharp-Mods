namespace ExtendedZoomMod.Lib;

// The zoom's shared state.
public static class ZoomState
{
    // Multiplier of the zoom-out limit (1 = normal).
    public static float Factor = 1.25f;
    // Smooth zoom is on.
    public static bool Smooth = true;
    // Pinching does not open the map (but a map opened with the button accepts pinching).
    public static bool NoPinchMap = true;
    public static bool SmoothMap = true;
    // The scale pinch or the slider asked for, still being animated (null = none).
    public static float? Target;
    // The useBump of the last request.
    public static bool Bump;
    // True while WE call ApplyPixelZoom, so the hook lets the call through.
    public static bool Applying;
    public static long Last;

    // The world camera's current ZoomScale, or 0 if there is no camera yet.
    public static float CurrentZoomScale()
    {
        XNAWorldCameraSettings camera = XNAUnityRunner._instance.IsNull ? default : XNAUnityRunner._instance.WorldCameraSettings;
        if (camera.IsNull)
            return 0;
        return camera.ZoomScale;
    }

    // Calls the game's ApplyPixelZoom. The hook lets it through because Applying is on.
    public static void Apply(float scale, bool bump)
    {
        GUIInstance gui = GUIInstance.Active;
        if (gui.IsNull || gui.GUIZoom.IsNull)
            return;
        Applying = true;
        try
        {
            gui.GUIZoom.ApplyPixelZoom(scale, bump);
        }
        finally
        {
            Applying = false;
        }
    }
}
