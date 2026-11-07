using System;
using MenuMediaPort.Lib;
using Terraria.ModLoader;

// Menu Media: each GIF or image in Assets/MenuThemes/ is a menu theme, with the audio file of the
// same name as its music. The theme is picked in Settings > Menu Theme.
public class MenuMediaMod : Mod
{
    public override void Load()
    {
        try
        {
            MenuMedia.Init(this);
        }
        catch (Exception e)
        {
            Logger.Error("Menu Media: " + e.Message);
        }
        if (MenuMedia.Themes.Count == 0)
            return;

        // The title logo is drawn after the game's background and before the buttons: drawing here,
        // before the original, the background covers the sky and the logo stays on top.
        // This must be added after the themes. They install the loader's own hook, which records every
        // draw inside it as a logo draw, and the hook added last runs first, outside that recording.
        On_GUILogo.Draw += (orig, self) =>
        {
            try
            {
                MenuMedia.Frame();
            }
            catch (Exception e)
            {
                Logger.Error("Menu Media: " + e);
            }
            orig(self);
        };
    }

    public override void PostSetupContent()
    {
        try
        {
            MediaText.SetLanguage(MenuMedia.Cfg.Language);
            MenuMedia.SetupMusic();
        }
        catch (Exception e)
        {
            Logger.Error("Menu Media (music): " + e);
        }
    }
}
