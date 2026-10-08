using Dalamud.Configuration;
using GlamourSaver.Services;

namespace GlamourSaver;

public sealed class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 4;
    public DisplayLanguage? DisplayLanguage { get; set; }
    public string DiscordWebhookUrl { get; set; } = string.Empty;
    public bool IncludeSlotNames { get; set; } = true;
    public int CapturePadding { get; set; } = 0;
    public int ButtonOffsetX { get; set; } = 0;
    public int ButtonOffsetY { get; set; } = 0;

    public bool Migrate()
    {
        var changed = false;
        if (!LocalizationService.IsValid(DisplayLanguage))
        {
            // Missing values and the short-lived value 0 from development builds are both uninitialized.
            DisplayLanguage = null;
            changed = true;
        }

        if (Version < 4)
        {
            Version = 4;
            changed = true;
        }

        return changed;
    }
}
