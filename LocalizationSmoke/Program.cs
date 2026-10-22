using Dalamud.Game;
using GlamourSaver;
using GlamourSaver.Services;

LocalizationService.ValidateOrThrow();
static void Eq(DisplayLanguage e, DisplayLanguage a, string n) { if (e != a) throw new Exception($"{n}: {a}"); }
Eq(DisplayLanguage.Japanese, LocalizationService.ResolveInitial(null, ClientLanguage.Japanese, "en"), "game ja");
Eq(DisplayLanguage.English, LocalizationService.ResolveInitial(null, ClientLanguage.English, "ja"), "game en");
Eq(DisplayLanguage.German, LocalizationService.ResolveInitial(null, null, "de-DE"), "de");
Eq(DisplayLanguage.French, LocalizationService.ResolveInitial(null, null, "fr_FR"), "fr");
Eq(DisplayLanguage.Korean, LocalizationService.ResolveInitial(null, null, "ko-KR"), "ko");
Eq(DisplayLanguage.SimplifiedChinese, LocalizationService.ResolveInitial(null, null, "zh-CN"), "hans");
Eq(DisplayLanguage.TraditionalChinese, LocalizationService.ResolveInitial(null, null, "zh-Hant"), "hant");
Eq(DisplayLanguage.English, LocalizationService.ResolveInitial(null, null, "zh"), "ambiguous zh");
foreach (var saved in LocalizationService.SelectionOrder) Eq(saved, LocalizationService.ResolveInitial(saved, ClientLanguage.Japanese, "de"), $"saved {saved}");
foreach (var value in new[] { DisplayLanguage.English, DisplayLanguage.Japanese }) { var c = new Configuration { Version = 3, DisplayLanguage = value }; c.Migrate(); Eq(value, c.DisplayLanguage!.Value, "legacy"); }
foreach (var value in new[] { (DisplayLanguage)0, (DisplayLanguage)99 }) { var c = new Configuration { Version = 3, DisplayLanguage = value }; c.Migrate(); if (c.DisplayLanguage != null) throw new Exception("invalid migration"); }
foreach (var language in LocalizationService.SelectionOrder) { var l = new LocalizationService(new Configuration { DisplayLanguage = language }); if (!l.Format("Posted {0}'s glamour.", "Test").Contains("Test")) throw new Exception($"format {language}"); }
var canonical = File.ReadAllText(Path.GetFullPath("../templates/Dalamud/StandardPluginTabs.cs")).Replace("\r\n", "\n");
var copied = File.ReadAllText("GlamourSaver/Shared/StandardPluginTabs.cs").Replace("\r\n", "\n");
if (canonical != copied) throw new Exception("StandardPluginTabs.cs differs from the canonical template.");
var uiSource = File.ReadAllText("GlamourSaver/Windows/PluginUi.cs");
if (uiSource.Split("DrawStandardTrailingTabs(", StringSplitOptions.None).Length - 1 != 1)
    throw new Exception("DrawStandardTrailingTabs must be called exactly once.");
if (uiSource.Contains("ImGui.BeginTabItem(", StringComparison.Ordinal))
    throw new Exception("PluginUi contains a tab outside the canonical trailing group.");
var helperSettings = canonical.IndexOf("settingsLabel", StringComparison.Ordinal);
var helperLanguage = canonical.IndexOf("languageSettingsLabel", helperSettings + 1, StringComparison.Ordinal);
var helperSupport = canonical.IndexOf("supportLabel", helperLanguage + 1, StringComparison.Ordinal);
if (!(helperSettings >= 0 && helperSettings < helperLanguage && helperLanguage < helperSupport))
    throw new Exception("Canonical trailing tab order is invalid.");
var fixture = Path.GetFullPath(Path.Combine("LocalizationSmoke", ".scratch-" + Guid.NewGuid().ToString("N")));
Directory.CreateDirectory(fixture);
try
{
    var path = Path.Combine(fixture, "config.json");
    File.WriteAllText(path, """{"$type":"GlamourSaver.Configuration, GlamourSaver","Version":3,"DisplayLanguage":2,"DiscordWebhookUrl":"synthetic-not-a-webhook","IncludeSlotNames":false,"ButtonOffsetX":51,"ButtonOffsetY":-12}""");
    using (var store = new ConfigurationStore<Configuration>(path))
    {
        var c = store.Value;
        if (c.DisplayLanguage != DisplayLanguage.Japanese || c.ButtonOffsetX != 51 || c.ButtonOffsetY != -12 || c.IncludeSlotNames || c.DiscordWebhookUrl != "synthetic-not-a-webhook") throw new Exception("Legacy config fidelity");
        if (c.Migrate()) store.MarkChanged();
        store.Dispose();
        if (store.Saves != 1) throw new Exception("Migration flush");
    }
    using var reopened = new ConfigurationStore<Configuration>(path);
    if (reopened.Value.Version != 4 || reopened.Value.DisplayLanguage != DisplayLanguage.Japanese) throw new Exception("Saved migration reload");
    reopened.Flush();
    if (reopened.Saves != 0) throw new Exception("No write on valid restart");
}
finally { Directory.Delete(fixture, true); }
if (!File.ReadAllBytes("../templates/Dalamud/StandardPluginTabs.cs").SequenceEqual(File.ReadAllBytes("GlamourSaver/Shared/StandardPluginTabs.cs"))) throw new Exception("Canonical bytes differ");
var png = await ScreenCaptureService.EncodePngAsync(new byte[16], new GlamourSaver.Models.ScreenRegion(0, 0, 2, 2), CancellationToken.None);
if (!png.Take(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) throw new Exception("PNG encode");
try
{
    await ScreenCaptureService.EncodePngAsync(new byte[16], new GlamourSaver.Models.ScreenRegion(0, 0, 2, 2), new CancellationToken(true));
    throw new Exception("PNG cancellation ignored");
}
catch (OperationCanceledException) { }
Console.WriteLine("Localization, migration, canonical source bytes, tab-order and legacy persistence smoke tests passed.");
