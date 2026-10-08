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
Console.WriteLine("Localization smoke tests passed.");
