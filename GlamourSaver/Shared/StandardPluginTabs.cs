using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Utility;

namespace Roxyz0501.Dalamud.CommonUi;

/// <summary>Shared visual and interaction contract for Roxyz0501 Dalamud plugin tabs.</summary>
internal static class StandardPluginTabs
{
    public static IReadOnlyList<(string Code, string SelfName)> Languages { get; } =
    [
        ("ja", "日本語"),
        ("en", "English"),
        ("de", "Deutsch"),
        ("fr", "Français"),
        ("ko", "한국어"),
        ("zh-Hans", "简体中文"),
        ("zh-Hant", "繁體中文"),
    ];

    public static void PushSupportTabStyle()
    {
        ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.55f, 0.34f, 0.05f, 1f));
        ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.95f, 0.58f, 0.08f, 1f));
        ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.82f, 0.47f, 0.05f, 1f));
        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0.95f, 0.82f, 1f));
    }

    public static void PopSupportTabStyle() => ImGui.PopStyleColor(4);

    public static string? DrawLanguageSelector(string label, string help, string currentCode)
    {
        (string Code, string SelfName) selected = Languages.FirstOrDefault(x => x.Code == currentCode);
        string preview = string.IsNullOrEmpty(selected.Code) ? "English" : selected.SelfName;
        string? changed = null;
        ImGui.SetNextItemWidth(220f);
        if (ImGui.BeginCombo($"{label}###DisplayLanguage", preview))
        {
            foreach ((string code, string selfName) in Languages)
            {
                if (ImGui.Selectable($"{selfName}###{code}", code == currentCode))
                    changed = code;
            }
            ImGui.EndCombo();
        }
        ImGui.TextWrapped(help);
        return changed;
    }

    public static void DrawSupportContent(string heading, string description, string recipient, string buttonLabel)
    {
        const string supportUrl = "https://ko-fi.com/roxyz0501";
        ImGui.Spacing();
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.18f, 1f), heading);
        ImGui.Spacing();
        ImGui.TextWrapped(description);
        ImGui.TextWrapped(recipient);
        ImGui.TextDisabled(supportUrl);
        ImGui.Spacing();

        ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.78f, 0.39f, 0.05f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.95f, 0.52f, 0.08f, 1f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.68f, 0.31f, 0.03f, 1f));
        float width = MathF.Min(ImGui.GetContentRegionAvail().X,
            MathF.Max(300f, ImGui.CalcTextSize(buttonLabel).X + ImGui.GetStyle().FramePadding.X * 4f));
        bool clicked = ImGui.Button($"{buttonLabel}###KoFiSupport", new Vector2(width, 46f));
        ImGui.PopStyleColor(3);
        if (clicked)
            Util.OpenLink(supportUrl);
    }
}
