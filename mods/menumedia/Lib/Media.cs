using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using MenuMediaPort.Common.Configs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using static MenuMediaPort.Lib.MediaText;

namespace MenuMediaPort.Lib;

// Each GIF or image in Assets/MenuThemes/ becomes a menu theme (ModMenu). The audio file of the same
// name, if any, is the theme's music.
//
//   Assets/MenuThemes/beach.gif + beach.mp3   -> theme "beach", with music
//   Assets/MenuThemes/night.png               -> theme "night", the game's music
//
// The GIF is decoded here, one step per game frame (Unity only opens PNG/JPG), and each frame becomes
// a PNG in the mod's cache/ folder. Only the selected theme is prepared.
public sealed class MediaSettings
{
    public string Language = "auto";      // auto | en | vi (language of the /menumedia command; English by default)
    public string Fit = "cover";          // cover | contain | stretch
    public double PadLeft = 0;            // extra drawing area beyond the screen edges, as a fraction of the screen size
    public double PadRight = 0.12;        // (the picture is fitted to the enlarged area, so it covers the screen edge to edge)
    public double PadTop = 0;
    public double PadBottom = 0;
    public double AreaScale = 1;          // multiplies the screen size used for drawing (advanced)
    public int MaxSide = 480;             // longest side of a frame (px)
    public double MaxFps = 15;            // playback fps cap: GIFs faster than this lose frames (duration is kept)
    public int MaxFrames = 120;           // frames kept per theme (the rest are merged; duration is kept)
    public double MaxPixels = 16000000;   // total pixels of a theme's frames (~64 MB)
    public int PreloadPerFrame = 1;       // frames sent to the GPU per work slot
    public bool ShowStatus = true;        // show a small status line on the menu while a GIF is preparing or failed
    public double BudgetMs = 8;           // target ms of work per game frame while preparing (higher = faster but laggier)
}

// One theme: a background (and its music) and the state of its preparation.
public sealed class Theme
{
    public readonly string Name;     // the background's file, e.g. beach.gif
    public readonly string Base;
    readonly string kind;            // "gif" | "image"
    public readonly string Audio;    // the audio's path in the mod, or ""
    public readonly string Icon;     // the path of <name>_Icon.png/.jpg (replaces the game's logo), or ""

    Asset<Texture2D>? iconAsset;
    bool iconTried;

    public int Slot;                 // the game's music slot (0 = none)
    public ModMenu? Menu;
    public string Status = "pending";   // pending | decoding | loading | ready | failed
    public string Message = "";

    readonly List<string> paths = new();    // asset paths of the frames, without the extension
    readonly List<int> delays = new();
    readonly List<Asset<Texture2D>> frames = new();
    int cur;
    GifReader? reader;
    string cacheFolder = "";         // the cache folder's path in the mod
    public int Index;
    double minInterval;
    double since;
    IEnumerator<byte[]?>? job;
    int wait;
    double frameStart;
    bool logged;
    bool hasSize;
    int textureWidth, textureHeight;

    public Theme(string name, string kind, string audio, string icon)
    {
        Name = name;
        Base = MenuMedia.BaseOf(name);
        this.kind = kind;
        Audio = audio;
        Icon = icon;
    }

    public bool IconLoaded => iconAsset != null;
    public int FramesLoaded => frames.Count;
    public int PathCount => paths.Count;
    public GifReader? Reader => reader;

    static double Now() => Stopwatch.GetTimestamp() * 1000.0 / Stopwatch.Frequency;

    // The icon as a game texture (loads the first time; null without an icon or if it failed).
    public Asset<Texture2D>? Logo()
    {
        if (Icon.Length == 0)
            return null;
        if (!iconTried)
        {
            iconTried = true;
            Mod mod = MenuMedia.Mod;
            try
            {
                Asset<Texture2D> asset = mod.Assets.Request<Texture2D>(MenuMedia.TexturePath(Icon));
                asset.Wait();
                iconAsset = asset;
                mod.Logger.Info("Menu Media [" + Base + "]: icon " + Path.GetFileName(Icon) + " loaded");
            }
            catch (Exception e)
            {
                mod.Logger.Error("Menu Media [" + Base + "]: icon failed: " + e.Message);
            }
        }
        return iconAsset;
    }

    static string FrameName(int n) => n.ToString("D4", CultureInfo.InvariantCulture);

    public void Step()
    {
        bool working = Status is "decoding" or "loading";
        if (working && wait > 0)
        {
            // The preparation is spread out: this time the game stays free.
            wait--;
            return;
        }
        double t0 = Now();
        try
        {
            switch (Status)
            {
                case "pending": Begin(); break;
                case "decoding": Decode(); break;
                case "loading": Load(); break;
                case "ready": Draw(); break;
            }
        }
        catch (Exception e)
        {
            Status = "failed";
            Message = e.Message;
            MenuMedia.Mod.Logger.Error("Menu Media [" + Base + "]: " + Message);
            return;
        }
        if (Status is "decoding" or "loading")
        {
            double spent = Now() - t0;
            wait = Math.Max(0, (int)Math.Ceiling(spent / MenuMedia.Cfg.BudgetMs) - 1);
        }
    }

    void Begin()
    {
        Mod mod = MenuMedia.Mod;
        string file = MenuMedia.Folder + "/" + Name;
        if (kind == "image")
        {
            paths.Clear();
            paths.Add(MenuMedia.TexturePath(file));
            delays.Clear();
            delays.Add(1000);
            Status = "loading";
            return;
        }

        byte[] bytes = mod.GetFileBytes(file);
        if (bytes.Length == 0)
            throw new IOException("could not read " + Name);

        MediaSettings cfg = MenuMedia.Cfg;
        string prefix = MenuMedia.Safe(Name) + "_";
        string key = prefix + bytes.Length + "_" + cfg.MaxSide + "_" + cfg.MaxFrames + "_" + MenuMedia.Number(cfg.MaxFps) + "_" + MenuMedia.Number(cfg.MaxPixels);
        string folder = MenuMedia.Cache + "/" + key;
        string absolute = Path.Combine(mod.Folder, folder);

        string cacheRoot = Path.Combine(mod.Folder, MenuMedia.Cache);
        Directory.CreateDirectory(cacheRoot);
        foreach (string sub in Directory.GetDirectories(cacheRoot))
        {
            string name = Path.GetFileName(sub);
            if (name.StartsWith(prefix, StringComparison.Ordinal) && name != key)
                Directory.Delete(sub, true);
        }

        if (ReadMeta(absolute, out List<int> cached))
        {
            delays.Clear();
            delays.AddRange(cached);
            paths.Clear();
            for (int i = 0; i < delays.Count; i++)
                paths.Add(folder + "/" + FrameName(i));
            Status = "loading";
            mod.Logger.Info("Menu Media [" + Base + "]: cached (" + delays.Count + " frames)");
            return;
        }

        // What an interrupted preparation left behind.
        if (Directory.Exists(absolute))
            Directory.Delete(absolute, true);
        Directory.CreateDirectory(absolute);
        var gif = new GifReader(bytes, cfg.MaxSide);
        int limit = (int)Math.Max(1, Math.Min(cfg.MaxFrames, Math.Floor(cfg.MaxPixels / (gif.OutWidth * gif.OutHeight))));
        reader = gif;
        cacheFolder = folder;
        Index = 0;
        minInterval = Math.Max(1000 / cfg.MaxFps, gif.Duration / (double)limit);
        since = 0;
        job = null;
        paths.Clear();
        delays.Clear();
        Status = "decoding";
        mod.Logger.Info("Menu Media [" + Base + "]: preparing " + gif.Width + "x" + gif.Height + " -> "
            + gif.OutWidth + "x" + gif.OutHeight + ", " + gif.Count + " frames, " + gif.Duration + " ms, min " + Math.Round(minInterval) + " ms per kept frame");
    }

    // The frame list of a finished preparation, in meta.txt.
    static bool ReadMeta(string absolute, out List<int> cached)
    {
        cached = new List<int>();
        string metaFile = Path.Combine(absolute, "meta.txt");
        try
        {
            if (!File.Exists(metaFile))
                return false;
            string[] lines = File.ReadAllLines(metaFile);
            if (lines.Length < 2 || lines[0] != "done")
                return false;
            foreach (string part in lines[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
                cached.Add(int.Parse(part, CultureInfo.InvariantCulture));
        }
        catch (Exception)
        {
            return false;
        }
        return cached.Count > 0 && File.Exists(Path.Combine(absolute, FrameName(cached.Count - 1) + ".png"));
    }

    // Per kept frame: decode the GIF (1 step) and build and write the PNG (3 steps).
    // Step() leaves a few game frames free between the steps.
    void Decode()
    {
        GifReader r = reader!;
        Mod mod = MenuMedia.Mod;
        string absolute = Path.Combine(mod.Folder, cacheFolder);

        if (job != null)
        {
            // The kept frame's PNG, one step at a time.
            if (job.MoveNext() && job.Current is byte[] png)
            {
                string name = FrameName(paths.Count);
                File.WriteAllBytes(Path.Combine(absolute, name + ".png"), png);
                paths.Add(cacheFolder + "/" + name);
                job.Dispose();
                job = null;
            }
            return;
        }

        if (!r.Next(out int delay))
        {
            if (paths.Count == 0)
                throw new InvalidOperationException("the GIF has no frames");
            var meta = new StringBuilder("done\n");
            meta.Append(string.Join(",", delays)).Append('\n').Append(r.OutWidth).Append(' ').Append(r.OutHeight).Append('\n');
            File.WriteAllText(Path.Combine(absolute, "meta.txt"), meta.ToString());
            reader = null;
            Status = "loading";
            mod.Logger.Info("Menu Media [" + Base + "]: " + paths.Count + " frames ready");
            return;
        }

        int i = Index++;
        // The frame stays when the minimum interval has passed; the others only add their time to the previous one.
        if (delays.Count == 0 || since + delay / 2.0 >= minInterval)
        {
            // The canvas only changes on the next Next(), after the PNG is written.
            job = Png.EncodeSteps(r.Canvas, r.Width, r.Height, r.OutWidth, r.OutHeight);
            delays.Add(delay);
            since = delay;
        }
        else
        {
            delays[^1] += delay;
            since += delay;
        }
        if ((i + 1) % 20 == 0)
            mod.Logger.Info("Menu Media [" + Base + "]: preparing " + (i + 1) + "/" + r.Count);
    }

    void Load()
    {
        Mod mod = MenuMedia.Mod;
        for (int n = 0; n < MenuMedia.Cfg.PreloadPerFrame && frames.Count < paths.Count; n++)
        {
            Asset<Texture2D> asset = mod.Assets.Request<Texture2D>(paths[frames.Count]);
            asset.Wait();
            frames.Add(asset);
        }
        if (frames.Count >= paths.Count)
        {
            Status = "ready";
            cur = 0;
            frameStart = 0;
            mod.Logger.Info("Menu Media [" + Base + "]: background live (" + frames.Count + " frames)");
        }
    }

    double Duration(int index, int fps) => fps > 0 ? 1000.0 / fps : delays[index];

    void Draw()
    {
        MediaSettings cfg = MenuMedia.Cfg;
        MenuMediaConfig live = MenuMediaConfig.Instance;
        int n = frames.Count;

        // Size, position and fps come from the Mod Configuration screen, read every frame.
        int fps = live.Fps;   // 0 = the GIF's own timing

        if (n > 1)
        {
            double now = Now();
            if (frameStart == 0 || now - frameStart > 2000)
                frameStart = now;
            for (int g = 0; g < 64 && now - frameStart >= Duration(cur, fps); g++)
            {
                frameStart += Duration(cur, fps);
                cur = (cur + 1) % n;
            }
        }

        Texture2D tex = frames[cur].Value;
        if (!hasSize)
        {
            hasSize = true;
            textureWidth = tex.Width;
            textureHeight = tex.Height;
        }
        double tw = textureWidth, th = textureHeight;
        double sw = Main.screenWidth * cfg.AreaScale, sh = Main.screenHeight * cfg.AreaScale;
        if (!(tw > 0 && th > 0 && sw > 0 && sh > 0))
            return;

        int size = live.Size, ox = live.OffsetX, oy = live.OffsetY;

        if (!logged)
        {
            logged = true;
            MenuMedia.Mod.Logger.Info("Menu Media [" + Base + "]: screen " + sw + "x" + sh + ", image " + tw + "x" + th + ", fit " + cfg.Fit
                + ", size " + size + "%, offset " + ox + "," + oy + ", fps " + (fps > 0 ? fps.ToString() : "original"));
        }

        // The drawing area is the screen plus the margins (padRight covers the strip the game left out).
        double ax = -cfg.PadLeft * sw, ay = -cfg.PadTop * sh;
        double aw = sw * (1 + cfg.PadLeft + cfg.PadRight), ah = sh * (1 + cfg.PadTop + cfg.PadBottom);

        double w = aw, h = ah;
        if (cfg.Fit != "stretch")
        {
            double k = cfg.Fit == "contain" ? Math.Min(aw / tw, ah / th) : Math.Max(aw / tw, ah / th);
            w = tw * k;
            h = th * k;
        }
        w *= size / 100.0;
        h *= size / 100.0;
        double x = ax + (aw - w) / 2 + ox;
        double y = ay + (ah - h) / 2 + oy;

        SpriteBatch sb = Main.spriteBatch;
        if (cfg.Fit == "contain")
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)Math.Floor(ax), (int)Math.Floor(ay), (int)Math.Ceiling(aw), (int)Math.Ceiling(ah)), new Color(0, 0, 0, 255));
        sb.Draw(tex, new Rectangle((int)Math.Floor(x + 0.5), (int)Math.Floor(y + 0.5), (int)Math.Ceiling(w), (int)Math.Ceiling(h)), Color.White);
    }
}

// One menu theme per background. The name identifies the theme in the player's saved choice, so it
// comes from the file's name.
[Autoload(false)]
public sealed class ThemeMenu : ModMenu
{
    readonly Theme theme;
    readonly string name;

    public ThemeMenu(Theme theme)
    {
        this.theme = theme;
        name = "MenuMedia_" + MenuMedia.Safe(theme.Base);
    }

    public override string Name => name;

    public override string DisplayName => theme.Base;

    public override int Music => theme.Slot > 0 ? theme.Slot : -1;

    // <name>_Icon in place of the game's logo.
    public override Asset<Texture2D>? Logo => theme.Logo();

    public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
    {
        if (theme.IconLoaded)
        {
            int k = MenuMediaConfig.Instance.IconSize;
            if (k > 0)
                logoScale = logoScale * k / 100;
        }
        return true;
    }
}

public static class MenuMedia
{
    public const string Folder = "Assets/MenuThemes";

    // Inside the mod's folder, because the loader only opens textures from there. Importing the mod again empties it.
    public const string Cache = "cache";

    static readonly string[] GifExtensions = { ".gif" };
    static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };
    static readonly string[] AudioExtensions = { ".mp3", ".ogg", ".wav", ".m4a", ".aac", ".flac" };
    static readonly string[] LoaderAudioExtensions = { ".mp3", ".ogg", ".wav" };
    static readonly string[] VideoExtensions = { ".mp4", ".mkv", ".webm", ".mov", ".avi", ".3gp" };

    public static Mod Mod = null!;
    public static readonly MediaSettings Cfg = new();
    public static readonly List<Theme> Themes = new();
    static readonly List<string> videos = new();
    static string lastSelected = "";
    static bool textBroken;

    static string Lower(string s) => s.ToLowerInvariant();

    static string ExtOf(string n)
    {
        int i = n.LastIndexOf('.');
        return i < 0 ? "" : Lower(n[i..]);
    }

    public static string BaseOf(string n)
    {
        int i = n.LastIndexOf('.');
        return i < 0 ? n : n[..i];
    }

    static string Hash(string s)
    {
        uint h = 5381;
        foreach (char c in s)
            h = (h * 33) ^ c;
        return h.ToString("x", CultureInfo.InvariantCulture);
    }

    public static string Safe(string s)
    {
        var clean = new StringBuilder(s.Length);
        foreach (char c in s)
            clean.Append(char.IsAsciiLetterOrDigit(c) || c == '_' || c == '-' ? c : '_');
        string text = clean.ToString();
        return (text.Length > 32 ? text[..32] : text) + "-" + Hash(s);
    }

    public static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);

    // The loader's asset path of an image in the mod, without the extension. It only asks for
    // lowercase .png, but Unity reads the file's content: anything else is copied to a .png name.
    public static string TexturePath(string file)
    {
        if (file.EndsWith(".png", StringComparison.Ordinal))
            return file[..^4];
        string source = Path.Combine(Mod.Folder, file);
        string name = "images/" + Safe(Path.GetFileName(file)) + "_" + new FileInfo(source).Length;
        CopyOnce(source, Path.Combine(Mod.Folder, Cache, name + ".png"));
        return Cache + "/" + name;
    }

    static void CopyOnce(string source, string target)
    {
        if (File.Exists(target))
            return;
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target);
    }

    // In the mod's Load: reads the package's folder and registers one theme per background.
    public static void Init(Mod mod)
    {
        Mod = mod;
        LoadConfig();

        List<string> files = mod.GetFileNames()
            .Where(f => f.StartsWith(Folder + "/", StringComparison.Ordinal) && f.IndexOf('/', Folder.Length + 1) < 0)
            .Select(f => f[(Folder.Length + 1)..])
            .OrderBy(f => f, StringComparer.Ordinal)
            .ToList();
        var byLower = new Dictionary<string, string>();
        foreach (string file in files)
            byLower[Lower(file)] = file;
        var seen = new HashSet<string>();

        foreach (string name in files)
        {
            string ext = ExtOf(name);
            string kind = GifExtensions.Contains(ext) ? "gif" : ImageExtensions.Contains(ext) ? "image" : "";
            string lowerBase = Lower(BaseOf(name));
            if (VideoExtensions.Contains(ext))
                continue;
            if (lowerBase.EndsWith("_icon", StringComparison.Ordinal))   // the icon of another theme
                continue;
            if (kind.Length == 0 || !seen.Add(lowerBase))
                continue;

            string audio = "";
            foreach (string a in AudioExtensions)
            {
                if (byLower.TryGetValue(lowerBase + a, out string? hit))
                {
                    audio = Folder + "/" + hit;
                    break;
                }
            }
            string icon = "";
            foreach (string e in ImageExtensions)
            {
                if (byLower.TryGetValue(lowerBase + "_icon" + e, out string? hit))
                {
                    icon = Folder + "/" + hit;
                    break;
                }
            }
            Register(new Theme(name, kind, audio, icon));
        }
        foreach (string f in files)
        {
            if (VideoExtensions.Contains(ExtOf(f)) && !Themes.Any(t => Lower(t.Base) == Lower(BaseOf(f))))
                videos.Add(f);
        }

        mod.Logger.Info("Menu Media: folder " + Folder);
        foreach (string line in Describe())
            mod.Logger.Info("Menu Media: " + line);
    }

    static void LoadConfig()
    {
        string file = Folder + "/config.json";
        Dictionary<string, object> user;
        try
        {
            if (!Mod.FileExists(file))
                return;
            user = FlatJson.Parse(Encoding.UTF8.GetString(Mod.GetFileBytes(file)));
        }
        catch (Exception e)
        {
            Mod.Logger.Error("Menu Media: config.json is broken (" + e.Message + "); using defaults");
            return;
        }

        MediaSettings cfg = Cfg;
        void Text(string key, ref string target)
        {
            if (user.TryGetValue(key, out object? value) && value is string text)
                target = text;
        }
        void Real(string key, ref double target)
        {
            if (user.TryGetValue(key, out object? value) && value is double number && !double.IsNaN(number))
                target = number;
        }
        // A number outside int's range clamps below, like the original's clamp after truncating.
        void Whole(string key, ref int target)
        {
            if (user.TryGetValue(key, out object? value) && value is double number && !double.IsNaN(number))
                target = (int)Math.Clamp(Math.Truncate(number), int.MinValue, int.MaxValue);
        }

        Text("language", ref cfg.Language);
        Text("fit", ref cfg.Fit);
        Real("padLeft", ref cfg.PadLeft);
        Real("padRight", ref cfg.PadRight);
        Real("padTop", ref cfg.PadTop);
        Real("padBottom", ref cfg.PadBottom);
        Real("areaScale", ref cfg.AreaScale);
        Whole("maxSide", ref cfg.MaxSide);
        Real("maxFps", ref cfg.MaxFps);
        Whole("maxFrames", ref cfg.MaxFrames);
        Real("maxPixels", ref cfg.MaxPixels);
        Whole("preloadPerFrame", ref cfg.PreloadPerFrame);
        Real("budgetMs", ref cfg.BudgetMs);
        if (user.TryGetValue("showStatus", out object? status) && status is bool show)
            cfg.ShowStatus = show;

        if (cfg.Fit is not ("cover" or "contain" or "stretch"))
            cfg.Fit = "cover";
        cfg.MaxSide = Math.Clamp(cfg.MaxSide, 32, 1280);
        cfg.MaxFrames = Math.Clamp(cfg.MaxFrames, 1, 600);
        cfg.PreloadPerFrame = Math.Clamp(cfg.PreloadPerFrame, 1, 16);
        if (!(cfg.AreaScale > 0))
            cfg.AreaScale = 1;
        cfg.PadLeft = Math.Clamp(cfg.PadLeft, 0, 1);
        cfg.PadRight = Math.Clamp(cfg.PadRight, 0, 1);
        cfg.PadTop = Math.Clamp(cfg.PadTop, 0, 1);
        cfg.PadBottom = Math.Clamp(cfg.PadBottom, 0, 1);
        cfg.MaxFps = Math.Clamp(cfg.MaxFps, 1, 60);
        cfg.BudgetMs = Math.Clamp(cfg.BudgetMs, 2, 50);
        if (cfg.Language is not ("auto" or "en" or "vi"))
            cfg.Language = "auto";
    }

    static void Register(Theme theme)
    {
        var menu = new ThemeMenu(theme);
        Mod.AddContent(menu);
        theme.Menu = menu;
        Themes.Add(theme);
    }

    // With the game ready (PostSetupContent): each audio file becomes a music slot.
    public static void SetupMusic()
    {
        foreach (Theme t in Themes)
        {
            if (t.Audio.Length == 0)
                continue;
            t.Slot = MusicLoader.GetMusicSlot(Mod, MusicPath(t.Audio));
            if (t.Slot == 0)
                Mod.Logger.Error("Menu Media [" + t.Base + "]: Android rejected " + t.Audio);
        }
    }

    // The loader only asks for .mp3, .ogg and .wav, but Android reads the file's content:
    // anything else is copied to a .mp3 name.
    static string MusicPath(string file)
    {
        if (LoaderAudioExtensions.Contains(ExtOf(file)))
            return file;
        string source = Path.Combine(Mod.Folder, file);
        string name = Cache + "/audio/" + Safe(Path.GetFileName(file)) + "_" + new FileInfo(source).Length + ".mp3";
        CopyOnce(source, Path.Combine(Mod.Folder, name));
        return name;
    }

    public static List<string> Describe()
    {
        var output = new List<string>();
        if (Themes.Count == 0)
            output.Add(T("noThemes"));
        foreach (Theme t in Themes)
        {
            output.Add(T("theme", t.Base, t.Name, T(t.Status), t.Message.Length > 0 ? " - " + t.Message : "",
                t.Audio.Length > 0 ? Path.GetFileName(t.Audio) : T("gameMusic")) + (t.Icon.Length > 0 ? T("iconInfo", Path.GetFileName(t.Icon)) : ""));
        }
        foreach (string v in videos)
            output.Add(T("video", v));
        return output;
    }

    // Every frame of the menus (before the logo): only the selected theme works.
    public static void Frame()
    {
        if (Themes.Count == 0 || !Main.gameMenu)
            return;
        foreach (Theme t in Themes)
        {
            if (t.Menu == null || !t.Menu.IsSelected)
                continue;

            if (lastSelected != t.Base)
            {
                lastSelected = t.Base;
                Mod.Logger.Info("Menu Media: selected theme " + t.Base + " (" + t.Status + ")");
            }
            if (t.Status != "failed")
                t.Step();
            if (Cfg.ShowStatus && t.Status != "ready")
                Overlay(t);
            return;
        }
    }

    // A line of text in the corner, so the player sees what is happening.
    static void Overlay(Theme t)
    {
        if (textBroken)
            return;
        string text;
        if (t.Status == "failed")
            text = T("osFail", t.Base, t.Message);
        else if (t.Status == "loading")
            text = T("osLoad", t.Base, 100 * t.FramesLoaded / Math.Max(1, t.PathCount));
        else if (t.Status == "decoding" && t.Reader != null)
            text = T("osPrep", t.Base, 100 * t.Index / Math.Max(1, t.Reader.Count));
        else
            return;

        try
        {
            Main.spriteBatch.DrawString(FontAssets.MouseText.Value, text, new Vector2(20, 20), Color.White, 0f, Vector2.Zero, 0.9f, SpriteEffects.None, 0f);
        }
        catch (Exception e)
        {
            textBroken = true;
            Mod.Logger.Error("Menu Media: status line disabled (" + e.Message + ")");
        }
    }
}
