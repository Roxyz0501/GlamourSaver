using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using GlamourSaver.Services;
using GlamourSaver.Windows;

namespace GlamourSaver;

public sealed class Plugin : IDalamudPlugin
{
    private const string Command = "/glamoursaver";
    private readonly IDalamudPluginInterface pluginInterface;
    private readonly ICommandManager commandManager;
    private readonly PluginUi ui;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IDataManager dataManager,
        IClientState clientState,
        IGameGui gameGui,
        IChatGui chatGui,
        IPluginLog log)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;

        var configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        var configurationChanged = configuration.Migrate();
        if (!LocalizationService.ResolutionRulesAreValid)
            throw new InvalidOperationException("Localization resolution rules failed validation.");
        var initialLanguage = LocalizationService.ResolveInitial(configuration.DisplayLanguage, clientState.ClientLanguage);
        if (configuration.DisplayLanguage != initialLanguage)
        {
            configuration.DisplayLanguage = initialLanguage;
            configurationChanged = true;
        }
        if (configurationChanged)
            pluginInterface.SavePluginConfig(configuration);
        var localization = new LocalizationService(configuration);
        var reader = new CoordinateReader(dataManager, log, localization);
        var capture = new ScreenCaptureService(localization);
        var discord = new DiscordWebhookService(configuration, localization);
        ui = new PluginUi(configuration, gameGui, chatGui, log, reader, capture, discord, localization,
            () => pluginInterface.SavePluginConfig(configuration));

        commandManager.AddHandler(Command, new CommandInfo((_, _) => ui.OpenSettings())
        {
            HelpMessage = localization.Text(
                "Open the Glamour Saver Discord Webhook settings.",
                "Glamour SaverのDiscord Webhook設定を開きます。"),
        });
        pluginInterface.UiBuilder.Draw += ui.Draw;
        pluginInterface.UiBuilder.OpenConfigUi += ui.OpenSettings;
        pluginInterface.UiBuilder.OpenMainUi += ui.OpenSettings;
    }

    public void Dispose()
    {
        pluginInterface.UiBuilder.Draw -= ui.Draw;
        pluginInterface.UiBuilder.OpenConfigUi -= ui.OpenSettings;
        pluginInterface.UiBuilder.OpenMainUi -= ui.OpenSettings;
        commandManager.RemoveHandler(Command);
        ui.Dispose();
    }
}
