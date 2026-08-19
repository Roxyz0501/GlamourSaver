using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;
using GlamourSaver.Models;

namespace GlamourSaver.Services;

public sealed class DiscordWebhookService(
    Configuration configuration,
    LocalizationService localization) : IDisposable
{
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };
    // Discord rejects webhook username overrides containing the word "discord".
    private const string WebhookUsername = "Glamour Saver";

    public static bool IsValidWebhook(string? raw)
    {
        if (!Uri.TryCreate(raw?.Trim(), UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;
        return (uri.Host.Equals("discord.com", StringComparison.OrdinalIgnoreCase)
                || uri.Host.Equals("discordapp.com", StringComparison.OrdinalIgnoreCase))
               && uri.AbsolutePath.StartsWith("/api/webhooks/", StringComparison.OrdinalIgnoreCase);
    }

    public async Task SendAsync(CoordinateSnapshot snapshot, byte[] png, CancellationToken cancellationToken = default)
    {
        var url = configuration.DiscordWebhookUrl.Trim();
        if (!IsValidWebhook(url))
            throw new InvalidOperationException(localization.Text(
                "Configure a Discord Webhook URL first.",
                "Discord Webhook URLを設定してください。"));

        var description = BuildDescription(snapshot);
        var payload = JsonSerializer.Serialize(new
        {
            username = WebhookUsername,
            embeds = new[]
            {
                new
                {
                    title = snapshot.CharacterName,
                    description,
                    color = 0xC78BDC,
                    image = new { url = "attachment://coordinate.png" },
                    footer = new
                    {
                        text = localization.Text(
                            $"Captured: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                            $"取得日時: {DateTime.Now:yyyy/MM/dd HH:mm:ss}"),
                    },
                },
            },
            attachments = new[] { new { id = 0, filename = "coordinate.png" } },
        });

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(payload, Encoding.UTF8, "application/json"), "payload_json");
        var image = new ByteArrayContent(png);
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(image, "files[0]", "coordinate.png");

        using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = form };
        using var response = await SendSafelyAsync(request, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(response);
    }

    public async Task TestAsync(CancellationToken cancellationToken = default)
    {
        var url = configuration.DiscordWebhookUrl.Trim();
        if (!IsValidWebhook(url))
            throw new InvalidOperationException(localization.Text(
                "Configure a Discord Webhook URL first.",
                "Discord Webhook URLを設定してください。"));
        var payload = JsonSerializer.Serialize(new
        {
            content = localization.Text(
                "Glamour Saver: Webhook connection test succeeded.",
                "Glamour Saver: Webhook接続テストに成功しました。"),
            username = WebhookUsername,
        });
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        using var response = await SendSafelyAsync(request, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(response);
    }

    public async Task ApplyIconAsync(CancellationToken cancellationToken = default)
    {
        var url = configuration.DiscordWebhookUrl.Trim();
        if (!IsValidWebhook(url))
            throw new InvalidOperationException(localization.Text(
                "Configure a Discord Webhook URL first.",
                "Discord Webhook URLを設定してください。"));

        await using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("GlamourSaver.icon.png")
                                 ?? throw new InvalidOperationException(localization.Text(
                                     "The bundled icon could not be loaded.",
                                     "内蔵アイコンを読み込めませんでした。"));
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        var avatar = "data:image/png;base64," + Convert.ToBase64String(memory.ToArray());
        var payload = JsonSerializer.Serialize(new { avatar });
        using var request = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        using var response = await SendSafelyAsync(request, cancellationToken).ConfigureAwait(false);
        EnsureSuccess(response);
    }

    private string BuildDescription(CoordinateSnapshot snapshot)
    {
        if (snapshot.Items.Count == 0)
            return localization.Text("No equipment could be read.", "装備を取得できませんでした。");

        return string.Join("\n", snapshot.Items.Select(item =>
            configuration.IncludeSlotNames
                ? $"**{item.SlotName}**: [{Escape(item.ItemName)}]({item.DatabaseUrl})"
                : $"[{Escape(item.ItemName)}]({item.DatabaseUrl})"));
    }

    private static string Escape(string value)
        => value.Replace("\\", "\\\\").Replace("*", "\\*").Replace("_", "\\_").Replace("[", "\\[").Replace("]", "\\]");

    private async Task<HttpResponseMessage> SendSafelyAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        try
        {
            return await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException(localization.Text(
                "The Discord connection timed out.",
                "Discordへの接続がタイムアウトしました。"));
        }
        catch (HttpRequestException)
        {
            // HttpClientの既定例外にはWebhook URLが含まれる場合があるため、秘密値を含まない例外へ置き換える。
            throw new HttpRequestException(localization.Text(
                "Communication with Discord failed.",
                "Discordとの通信に失敗しました。"));
        }
    }

    private void EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(localization.Text(
                $"Discord returned HTTP {(int)response.StatusCode}.",
                $"DiscordがHTTP {(int)response.StatusCode}を返しました。"));
    }

    public void Dispose() => http.Dispose();
}
