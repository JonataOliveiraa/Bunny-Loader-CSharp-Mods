using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Terraria.Localization;

namespace MenuMediaPort.Lib;

// Texts the player sees (the /menumedia command and the status line). English is the default and
// the fallback; Vietnamese is picked when config.json says "language": "vi" or, with "auto", when
// the game's language is Vietnamese. Named MediaText because Terraria.Lang is in scope everywhere.
public static class MediaText
{
    static readonly Dictionary<string, string> En = new()
    {
        ["noThemes"] = "No .gif/.png/.jpg in Assets/MenuThemes: no themes.",
        ["theme"] = "theme {0} ({1}): {2}{3}, music: {4}",
        ["gameMusic"] = "game music",
        ["video"] = "video {0} ignored: convert it to GIF + MP3 first (convert-video.sh / convert-video.bat)",
        ["iconInfo"] = ", icon: {0}",
        ["pending"] = "waiting",
        ["decoding"] = "preparing",
        ["loading"] = "loading",
        ["ready"] = "ready",
        ["failed"] = "failed",
        ["osPrep"] = "Menu Media: preparing \"{0}\" {1}%",
        ["osLoad"] = "Menu Media: loading \"{0}\" {1}%",
        ["osFail"] = "Menu Media: \"{0}\" failed: {1}",
    };

    static readonly Dictionary<string, string> Vi = new()
    {
        ["noThemes"] = "Không có .gif/.png/.jpg nào trong Assets/MenuThemes: chưa có theme.",
        ["theme"] = "theme {0} ({1}): {2}{3}, nhạc: {4}",
        ["gameMusic"] = "nhạc của game",
        ["video"] = "video {0} bị bỏ qua: hãy đổi sang GIF + MP3 trước (convert-video.sh / convert-video.bat)",
        ["iconInfo"] = ", icon: {0}",
        ["pending"] = "đang chờ",
        ["decoding"] = "đang chuẩn bị",
        ["loading"] = "đang tải",
        ["ready"] = "sẵn sàng",
        ["failed"] = "lỗi",
        ["osPrep"] = "Menu Media: đang chuẩn bị \"{0}\" {1}%",
        ["osLoad"] = "Menu Media: đang tải \"{0}\" {1}%",
        ["osFail"] = "Menu Media: \"{0}\" bị lỗi: {1}",
    };

    static readonly Regex Placeholder = new(@"\{(\d+)\}");

    static Dictionary<string, string> current = En;

    // Reads the game's language, so it waits for PostSetupContent.
    public static void SetLanguage(string setting)
    {
        string code = (string.IsNullOrEmpty(setting) ? "auto" : setting).ToLowerInvariant();
        if (code == "auto")
        {
            try
            {
                code = (Language.ActiveCulture.Name ?? "en").ToLowerInvariant();
            }
            catch (Exception)
            {
                code = "en";
            }
        }
        current = code.StartsWith("vi", StringComparison.Ordinal) ? Vi : En;
    }

    public static string T(string key, params object[] args)
    {
        if (!current.TryGetValue(key, out string? text) && !En.TryGetValue(key, out text))
            text = key;
        return Placeholder.Replace(text, m =>
        {
            int index = int.Parse(m.Groups[1].Value);
            return index < args.Length ? args[index].ToString() ?? "" : m.Value;
        });
    }
}
