using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace DevToolMod;

// Texture table: the key is the file name without folder or extension ('TB_Button',
// 'CS_Icon_0'...). The interfaces under Content/UI fetch their textures by this short name.
public static class Textures
{
    const string Folder = "Textures/";
    const string Extension = ".png";

    static readonly Dictionary<string, Texture2D> Table = new();
    static bool done;

    public static void InitializeTextures()
    {
        if (done) return;
        done = true;
        foreach (string file in DevTool.Instance.GetFileNames())
        {
            if (!file.StartsWith(Folder, StringComparison.Ordinal) || !file.EndsWith(Extension, StringComparison.OrdinalIgnoreCase)) continue;
            string path = file.Substring(0, file.Length - Extension.Length);
            try
            {
                Table[path.Substring(path.LastIndexOf('/') + 1)] = ModContent.Request<Texture2D>("devtools/" + path).Value;
            }
            catch (Exception e)
            {
                DevTool.Instance.Logger.Error("DevTool: failed to load texture " + file + ": " + e);
            }
        }
    }

    // A missing name gives an empty handle, and the draw code skips it.
    public static Texture2D Get(string key)
    {
        if (!done) InitializeTextures();
        return Table.GetValueOrDefault(key);
    }
}
