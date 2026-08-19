using Dalamud.Game;

namespace GlamourSaver.Services;

public enum DisplayLanguage
{
    English = 1,
    Japanese = 2,
}

public sealed class LocalizationService(Configuration configuration)
{
    public static bool ResolutionRulesAreValid
        => ResolveInitial(null, ClientLanguage.Japanese) == DisplayLanguage.Japanese
           && ResolveInitial(null, ClientLanguage.English) == DisplayLanguage.English
           && ResolveInitial(null, null) == DisplayLanguage.English
           && ResolveInitial(DisplayLanguage.English, ClientLanguage.Japanese) == DisplayLanguage.English
           && ResolveInitial(DisplayLanguage.Japanese, ClientLanguage.English) == DisplayLanguage.Japanese;

    public DisplayLanguage ResolvedLanguage
        => configuration.DisplayLanguage is DisplayLanguage.Japanese
            ? DisplayLanguage.Japanese
            : DisplayLanguage.English;

    public bool IsJapanese => ResolvedLanguage == DisplayLanguage.Japanese;

    public string Text(string english, string japanese)
        => IsJapanese ? japanese : english;

    public static DisplayLanguage ResolveInitial(DisplayLanguage? saved, ClientLanguage? clientLanguage)
        => saved is DisplayLanguage.English or DisplayLanguage.Japanese
            ? saved.Value
            : clientLanguage == ClientLanguage.Japanese
                ? DisplayLanguage.Japanese
                : DisplayLanguage.English;
}
