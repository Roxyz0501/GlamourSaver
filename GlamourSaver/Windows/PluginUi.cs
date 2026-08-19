using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;
using GlamourSaver.Models;
using GlamourSaver.Services;

namespace GlamourSaver.Windows;

public sealed class PluginUi : IDisposable
{
    private const string SupportUrl = "https://ko-fi.com/roxyz0501";
    private readonly Configuration configuration;
    private readonly IGameGui gameGui;
    private readonly IChatGui chat;
    private readonly IPluginLog log;
    private readonly CoordinateReader reader;
    private readonly ScreenCaptureService capture;
    private readonly DiscordWebhookService discord;
    private readonly LocalizationService localization;
    private readonly Action save;
    private readonly CancellationTokenSource lifetime = new();

    private bool settingsOpen;
    private bool inspectWasVisible;
    private bool positionEditing;
    private int draftButtonOffsetX;
    private int draftButtonOffsetY;
    private int posting;
    private string status = string.Empty;
    private string supportStatus = string.Empty;
    private volatile bool disposed;

    public PluginUi(
        Configuration configuration,
        IGameGui gameGui,
        IChatGui chat,
        IPluginLog log,
        CoordinateReader reader,
        ScreenCaptureService capture,
        DiscordWebhookService discord,
        LocalizationService localization,
        Action save)
    {
        this.configuration = configuration;
        this.gameGui = gameGui;
        this.chat = chat;
        this.log = log;
        this.reader = reader;
        this.capture = capture;
        this.discord = discord;
        this.localization = localization;
        this.save = save;
    }

    public void OpenSettings() => settingsOpen = true;

    public void Draw()
    {
        DrawCaptureButton();
        DrawSettings();
    }

    private unsafe void DrawCaptureButton()
    {
        var inspectVisible = TryGetInspectAddon(out _, out var bounds);
        if (!inspectVisible)
        {
            if (positionEditing)
            {
                CancelPositionEditing(T(
                    "Position editing was canceled because the Examine window was closed.",
                    "「調べる」画面が閉じられたため、位置変更をキャンセルしました。"));
            }
            else if (inspectWasVisible)
                status = string.Empty;
            inspectWasVisible = false;
            return;
        }
        inspectWasVisible = true;

        var offsetX = positionEditing ? draftButtonOffsetX : configuration.ButtonOffsetX;
        var offsetY = positionEditing ? draftButtonOffsetY : configuration.ButtonOffsetY;
        var position = CalculateButtonPosition(bounds, offsetX, offsetY);
        ImGui.SetNextWindowPos(position, ImGuiCond.Always);
        ImGui.SetNextWindowBgAlpha(0.92f);
        const ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                       | ImGuiWindowFlags.NoResize
                                       | ImGuiWindowFlags.NoMove
                                       | ImGuiWindowFlags.AlwaysAutoResize
                                       | ImGuiWindowFlags.NoSavedSettings
                                       | ImGuiWindowFlags.NoFocusOnAppearing;
        if (!ImGui.Begin("SendDiscord###GlamourSaverCapture", flags))
        {
            ImGui.End();
            return;
        }

        var busy = Volatile.Read(ref posting) != 0;
        var dataReady = reader.IsReady;
        if (positionEditing)
        {
            ImGui.Button("SendDiscord##PositionPreview");
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                var delta = ImGui.GetIO().MouseDelta;
                draftButtonOffsetX = Math.Clamp(draftButtonOffsetX + (int)MathF.Round(delta.X), -1000, 1000);
                draftButtonOffsetY = Math.Clamp(draftButtonOffsetY + (int)MathF.Round(delta.Y), -1000, 1000);
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(T("Drag to change the position", "ドラッグして位置を変更"));
        }
        else
        {
            if (busy || !dataReady)
                ImGui.BeginDisabled();
            var buttonLabel = busy
                ? T("Sending...", "送信中...")
                : dataReady ? "SendDiscord" : T("Loading...", "読み込み中...");
            if (ImGui.Button(buttonLabel))
                BeginPost(bounds);
            if (busy || !dataReady)
                ImGui.EndDisabled();

            if (!dataReady)
            {
                ImGui.Separator();
                ImGui.TextDisabled(reader.StateDescription);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                ImGui.Separator();
                ImGui.TextWrapped(status);
            }
        }
        ImGui.End();
    }

    private unsafe void DrawSettings()
    {
        if (!settingsOpen)
            return;

        ImGui.SetNextWindowSize(new Vector2(680, 520), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin(T(
                "Glamour Saver Settings###GlamourSaverSettings",
                "Glamour Saver 設定###GlamourSaverSettings"), ref settingsOpen))
        {
            ImGui.End();
            return;
        }

        if (ImGui.BeginTabBar("##GlamourSaverTabs"))
        {
            if (ImGui.BeginTabItem(T("Settings###SettingsTab", "設定###SettingsTab")))
            {
                DrawGeneralSettings();
                ImGui.EndTabItem();
            }

            ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.76f, 0.43f, 0.08f, 1f));
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.95f, 0.60f, 0.15f, 1f));
            ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(1.00f, 0.72f, 0.24f, 1f));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.08f, 0.06f, 0.03f, 1f));
            var supportTabOpen = ImGui.BeginTabItem(T("★ Support###SupportTab", "★ 支援###SupportTab"));
            ImGui.PopStyleColor(4);
            if (supportTabOpen)
            {
                DrawSupportTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
        ImGui.End();
    }

    private unsafe void DrawGeneralSettings()
    {
        var selectedLanguage = configuration.DisplayLanguage == DisplayLanguage.Japanese ? 1 : 0;
        ImGui.SetNextItemWidth(180);
        if (ImGui.Combo(T("Display language", "表示言語"), ref selectedLanguage, "English\0日本語\0"))
        {
            configuration.DisplayLanguage = selectedLanguage == 1
                ? DisplayLanguage.Japanese
                : DisplayLanguage.English;
            status = string.Empty;
            supportStatus = string.Empty;
            save();
        }
        ImGui.TextDisabled(T(
            "The initial language follows the game client once. Your selection is then kept until you change it.",
            "初回のみゲームクライアント言語から選択し、その後はここで選んだ言語を維持します。"));
        ImGui.Separator();

        ImGui.TextUnformatted("Discord Webhook URL");
        var webhook = configuration.DiscordWebhookUrl;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputText("##GlamourSaverWebhook", ref webhook, 2048, ImGuiInputTextFlags.Password))
            configuration.DiscordWebhookUrl = webhook;
        if (ImGui.IsItemDeactivatedAfterEdit())
            save();
        ImGui.TextDisabled(T(
            "Only https://discord.com/api/webhooks/... URLs are accepted.",
            "https://discord.com/api/webhooks/... のURLだけを受け付けます。"));
        ImGui.TextDisabled(T(
            "The URL is stored as plain text in the Dalamud Glamour Saver config, but is never written to logs or message content.",
            "URLはDalamudのGlamour Saver設定ファイルへ平文で保存され、ログや投稿本文には出力しません。"));

        var webhookMissing = string.IsNullOrWhiteSpace(configuration.DiscordWebhookUrl);
        if (webhookMissing)
            ImGui.BeginDisabled();
        if (ImGui.Button(T("Delete saved Webhook URL", "保存済みWebhook URLを削除")))
        {
            configuration.DiscordWebhookUrl = string.Empty;
            save();
            status = T("The saved Webhook URL was deleted.", "保存済みWebhook URLを削除しました。");
        }
        if (webhookMissing)
            ImGui.EndDisabled();

        var includeSlots = configuration.IncludeSlotNames;
        if (ImGui.Checkbox(T("Include equipment slot names", "装備部位名も投稿する"), ref includeSlots))
        {
            configuration.IncludeSlotNames = includeSlots;
            save();
        }

        if (ImGui.Button(T("Test Webhook connection", "Webhook接続テスト")))
            _ = TestWebhookAsync();
        ImGui.SameLine();
        if (ImGui.Button(T("Apply bundled icon to Webhook", "作成したアイコンをWebhookへ適用")))
            _ = ApplyWebhookIconAsync();
        ImGui.SameLine();
        ImGui.TextWrapped(status);

        ImGui.Separator();
        ImGui.TextColored(new Vector4(1.00f, 0.68f, 0.25f, 1f),
            T("Data sent to Discord", "Discordへ送信する内容"));
        ImGui.BulletText(T("Target character name", "対象キャラクター名"));
        ImGui.BulletText(T(
            "PNG crop of the Examine window",
            "「調べる」画面を切り出したPNG画像"));
        ImGui.BulletText(T(
            "Equipment slot names (optional), item names, and Eorzea Database search links",
            "各装備の部位名（設定で省略可能）、アイテム名、エオルゼアデータベース検索リンク"));
        ImGui.TextWrapped(T(
            "Data is sent only when you explicitly press SendDiscord. Nothing is sent automatically.",
            "送信は、ユーザーが「SendDiscord」を明示的に押した場合だけ実行します。自動送信は行いません。"));

        ImGui.Separator();
        ImGui.TextUnformatted(T("SendDiscord button position", "SendDiscordボタンの位置"));
        if (ImGui.Button(positionEditing
                ? T("Cancel", "キャンセル")
                : T("Change button position", "ボタンの位置を変更")))
        {
            if (positionEditing)
            {
                CancelPositionEditing(T("Position editing was canceled.", "位置変更をキャンセルしました。"));
            }
            else if (!TryGetInspectAddon(out _, out _))
            {
                status = T(
                    "Open the Examine window before changing the button position.",
                    "位置を変更するには、先に「調べる」画面を表示してください。");
            }
            else
            {
                draftButtonOffsetX = configuration.ButtonOffsetX;
                draftButtonOffsetY = configuration.ButtonOffsetY;
                positionEditing = true;
                status = T(
                    "Drag the SendDiscord button next to the Examine window, then press Save position.",
                    "「調べる」画面上のSendDiscordボタンをドラッグし、「位置を保存」を押してください。");
            }
        }
        ImGui.SameLine();
        var savePositionDisabled = !positionEditing;
        if (savePositionDisabled)
            ImGui.BeginDisabled();
        if (ImGui.Button(T("Save position", "位置を保存")))
        {
            if (!TryGetInspectAddon(out _, out _))
            {
                CancelPositionEditing(T(
                    "Position editing was canceled because the Examine window was closed.",
                    "「調べる」画面が閉じられたため、位置変更をキャンセルしました。"));
            }
            else
            {
                configuration.ButtonOffsetX = draftButtonOffsetX;
                configuration.ButtonOffsetY = draftButtonOffsetY;
                save();
                positionEditing = false;
                status = T("The SendDiscord button position was saved.", "SendDiscordボタンの位置を保存しました。");
            }
        }
        if (savePositionDisabled)
            ImGui.EndDisabled();
        ImGui.SameLine();
        if (ImGui.Button(T("Reset to default", "位置をデフォルトへ戻す")))
        {
            if (positionEditing)
            {
                draftButtonOffsetX = 0;
                draftButtonOffsetY = 0;
                status = T(
                    "The preview was reset to the default position. Press Save position to confirm.",
                    "プレビュー位置をデフォルトへ戻しました。「位置を保存」で確定してください。");
            }
            else
            {
                configuration.ButtonOffsetX = 0;
                configuration.ButtonOffsetY = 0;
                save();
                status = T(
                    "The SendDiscord button was reset to its default position.",
                    "SendDiscordボタンの位置をデフォルトへ戻しました。");
            }
        }
        ImGui.TextDisabled(positionEditing
            ? T(
                "Drag the preview button next to the Examine window to choose its position.",
                "「調べる」画面上のプレビューボタンをドラッグして位置を決めてください。")
            : T(
                "The button can be dragged only while position editing is active.",
                "位置変更中だけ、「調べる」画面上のボタンをドラッグできます。"));

        ImGui.Separator();
        ImGui.TextWrapped(T(
            "Usage: Right-click another character, open Examine, then press the displayed SendDiscord button.",
            "使い方: 他キャラクターを右クリックして「調べる」を開き、表示された「SendDiscord」を押します。"));
        ImGui.TextDisabled(GetInspectDetectionText());
    }

    private void DrawSupportTab()
    {
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1.00f, 0.68f, 0.20f, 1f),
            T("Support Roxyz0501's development", "Roxyz0501の開発を支援"));
        ImGui.Spacing();
        ImGui.TextWrapped(T(
            "If you enjoy Glamour Saver, you can optionally support development through Ko-fi. All features remain available whether or not you contribute.",
            "Glamour Saverを気に入っていただけた場合、Ko-fiから任意で開発を支援できます。支援の有無で機能が変わることはありません。"));
        ImGui.Spacing();
        ImGui.TextUnformatted(T("Recipient: Roxyz0501", "支援先: Roxyz0501"));
        ImGui.TextDisabled(SupportUrl);
        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.90f, 0.48f, 0.08f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(1.00f, 0.62f, 0.14f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.78f, 0.36f, 0.04f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.08f, 0.05f, 0.02f, 1f));
        var openSupport = ImGui.Button(
            T("Support Roxyz0501 on Ko-fi", "Ko-fiでRoxyz0501を支援する"),
            new Vector2(320, 52));
        ImGui.PopStyleColor(4);

        if (openSupport)
            OpenSupportPage();
        if (!string.IsNullOrWhiteSpace(supportStatus))
            ImGui.TextWrapped(supportStatus);
    }

    private void OpenSupportPage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(SupportUrl) { UseShellExecute = true });
            supportStatus = T(
                "Opened Roxyz0501's Ko-fi page in your browser.",
                "Ko-fiのRoxyz0501ページをブラウザで開きました。");
        }
        catch
        {
            supportStatus = T(
                $"The browser could not be opened. Open this URL manually: {SupportUrl}",
                $"ブラウザを開けませんでした。URLを直接開いてください: {SupportUrl}");
        }
    }

    private void BeginPost(FFXIVClientStructs.FFXIV.Common.Math.Bounds bounds)
    {
        if (Interlocked.CompareExchange(ref posting, 1, 0) != 0)
            return;

        try
        {
            if (!DiscordWebhookService.IsValidWebhook(configuration.DiscordWebhookUrl))
                throw new InvalidOperationException(T(
                    "Configure a Discord Webhook URL with /glamoursaver first.",
                    "先に /glamoursaver でDiscord Webhook URLを設定してください。"));

            var snapshot = reader.Read() ?? throw new InvalidOperationException(T(
                "The character's glamour could not be read.",
                "キャラクターのコーディネートを取得できませんでした。"));
            if (snapshot.Items.Count == 0)
                throw new InvalidOperationException(T(
                    "Appearance data is not ready. Open the Coordinate view and try again.",
                    "見た目装備がまだ読み込まれていません。コーディネート画面を開いてから再度お試しください。"));

            var region = ToScreenRegion(bounds, configuration.CapturePadding);
            var png = capture.CapturePng(region);
            status = T("Sending to Discord...", "Discordへ送信中...");
            _ = PostAsync(snapshot, png, lifetime.Token);
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref posting, 0);
            status = ex.Message;
            chat.PrintError($"[Glamour Saver] {ex.Message}");
            log.Error(ex, "ミラプリ取得処理に失敗しました");
        }
    }

    private async Task PostAsync(CoordinateSnapshot snapshot, byte[] png, CancellationToken cancellationToken)
    {
        try
        {
            await discord.SendAsync(snapshot, png, cancellationToken).ConfigureAwait(false);
            status = T(
                $"Posted {snapshot.CharacterName}'s glamour.",
                $"{snapshot.CharacterName} のコーディネートを投稿しました。");
            chat.Print($"[Glamour Saver] {status}");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // プラグインのアンロード時は通知せず終了する。
        }
        catch (Exception) when (disposed)
        {
            // アンロードと通信完了が競合した場合も通知せず終了する。
        }
        catch (Exception ex)
        {
            status = T("Post failed: ", "投稿失敗: ") + ex.Message;
            chat.PrintError($"[Glamour Saver] {status}");
            log.Error(ex, "Discordへの投稿に失敗しました");
        }
        finally
        {
            Interlocked.Exchange(ref posting, 0);
        }
    }

    private async Task TestWebhookAsync()
    {
        try
        {
            status = T("Testing connection...", "接続テスト中...");
            await discord.TestAsync(lifetime.Token).ConfigureAwait(false);
            status = T("Connection test succeeded.", "接続テストに成功しました。");
        }
        catch (OperationCanceledException) when (disposed)
        {
        }
        catch (Exception) when (disposed)
        {
        }
        catch (Exception ex)
        {
            status = T("Connection test failed: ", "接続テスト失敗: ") + ex.Message;
            log.Warning(ex, "Discord Webhook接続テストに失敗しました");
        }
    }

    private async Task ApplyWebhookIconAsync()
    {
        try
        {
            status = T("Applying the Webhook icon...", "Webhookアイコンを適用中...");
            await discord.ApplyIconAsync(lifetime.Token).ConfigureAwait(false);
            status = T(
                "Applied the Glamour Saver icon to the Webhook.",
                "WebhookアイコンをGlamour Saverへ適用しました。");
        }
        catch (OperationCanceledException) when (disposed)
        {
        }
        catch (Exception) when (disposed)
        {
        }
        catch (Exception ex)
        {
            status = T("Failed to apply icon: ", "アイコン適用失敗: ") + ex.Message;
            log.Warning(ex, "Discord Webhookアイコンの適用に失敗しました");
        }
    }

    private unsafe bool TryGetInspectAddon(out AtkUnitBase* addon, out FFXIVClientStructs.FFXIV.Common.Math.Bounds bounds)
    {
        addon = null;
        bounds = default;
        var addonPtr = gameGui.GetAddonByName("CharacterInspect", 1);
        addon = (AtkUnitBase*)addonPtr.Address;
        if (addon == null || !addon->IsVisible)
            return false;
        FFXIVClientStructs.FFXIV.Common.Math.Bounds localBounds;
        addon->GetWindowBounds(&localBounds);
        bounds = localBounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            var width = (int)MathF.Round(addon->GetScaledWidth(true));
            var height = (int)MathF.Round(addon->GetScaledHeight(true));
            bounds.Pos1.X = addon->X;
            bounds.Pos1.Y = addon->Y;
            bounds.Pos2.X = addon->X + width;
            bounds.Pos2.Y = addon->Y + height;
        }
        return bounds.Width > 0 && bounds.Height > 0;
    }

    private unsafe string GetInspectDetectionText()
        => TryGetInspectAddon(out _, out var bounds)
            ? T(
                $"Window detected: CharacterInspect ({bounds.Width}x{bounds.Height}) / {reader.StateDescription}",
                $"画面検出: CharacterInspect ({bounds.Width}x{bounds.Height}) / {reader.StateDescription}")
            : T(
                "Window detection: CharacterInspect is not currently visible.",
                "画面検出: CharacterInspectは現在表示されていません。");

    private Vector2 CalculateButtonPosition(
        FFXIVClientStructs.FFXIV.Common.Math.Bounds bounds,
        int offsetX,
        int offsetY)
    {
        const float estimatedWidth = 230;
        var displaySize = ImGui.GetIO().DisplaySize;
        var x = bounds.Pos2.X + 8f;
        if (x + estimatedWidth > displaySize.X - 8)
            x = bounds.Pos1.X - estimatedWidth - 8;
        x += offsetX;
        var y = bounds.Pos1.Y + 42f + offsetY;
        x = Math.Clamp(x, 8, Math.Max(8, displaySize.X - estimatedWidth - 8));
        y = Math.Clamp(y, 8, Math.Max(8, displaySize.Y - 100f));
        return new Vector2(x, y);
    }

    private void CancelPositionEditing(string message)
    {
        positionEditing = false;
        draftButtonOffsetX = configuration.ButtonOffsetX;
        draftButtonOffsetY = configuration.ButtonOffsetY;
        status = message;
    }

    private string T(string english, string japanese)
        => localization.Text(english, japanese);

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        lifetime.Cancel();
        discord.Dispose();
        lifetime.Dispose();
    }

    private static ScreenRegion ToScreenRegion(FFXIVClientStructs.FFXIV.Common.Math.Bounds bounds, int padding)
    {
        var hwnd = Process.GetCurrentProcess().MainWindowHandle;
        var origin = new NativePoint();
        ClientToScreen(hwnd, ref origin);
        padding = Math.Clamp(padding, 0, 20);
        return new ScreenRegion(
            origin.X + bounds.Pos1.X - padding,
            origin.Y + bounds.Pos1.Y - padding,
            bounds.Width + (padding * 2),
            bounds.Height + (padding * 2));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hwnd, ref NativePoint point);
}
