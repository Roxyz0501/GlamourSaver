using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Plugin;
using Dalamud.Utility;

namespace Roxyz0501.Dalamud.CommonUi;

// Use language codes ja/en/de/fr/ko/zh-Hans/zh-Hant. English is the fallback.
internal sealed record InfoText(string En, string Ja, string De, string Fr, string Ko, string Hans, string Hant)
{
    public string Get(string code) => code switch { "ja" => Ja, "de" => De, "fr" => Fr, "ko" => Ko, "zh-Hans" => Hans, "zh-Hant" => Hant, _ => En };
}

internal sealed record PluginDependency(string InternalName, string Name, bool Required, InfoText Purpose, InfoText? Note = null, string? Guide = null);

internal sealed class StandardPluginInfo(params PluginDependency[] dependencies)
{
    private Dictionary<string, bool>? installed;
    private bool checkedOnce, actionFailed;
    private static readonly InfoText[] labels =
    [
        new("Information", "情報", "Information", "Informations", "정보", "信息", "資訊"),
        new("Required plugins", "必須プラグイン", "Erforderliche Plugins", "Plugins requis", "필수 플러그인", "必需插件", "必要插件"),
        new("None. Basic features work independently.", "なし。基本機能は単体で利用できます。", "Keine. Grundfunktionen sind unabhängig.", "Aucun. Les fonctions de base sont autonomes.", "없음. 기본 기능은 단독으로 작동합니다.", "无。基本功能可独立使用。", "無。基本功能可獨立使用。"),
        new("Feature dependencies", "機能別に必要なプラグイン", "Funktionsabhängigkeiten", "Plugins par fonctionnalité", "기능별 플러그인", "功能依赖插件", "功能所需插件"),
        new("Refresh status", "状態を再確認", "Status prüfen", "Actualiser", "상태 확인", "刷新状态", "重新確認狀態"),
        new("Plugin", "プラグイン", "Plugin", "Plugin", "플러그인", "插件", "插件"),
        new("Status", "状態", "Status", "État", "상태", "状态", "狀態"),
        new("Feature", "使用する機能", "Funktion", "Fonction", "기능", "功能", "功能"),
        new("Action", "操作", "Aktion", "Action", "작업", "操作", "操作"),
        new("Ready", "利用可能", "Bereit", "Disponible", "사용 가능", "可用", "可用"),
        new("Not loaded", "導入済み・無効", "Nicht geladen", "Non chargé", "설치됨・비활성", "已安装・未启用", "已安裝・未啟用"),
        new("Not installed", "未導入", "Nicht installiert", "Non installé", "미설치", "未安装", "未安裝"),
        new("Unknown", "確認できません", "Unbekannt", "Inconnu", "확인 불가", "无法确认", "無法確認"),
        new("Install", "導入する", "Installieren", "Installer", "설치", "安装", "安裝"),
        new("Manage", "管理を開く", "Verwalten", "Gérer", "관리", "管理", "管理"),
        new("Setup guide", "導入案内", "Anleitung", "Guide", "설치 안내", "安装指南", "安裝指南"),
        new("Could not open.", "開けませんでした。", "Öffnen fehlgeschlagen.", "Ouverture impossible.", "열 수 없습니다.", "无法打开。", "無法開啟。"),
    ];
    public static string Title(string code) => labels[0].Get(code);
    public void Draw(IDalamudPluginInterface host, string code)
    {
        string L(int i) => labels[i].Get(code);
        if (ImGui.Button(L(4)) || !checkedOnce)
        {
            checkedOnce = true;
            actionFailed = false;
            try
            {
                var result = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in host.InstalledPlugins)
                    result[item.InternalName] = item.IsLoaded || result.GetValueOrDefault(item.InternalName);
                installed = result;
            }
            catch { installed = null; }
        }
        Section(true);
        Section(false);
        if (actionFailed) ImGui.TextUnformatted(L(16));
        void Section(bool required)
        {
            bool any = Array.Exists(dependencies, d => d.Required == required);
            if (!required && !any) return;
            ImGui.Separator();
            ImGui.TextUnformatted(L(required ? 1 : 3));
            if (!any) { ImGui.TextWrapped(L(2)); return; }
            if (!ImGui.BeginTable(required ? "required##info" : "optional##info", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg)) return;
            try
            {
                for (int i = 5; i <= 8; i++) ImGui.TableSetupColumn(L(i));
                ImGui.TableHeadersRow();
                foreach (var d in dependencies)
                {
                    if (d.Required != required) continue;
                    int state = installed is null ? 12 : !installed.TryGetValue(d.InternalName, out bool loaded) ? 11 : loaded ? 9 : 10;
                    ImGui.TableNextRow(); ImGui.TableNextColumn(); ImGui.TextUnformatted(d.Name);
                    ImGui.TableNextColumn(); ImGui.TextColored(state switch { 9 => new Vector4(.4f,.9f,.5f,1), 10 => new Vector4(1,.8f,.3f,1), 11 => new Vector4(1,.5f,.5f,1), _ => Vector4.One }, L(state));
                    ImGui.TableNextColumn(); ImGui.TextWrapped(d.Purpose.Get(code));
                    ImGui.TableNextColumn();
                    if (state is 10 or 11 && ImGui.SmallButton($"{L(state == 11 ? 13 : 14)}##{d.InternalName}"))
                    {
                        try { actionFailed = !host.OpenPluginInstallerTo(state == 11 ? PluginInstallerOpenKind.AllPlugins : PluginInstallerOpenKind.InstalledPlugins, d.InternalName); }
                        catch { actionFailed = true; }
                    }
                    if (state == 11 && Uri.TryCreate(d.Guide, UriKind.Absolute, out var url) && url.Scheme == "https" && ImGui.SmallButton($"{L(15)}##guide{d.InternalName}"))
                    {
                        try { Util.OpenLink(url.AbsoluteUri); } catch { actionFailed = true; }
                    }
                }
            }
            finally { ImGui.EndTable(); }
            foreach (var d in dependencies) if (d.Required == required && d.Note != null) ImGui.TextWrapped(d.Note.Get(code));
        }
    }
}
