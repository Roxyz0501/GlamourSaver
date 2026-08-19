using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using GlamourSaver.Models;
using Dalamud.Plugin.Services;

namespace GlamourSaver.Services;

public sealed unsafe class CoordinateReader(
    IDataManager dataManager,
    IPluginLog log,
    LocalizationService localization)
{
    private static readonly string[] EnglishSlotNames =
    [
        "Main Hand", "Off Hand", "Head", "Body", "Hands", "Legs", "Feet",
        "Earrings", "Necklace", "Bracelets", "Right Ring", "Left Ring", "Soul Crystal",
    ];

    private static readonly string[] JapaneseSlotNames =
    [
        "主道具", "副道具", "頭", "胴", "手", "脚", "足",
        "耳飾り", "首飾り", "腕輪", "指輪（右）", "指輪（左）", "ジョブクリスタル",
    ];

    private string[] SlotNames => localization.IsJapanese ? JapaneseSlotNames : EnglishSlotNames;

    public bool IsReady
    {
        get
        {
            var agent = GetAgent();
            return agent != null && CountAvailableItems(agent) > 0;
        }
    }

    /// <summary>
    /// 7.3で追加されたコーディネート表示用配列が埋まっている間だけtrue。
    /// 通常の「調べる」画面ではItemsだけが有効なため、ボタン表示状態の判定に使える。
    /// </summary>
    public bool IsCoordinateViewActive
    {
        get
        {
            var agent = GetAgent();
            if (agent == null)
                return false;
            for (var i = 0; i < SlotNames.Length; i++)
            {
                if (agent->GlamourItems[i].ItemId != 0)
                    return true;
            }
            return false;
        }
    }

    public string StateDescription
    {
        get
        {
            var agent = GetAgent();
            if (agent == null)
                return localization.Text("Inspect agent was not found.", "Inspect Agentが見つかりません");
            var itemCount = CountAvailableItems(agent);
            if (itemCount > 0)
                return localization.Text(
                    $"Equipment data ready ({itemCount} slots)",
                    $"装備データ取得済み（{itemCount}枠）");
            return agent->FetchCharacterDataStatus switch
            {
                0 => localization.Text("Waiting for character data", "キャラクターデータ待機中"),
                1 => localization.Text("Loading character data", "キャラクターデータ取得中"),
                2 => localization.Text("Equipment data ready", "装備データ取得済み"),
                3 => localization.Text("Failed to load character data", "キャラクターデータ取得失敗"),
                var value => localization.Text($"Character data status: {value}", $"キャラクターデータ状態: {value}"),
            };
        }
    }

    public CoordinateSnapshot? Read()
    {
        try
        {
            var agent = GetAgent();
            var uiState = UIState.Instance();
            // FetchCharacterDataStatus は画面反映後に 0 へ戻るため、完了フラグには使わない。
            // 実際の装備配列が埋まっていることを読み取り可能条件とする。
            if (agent == null || uiState == null || CountAvailableItems(agent) == 0)
                return null;

            var name = uiState->Inspect.NameString.Trim();
            if (name.Length == 0)
                name = localization.Text(
                    $"Character {agent->CurrentEntityId:X8}",
                    $"キャラクター {agent->CurrentEntityId:X8}");

            var itemSheet = dataManager.GetExcelSheet<Item>();
            var result = new List<CoordinateItem>(SlotNames.Length);
            for (var i = 0; i < SlotNames.Length; i++)
            {
                var equipped = agent->Items[i];
                var coordinate = agent->GlamourItems[i];

                // コーディネート側の配列を最優先し、未設定時は装備に付与されたミラプリ、素の装備の順で補完。
                var itemId = coordinate.ItemId != 0
                    ? coordinate.ItemId
                    : equipped.GlamourItemId != 0 ? equipped.GlamourItemId : equipped.ItemId;
                itemId %= 1_000_000;
                if (itemId == 0 || !itemSheet.TryGetRow(itemId, out var row))
                    continue;

                var itemName = row.Name.ToString();
                if (string.IsNullOrWhiteSpace(itemName))
                    continue;

                result.Add(new CoordinateItem(i, SlotNames[i], itemId, itemName, BuildDatabaseUrl(itemName)));
            }

            return new CoordinateSnapshot(name, result);
        }
        catch (Exception ex)
        {
            log.Error(ex, "コーディネート情報の読み取りに失敗しました");
            return null;
        }
    }

    private static AgentInspect* GetAgent()
    {
        var module = AgentModule.Instance();
        return module == null
            ? null
            : (AgentInspect*)module->GetAgentByInternalId(AgentId.Inspect);
    }

    private static int CountAvailableItems(AgentInspect* agent)
    {
        var count = 0;
        for (var i = 0; i < EnglishSlotNames.Length; i++)
        {
            var equipped = agent->Items[i];
            var coordinate = agent->GlamourItems[i];
            if (coordinate.ItemId != 0 || equipped.GlamourItemId != 0 || equipped.ItemId != 0)
                count++;
        }
        return count;
    }

    private string BuildDatabaseUrl(string itemName)
    {
        var host = localization.IsJapanese ? "jp.finalfantasyxiv.com" : "na.finalfantasyxiv.com";
        return $"https://{host}/lodestone/playguide/db/item/?q=" + Uri.EscapeDataString(itemName);
    }
}
