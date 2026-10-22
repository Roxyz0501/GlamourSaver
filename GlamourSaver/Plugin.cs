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
    private readonly ConfigurationStore<Configuration> store;
    private readonly IFramework framework;
    private readonly IClientState clientState;
    private readonly IPluginLog log;
    private bool disposed;
    private readonly CancellationTokenRegistration shutdownRegistration;
    private readonly CancellationTokenRegistration unloadRegistration;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        ICommandManager commandManager,
        IDataManager dataManager,
        IClientState clientState,
        IGameGui gameGui,
        IChatGui chatGui,
        IPluginLog log,
        IFramework framework,
        IGameLifecycle lifecycle)
    {
        this.pluginInterface = pluginInterface;
        this.commandManager = commandManager;
        this.framework = framework;
        this.clientState = clientState;
        this.log = log;

        store = new ConfigurationStore<Configuration>(pluginInterface.ConfigFile.FullName,
            validate: config => config.Version is >= 0 and <= 4 && config.DiscordWebhookUrl is not null);
        var configuration = store.Value;
        var configurationChanged = configuration.Migrate();
        LocalizationService.ValidateOrThrow();
        var initialLanguage = LocalizationService.ResolveInitial(configuration.DisplayLanguage, clientState.ClientLanguage, pluginInterface.UiLanguage);
        if (configuration.DisplayLanguage != initialLanguage)
        {
            configuration.DisplayLanguage = initialLanguage;
            configurationChanged = true;
        }
        if (configurationChanged)
            store.MarkChanged();
        var localization = new LocalizationService(configuration);
        var reader = new CoordinateReader(dataManager, log, localization);
        var capture = new ScreenCaptureService(localization);
        var discord = new DiscordWebhookService(configuration, localization);
        ui = new PluginUi(configuration, gameGui, chatGui, log, reader, capture, discord, localization,
            store);

        commandManager.AddHandler(Command, new CommandInfo((_, _) => ui.OpenSettings())
        {
            HelpMessage = localization.Text(
                "Open the Glamour Saver Discord Webhook settings.",
                "Glamour SaverのDiscord Webhook設定を開きます。"),
        });
        pluginInterface.UiBuilder.Draw += ui.Draw;
        pluginInterface.UiBuilder.OpenConfigUi += ui.OpenSettings;
        pluginInterface.UiBuilder.OpenMainUi += ui.OpenSettings;
        framework.Update += OnUpdate;
        clientState.Logout += OnLogout;
        shutdownRegistration = lifecycle.GameShuttingDownToken.Register(store.Flush);
        unloadRegistration = lifecycle.DalamudUnloadingToken.Register(store.Flush);
    }

    private void OnUpdate(IFramework _) => store.Tick();
    private void OnLogout(int type, int code) => store.Flush();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        shutdownRegistration.Dispose();
        unloadRegistration.Dispose();
        framework.Update -= OnUpdate;
        clientState.Logout -= OnLogout;
        pluginInterface.UiBuilder.Draw -= ui.Draw;
        pluginInterface.UiBuilder.OpenConfigUi -= ui.OpenSettings;
        pluginInterface.UiBuilder.OpenMainUi -= ui.OpenSettings;
        commandManager.RemoveHandler(Command);
        try { ui.Dispose(); }
        finally
        {
            store.Dispose();
            if (store.CanRetry || store.Stopped)
                log.Warning("Glamour Saver settings could not be saved at shutdown. Check disk access and restore a valid configuration if necessary.");
        }
    }
}
