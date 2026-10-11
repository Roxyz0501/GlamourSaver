using System.Globalization;
using Dalamud.Game;

namespace GlamourSaver.Services;

public enum DisplayLanguage
{
    English = 1,
    Japanese = 2,
    German = 3,
    French = 4,
    Korean = 5,
    SimplifiedChinese = 6,
    TraditionalChinese = 7,
}

public sealed class LocalizationService(Configuration configuration)
{
    public static readonly DisplayLanguage[] SelectionOrder =
    [DisplayLanguage.Japanese, DisplayLanguage.English, DisplayLanguage.German, DisplayLanguage.French,
     DisplayLanguage.Korean, DisplayLanguage.SimplifiedChinese, DisplayLanguage.TraditionalChinese];

    public static readonly string[] LanguageNames =
    ["日本語", "English", "Deutsch", "Français", "한국어", "简体中文", "繁體中文"];

    private static readonly IReadOnlyDictionary<string, string[]> Catalog = BuildCatalog();

    public DisplayLanguage ResolvedLanguage
        => IsValid(configuration.DisplayLanguage) ? configuration.DisplayLanguage!.Value : DisplayLanguage.English;

    public string LanguageCode => ResolvedLanguage switch
    {
        DisplayLanguage.Japanese => "ja",
        DisplayLanguage.German => "de",
        DisplayLanguage.French => "fr",
        DisplayLanguage.Korean => "ko",
        DisplayLanguage.SimplifiedChinese => "zh-Hans",
        DisplayLanguage.TraditionalChinese => "zh-Hant",
        _ => "en",
    };

    public static bool TryFromCode(string code, out DisplayLanguage language)
    {
        language = FromLanguageCode(code) ?? DisplayLanguage.English;
        return FromLanguageCode(code) is not null;
    }

    public string Text(string englishKey, string legacyJapanese)
        => ResolvedLanguage == DisplayLanguage.Japanese && !string.IsNullOrWhiteSpace(legacyJapanese)
            ? legacyJapanese
            : Lookup(englishKey);
    public string Text(string englishKey, params object?[] args)
        => Format(englishKey, args);
    public string Format(string englishKey, params object?[] args)
        => string.Format(CultureInfo.CurrentCulture, Lookup(englishKey), args);

    private string Lookup(string key)
    {
        if (Catalog.TryGetValue(key, out var row))
            return row[(int)ResolvedLanguage - 1];
        return string.IsNullOrWhiteSpace(key) ? "[missing-localization-key]" : key;
    }

    public static bool IsValid(DisplayLanguage? language)
        => language is >= DisplayLanguage.English and <= DisplayLanguage.TraditionalChinese;

    public static DisplayLanguage ResolveInitial(DisplayLanguage? saved, ClientLanguage? game, string? dalamudUi)
    {
        if (IsValid(saved)) return saved!.Value;
        return FromGameLanguage(game) ?? FromLanguageCode(dalamudUi) ?? DisplayLanguage.English;
    }

    public static DisplayLanguage? FromGameLanguage(ClientLanguage? language) => language switch
    {
        ClientLanguage.Japanese => DisplayLanguage.Japanese,
        ClientLanguage.English => DisplayLanguage.English,
        ClientLanguage.German => DisplayLanguage.German,
        ClientLanguage.French => DisplayLanguage.French,
        _ => null,
    };

    public static DisplayLanguage? FromLanguageCode(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var code = raw.Trim().Replace('_', '-').ToLowerInvariant();
        if (code == "zh") return null;
        if (code is "zh-hans" or "zh-cn" or "zh-sg" || code.StartsWith("zh-hans-", StringComparison.Ordinal))
            return DisplayLanguage.SimplifiedChinese;
        if (code is "zh-hant" or "zh-tw" or "zh-hk" or "zh-mo" || code.StartsWith("zh-hant-", StringComparison.Ordinal))
            return DisplayLanguage.TraditionalChinese;
        return code.Split('-', 2)[0] switch
        {
            "ja" => DisplayLanguage.Japanese,
            "en" => DisplayLanguage.English,
            "de" => DisplayLanguage.German,
            "fr" => DisplayLanguage.French,
            "ko" => DisplayLanguage.Korean,
            _ => null,
        };
    }

    public static void ValidateOrThrow()
    {
        var failures = new List<string>();
        foreach (var (key, row) in Catalog)
        {
            if (row.Length != 7 || row.Any(string.IsNullOrWhiteSpace))
                failures.Add(key);
            else
            {
                var expected = FormatIndexes(row[0]);
                if (row.Skip(1).Any(value => !expected.SetEquals(FormatIndexes(value))))
                    failures.Add("format: " + key);
            }
        }
        var rules = ResolveInitial(DisplayLanguage.English, ClientLanguage.Japanese, "ja") == DisplayLanguage.English
                    && ResolveInitial(DisplayLanguage.Japanese, ClientLanguage.English, "en") == DisplayLanguage.Japanese
                    && ResolveInitial(null, ClientLanguage.Japanese, "en") == DisplayLanguage.Japanese
                    && ResolveInitial(null, ClientLanguage.English, "ja") == DisplayLanguage.English
                    && ResolveInitial(null, null, "de-DE") == DisplayLanguage.German
                    && ResolveInitial(null, null, "fr_FR") == DisplayLanguage.French
                    && ResolveInitial(null, null, "ko-KR") == DisplayLanguage.Korean
                    && ResolveInitial(null, null, "zh-CN") == DisplayLanguage.SimplifiedChinese
                    && ResolveInitial(null, null, "zh-Hant") == DisplayLanguage.TraditionalChinese
                    && ResolveInitial(null, null, "zh") == DisplayLanguage.English
                    && ResolveInitial(null, null, "unknown") == DisplayLanguage.English;
        if (!rules) failures.Add("resolution rules");
        if (failures.Count != 0)
            throw new InvalidOperationException("Localization validation failed: " + string.Join("; ", failures));
    }

    private static HashSet<int> FormatIndexes(string value)
    {
        var result = new HashSet<int>();
        for (var i = 0; i < value.Length - 1; i++)
        {
            if (value[i] != '{' || !char.IsDigit(value[i + 1])) continue;
            var end = i + 1;
            while (end < value.Length && char.IsDigit(value[end])) end++;
            if (int.TryParse(value.AsSpan(i + 1, end - i - 1), out var index)) result.Add(index);
        }
        return result;
    }

    private static string[] R(string en, string ja, string de, string fr, string ko, string zhHans, string zhHant)
        => [en, ja, de, fr, ko, zhHans, zhHant];

    private static IReadOnlyDictionary<string, string[]> BuildCatalog()
    {
        string[][] rows =
        [
        R("Language settings", "言語設定", "Spracheinstellungen", "Paramètres de langue", "언어 설정", "语言设置", "語言設定"),
        R("Language settings###LanguageSettings", "言語設定###LanguageSettings", "Spracheinstellungen###LanguageSettings", "Paramètres de langue###LanguageSettings", "언어 설정###LanguageSettings", "语言设置###LanguageSettings", "語言設定###LanguageSettings"),
        R("Settings", "設定", "Einstellungen", "Paramètres", "설정", "设置", "設定"),
        R("Support", "支援", "Unterstützung", "Soutien", "후원", "支持", "支援"),
        R("Support###Support", "支援###Support", "Unterstützung###Support", "Soutien###Support", "후원###Support", "支持###Support", "支援###Support"),
        R("★ Support###SupportTab", "★サポート###SupportTab", "★ Unterstützung###SupportTab", "★ Assistance###SupportTab", "★ 지원###SupportTab", "★ 支持###SupportTab", "★ 支持###SupportTab"),
        R("Appearance data is not ready. Open the Coordinate view and try again.", "外観データが準備されていません。座標ビューを開いて、再試行してください。", "Die Darstellungsdaten sind nicht bereit. Öffnen Sie die Koordinatenansicht und versuchen Sie es erneut.", "Les données d'apparence ne sont pas prêtes. Ouvrez la vue Coordonnées et réessayez.", "외모 데이터가 준비되지 않았습니다. 좌표 보기를 열고 다시 시도하세요.", "外观数据尚未准备好。打开坐标视图并重试。", "外觀數據尚未準備好。開啟座標視圖並重試。"),
        R("Applied the Glamour Saver icon to the Webhook.", "Glamour Saver アイコンを Webhook に適用しました。", "Das Glamour Saver-Symbol wurde auf den Webhook angewendet.", "Application de l'icône Glamour Saver au Webhook.", "Webhook에 Glamour Saver 아이콘을 적용했습니다.", "将 Glamour Saver 图标应用到 Webhook。", "將 Glamour Saver 圖示套用到 Webhook。"),
        R("Apply bundled icon to Webhook", "バンドルされたアイコンを Webhook に適用する", "Gebündeltes Symbol auf Webhook anwenden", "Appliquer l'icône fournie au Webhook", "Webhook에 번들 아이콘 적용", "将捆绑图标应用到 Webhook", "將捆綁圖示套用到 Webhook"),
        R("Applying the Webhook icon...", "Webhook アイコンを適用しています...", "Anwenden des Webhook-Symbols...", "Application de l'icône Webhook...", "웹훅 아이콘을 적용하는 중...", "正在应用 Webhook 图标...", "正在套用 Webhook 圖示..."),
        R("Body", "本体", "Körper", "Corps", "본체", "身体", "身體"),
        R("Bracelets", "ブレスレット", "Armbänder", "Bracelets", "팔찌", "手链", "手鍊"),
        R("Cancel", "キャンセル", "Abbrechen", "Annuler", "취소", "取消", "取消"),
        R("Captured: {0}", "捕獲されました: {0}", "Erfasst: {0}", "Capturé : {0}", "캡처됨: {0}", "捕获：{0}", "捕獲：{0}"),
        R("Change button position", "ボタンの位置を変更する", "Tastenposition ändern", "Changer la position du bouton", "버튼 위치 변경", "更改按钮位置", "更改按鈕位置"),
        R("Character {0:X8}", "キャラクター {0:X8}", "Zeichen {0:X8}", "Caractère {0:X8}", "문자 {0:X8}", "角色 {0:X8}", "角色 {0:X8}"),
        R("Character data status: {0}", "文字データのステータス: {0}", "Zeichendatenstatus: {0}", "État des données de personnage : {0}", "문자 데이터 상태: {0}", "角色数据状态：{0}", "角色資料狀態：{0}"),
        R("Communication with Discord failed.", "Discordとの通信に失敗しました。", "Die Kommunikation mit Discord ist fehlgeschlagen.", "La communication avec Discord a échoué.", "Discord와의 통신에 실패했습니다.", "与 Discord 的通信失败。", "與 Discord 的通訊失敗。"),
        R("Configure a Discord Webhook URL first.", "まず、Discord Webhook URL を設定します。", "Konfigurieren Sie zunächst eine Discord-Webhook-URL.", "Configurez d’abord une URL de webhook Discord.", "먼저 Discord Webhook URL을 구성하세요.", "首先配置一个 Discord Webhook URL。", "首先配置一個 Discord Webhook URL。"),
        R("Configure a Discord Webhook URL with /glamoursaver first.", "まず、/glamoursaver を使用して Discord Webhook URL を設定します。", "Konfigurieren Sie zunächst eine Discord-Webhook-URL mit /glamoursaver.", "Configurez d’abord une URL de Webhook Discord avec /glamoursaver.", "먼저 /glamoursaver를 사용하여 Discord Webhook URL을 구성하세요.", "首先使用 /glamorsaver 配置 Discord Webhook URL。", "首先使用 /glamorsaver 配置 Discord Webhook URL。"),
        R("Connection test failed: {0}", "接続テストが失敗しました: {0}", "Verbindungstest fehlgeschlagen: {0}", "Échec du test de connexion : {0}", "연결 테스트 실패: {0}", "连接测试失败：{0}", "連結測試失敗：{0}"),
        R("Connection test succeeded.", "接続テストに成功しました。", "Verbindungstest erfolgreich.", "Le test de connexion a réussi.", "연결 테스트에 성공했습니다.", "连接测试成功。", "連線測試成功。"),
        R("Data is sent only when you explicitly press SendDiscord. Nothing is sent automatically.", "データは明示的に SendDiscord を押した場合にのみ送信されます。何も自動的には送信されません。", "Daten werden nur gesendet, wenn Sie explizit auf SendDiscord klicken. Es wird nichts automatisch gesendet.", "Les données sont envoyées uniquement lorsque vous appuyez explicitement sur SendDiscord. Rien n'est envoyé automatiquement.", "SendDiscord를 명시적으로 누른 경우에만 데이터가 전송됩니다. 아무것도 자동으로 전송되지 않습니다.", "仅当您明确按 SendDiscord 时才会发送数据。不会自动发送任何内容。", "仅当您明确按 SendDiscord 时才会发送数据。不會自動發送任何內容。"),
        R("Data sent to Discord", "Discordに送信されるデータ", "An Discord gesendete Daten", "Données envoyées à Discord", "Discord로 전송된 데이터", "数据发送至 Discord", "資料傳送至 Discord"),
        R("Delete saved Webhook URL", "保存された Webhook URL を削除する", "Gespeicherte Webhook-URL löschen", "Supprimer l'URL du Webhook enregistrée", "저장된 웹훅 URL 삭제", "删除已保存的 Webhook URL", "刪除已儲存的 Webhook URL"),
        R("Discord returned HTTP {0}.", "Discord は HTTP {0} を返しました。", "Discord hat HTTP {0} zurückgegeben.", "Discord a renvoyé HTTP {0}.", "Discord가 HTTP {0}을(를) 반환했습니다.", "Discord 返回了 HTTP {0}。", "Discord 回傳了 HTTP {0}。"),
        R("Discord Webhook URL", "Discord Webhook URL", "Discord-Webhook-URL", "URL du webhook Discord", "Discord 웹훅 URL", "Discord Webhook URL", "Discord Webhook URL"),
        R("Display language", "表示言語", "Anzeigesprache", "Langue d'affichage", "표시 언어", "显示语言", "顯示語言"),
        R("Drag the preview button next to the Examine window to choose its position.", "「検査」ウィンドウの横にある「プレビュー」ボタンをドラッグして、その位置を選択します。", "Ziehen Sie die Vorschauschaltfläche neben das Fenster „Untersuchen“, um dessen Position auszuwählen.", "Faites glisser le bouton d'aperçu à côté de la fenêtre Examiner pour choisir sa position.", "검사 창 옆에 있는 미리보기 버튼을 드래그하여 위치를 선택하세요.", "拖动检查窗口旁边的预览按钮以选择其位置。", "拖动检查窗口旁边的预览按钮以选择其位置。"),
        R("Drag the SendDiscord button next to the Examine window, then press Save position.", "「検査」ウィンドウの横にある「SendDiscord」ボタンをドラッグし、「位置を保存」を押します。", "Ziehen Sie die Schaltfläche „SendDiscord“ neben das Fenster „Untersuchen“ und klicken Sie dann auf „Position speichern“.", "Faites glisser le bouton SendDiscord à côté de la fenêtre Examiner, puis appuyez sur Enregistrer la position.", "검사 창 옆에 있는 SendDiscord 버튼을 드래그한 다음 위치 저장을 누릅니다.", "拖动检查窗口旁边的 SendDiscord 按钮，然后按保存位置。", "拖动检查窗口旁边的 SendDiscord 按钮，然后按保存位置。"),
        R("Drag to change the position", "ドラッグして位置を変更します", "Ziehen Sie, um die Position zu ändern", "Faites glisser pour changer la position", "위치를 변경하려면 드래그하세요.", "拖动以更改位置", "拖曳以更改位置"),
        R("Earrings", "イヤリング", "Ohrringe", "Boucles d'oreilles", "귀걸이", "耳环", "耳環"),
        R("Equipment data ready", "設備データの準備完了", "Gerätedaten bereit", "Données d'équipement prêtes", "장비 데이터 준비됨", "设备数据准备就绪", "設備資料準備就緒"),
        R("Equipment data ready ({0} slots)", "機器データの準備ができました ({0} スロット)", "Gerätedaten bereit ({0} Slots)", "Données d'équipement prêtes ({0} emplacements)", "장비 데이터 준비됨({0} 슬롯)", "设备数据就绪（{0} 个插槽）", "設備資料就緒（{0} 個插槽）"),
        R("Equipment slot names (optional), item names, and Eorzea Database search links", "装備スロット名(任意)、アイテム名、エオルゼアデータベース検索リンク", "Namen von Ausrüstungsplätzen (optional), Namen von Gegenständen und Suchlinks für die Eorzea-Datenbank", "Noms des emplacements d'équipement (facultatif), noms d'objets et liens de recherche dans la base de données d'Éorzéa", "장비 슬롯명(선택), 아이템명, 에오르제아 데이터베이스 검색링크", "装备槽名称（可选）、物品名称和艾欧泽亚数据库搜索链接", "装备槽名称（可选）、物品名称和艾欧泽亚数据库搜索链接"),
        R("Failed to apply icon: {0}", "アイコンを適用できませんでした: {0}", "Symbol konnte nicht angewendet werden: {0}", "Échec de l'application de l'icône : {0}", "아이콘 적용 실패: {0}", "未能应用图标：{0}", "未能套用圖示：{0}"),
        R("Failed to capture the game window.", "ゲームウィンドウのキャプチャに失敗しました。", "Das Spielfenster konnte nicht erfasst werden.", "Échec de la capture de la fenêtre de jeu.", "게임창 캡처에 실패했습니다.", "无法捕获游戏窗口。", "無法捕獲遊戲視窗。"),
        R("Failed to load character data", "キャラクターデータの読み込みに失敗しました", "Zeichendaten konnten nicht geladen werden", "Échec du chargement des données de caractères", "캐릭터 데이터를 불러오지 못했습니다.", "加载角色数据失败", "載入角色資料失敗"),
        R("Failed to read the captured image pixels.", "キャプチャされた画像ピクセルの読み取りに失敗しました。", "Die erfassten Bildpixel konnten nicht gelesen werden.", "Échec de la lecture des pixels de l'image capturée.", "캡처된 이미지 픽셀을 읽지 못했습니다.", "无法读取捕获的图像像素。", "無法讀取捕獲的影像像素。"),
        R("Feet", "足", "Füße", "Pieds", "발", "脚", "腳"),
        R("Glamour Saver Settings###GlamourSaverSettings", "グラマーセーバー設定###GlamourSaverSettings", "Glamour Saver-Einstellungen###GlamourSaverSettings", "Paramètres de l'économiseur de glamour###GlamourSaverSettings", "글래머 세이버 설정###GlamourSaverSettings", "魅力保护程序设置###GlamourSaverSettings", "魅力保護程式設定###GlamourSaverSettings"),
        R("Glamour Saver: Webhook connection test succeeded.", "Glamour Saver: Webhook 接続テストに成功しました。", "Glamour Saver: Webhook-Verbindungstest erfolgreich.", "Glamour Saver : le test de connexion Webhook a réussi.", "Glamour Saver: 웹훅 연결 테스트에 성공했습니다.", "Glamour Saver：Webhook 连接测试成功。", "Glamour Saver：Webhook 連線測試成功。"),
        R("Hands", "手", "Hände", "Mains", "손", "手", "手"),
        R("Head", "頭", "Kopf", "Tête", "머리", "头", "頭"),
        R("If you enjoy Glamour Saver, you can optionally support development through Ko-fi. All features remain available whether or not you contribute.", "Glamour Saver を気に入っている場合は、オプションで Ko-fi を通じて開発をサポートできます。貢献するかどうかに関係なく、すべての機能を引き続き利用できます。", "Wenn Ihnen Glamour Saver gefällt, können Sie optional die Entwicklung über Ko-fi unterstützen. Alle Funktionen bleiben verfügbar, unabhängig davon, ob Sie einen Beitrag leisten oder nicht.", "Si vous appréciez Glamour Saver, vous pouvez éventuellement prendre en charge le développement via Ko-fi. Toutes les fonctionnalités restent disponibles, que vous contribuiez ou non.", "Glamour Saver를 즐기신다면 선택적으로 Ko-fi를 통해 개발을 지원할 수 있습니다. 귀하의 기여 여부에 관계없이 모든 기능을 계속 사용할 수 있습니다.", "如果您喜欢 Glamour Saver，您可以选择通过 Ko-fi 支持开发。无论您是否贡献，所有功能都仍然可用。", "如果您喜欢 Glamour Saver，您可以选择通过 Ko-fi 支持开发。无论您是否贡献，所有功能都仍然可用。"),
        R("Include equipment slot names", "機器スロット名を含める", "Geben Sie die Namen der Ausrüstungsplätze an", "Inclure les noms des emplacements d'équipement", "장비 슬롯 이름 포함", "包含设备槽位名称", "包含設備插槽名稱"),
        R("Inspect agent was not found.", "検査エージェントが見つかりませんでした。", "Der Inspect-Agent wurde nicht gefunden.", "L'agent d'inspection n'a pas été trouvé.", "검사 에이전트를 찾을 수 없습니다.", "未找到检查代理。", "未找到檢查代理。"),
        R("Left Ring", "左リング", "Linker Ring", "Anneau gauche", "왼쪽 링", "左环", "左環"),
        R("Legs", "脚", "Beine", "Jambes", "다리", "腿", "腿"),
        R("Loading character data", "キャラクターデータの読み込み", "Charakterdaten werden geladen", "Chargement des données de caractères", "문자 데이터 로드", "加载字符数据", "載入字元數據"),
        R("Loading...", "読み込み中...", "Laden...", "Chargement...", "로드 중...", "正在加载...", "正在加載..."),
        R("Main Hand", "メインハンド", "Haupthand", "Main principale", "주요 손", "主手", "主手"),
        R("Necklace", "ネックレス", "Halskette", "Collier", "목걸이", "项链", "項鍊"),
        R("No equipment could be read.", "機器を読み取れませんでした。", "Es konnten keine Geräte ausgelesen werden.", "Aucun équipement n'a pu être lu.", "장비를 읽을 수 없습니다.", "无法读取任何设备。", "無法讀取任何設備。"),
        R("Off Hand", "オフハンド", "Aus der Hand", "Hors main", "오프 핸드", "副手", "副手"),
        R("Only https://discord.com/api/webhooks/... URLs are accepted.", "https://discord.com/api/webhooks/... URL のみが受け入れられます。", "Es werden nur https://discord.com/api/webhooks/...-URLs akzeptiert.", "Seules les URL https://discord.com/api/webhooks/... sont acceptées.", "https://discord.com/api/webhooks/... URL만 허용됩니다.", "仅接受 https://discord.com/api/webhooks/... URL。", "僅接受 https://discord.com/api/webhooks/... URL。"),
        R("Open the Examine window before changing the button position.", "ボタンの位置を変更する前に検査ウィンドウを開いてください。", "Öffnen Sie das Fenster „Untersuchen“, bevor Sie die Position der Schaltfläche ändern.", "Ouvrez la fenêtre Examiner avant de modifier la position du bouton.", "버튼 위치를 변경하기 전에 검사 창을 엽니다.", "在更改按钮位置之前打开“检查”窗口。", "在更改按钮位置之前打开“检查”窗口。"),
        R("Open the Glamour Saver Discord Webhook settings.", "Glamour Saver Discord Webhook 設定を開きます。", "Öffnen Sie die Glamour Saver Discord Webhook-Einstellungen.", "Ouvrez les paramètres du Webhook Glamour Saver Discord.", "Glamour Saver Discord 웹훅 설정을 엽니다.", "打开 Glamour Saver Discord Webhook 设置。", "開啟 Glamour Saver Discord Webhook 設定。"),
        R("Opened Roxyz0501's Ko-fi page in your browser.", "ブラウザで Roxyz0501 の Ko-fi ページを開きました。", "Die Ko-Fi-Seite von Roxyz0501 in Ihrem Browser geöffnet.", "Ouverture de la page Ko-fi de Roxyz0501 dans votre navigateur.", "브라우저에서 Roxyz0501의 Ko-fi 페이지를 열었습니다.", "在浏览器中打开 Roxyz0501 的 Ko-fi 页面。", "在瀏覽器中開啟 Roxyz0501 的 Ko-fi 頁面。"),
        R("PNG crop of the Examine window", "検査ウィンドウの PNG トリミング", "PNG-Ausschnitt des Untersuchungsfensters", "Recadrage PNG de la fenêtre Examiner", "검사 창의 PNG 자르기", "检查窗口的 PNG 裁剪", "檢查視窗的 PNG 裁剪"),
        R("Position editing was canceled because the Examine window was closed.", "検査ウィンドウが閉じられたため、位置の編集がキャンセルされました。", "Die Positionsbearbeitung wurde abgebrochen, da das Fenster „Untersuchen“ geschlossen wurde.", "La modification de position a été annulée car la fenêtre Examiner était fermée.", "검토 창이 닫혀서 위치 편집이 취소되었습니다.", "由于“检查”窗口已关闭，位置编辑被取消。", "由於「檢查」視窗已關閉，位置編輯被取消。"),
        R("Position editing was canceled.", "位置編集がキャンセルされました。", "Die Positionsbearbeitung wurde abgebrochen.", "La modification de position a été annulée.", "위치 수정이 취소되었습니다.", "位置编辑已取消。", "位置編輯已取消。"),
        R("Post failed: {0}", "投稿に失敗しました: {0}", "Beitrag fehlgeschlagen: {0}", "Échec de la publication : {0}", "게시 실패: {0}", "发布失败：{0}", "發布失敗：{0}"),
        R("Posted {0}'s glamour.", "{0}の魅力を投稿しました。", "Hat den Glamour von {0} gepostet.", "J'ai publié le glamour de {0}.", "{0}의 매력을 게시했습니다.", "发布了{0}的魅力。", "發佈了{0}的魅力。"),
        R("Recipient: Roxyz0501", "受信者: Roxyz0501", "Empfänger: Roxyz0501", "Destinataire : Roxyz0501", "수신자: Roxyz0501", "收件人：Roxyz0501", "收件人：Roxyz0501"),
        R("Reset to default", "デフォルトにリセットする", "Auf Standard zurücksetzen", "Réinitialiser aux valeurs par défaut", "기본값으로 재설정", "重置为默认值", "重設為預設值"),
        R("Right Ring", "右リング", "Rechter Ring", "Bague droite", "오른쪽 링", "右环", "右環"),
        R("Save position", "位置を保存", "Position speichern", "Enregistrer la position", "위치 저장", "保存位置", "儲存位置"),
        R("SendDiscord button position", "SendDiscordボタンの位置", "Position der SendDiscord-Schaltfläche", "Position du bouton SendDiscord", "SendDiscord 버튼 위치", "SendDiscord 按钮位置", "SendDiscord 按鈕位置"),
        R("Sending to Discord...", "Discordに送信中...", "An Discord senden...", "Envoi sur Discord...", "디스코드로 보내는 중...", "正在发送至 Discord...", "正在發送至 Discord..."),
        R("Sending...", "送信中...", "Senden...", "Envoi...", "보내는 중...", "正在发送...", "正在發送..."),
        R("Settings###SettingsTab", "設定###SettingsTab", "Einstellungen###SettingsTab", "Paramètres###SettingsTab", "설정###SettingsTab", "设置###SettingsTab", "設定###SettingsTab"),
        R("Soul Crystal", "ソウルクリスタル", "Seelenkristall", "Cristal d'âme", "소울 크리스탈", "灵魂水晶", "靈魂水晶"),
        R("Support Roxyz0501 on Ko-fi", "Ko-fi で Roxyz0501 をサポートする", "Unterstützen Sie Roxyz0501 auf Ko-fi", "Supporte Roxyz0501 sur Ko-fi", "Ko-fi에서 Roxyz0501 지원", "在 Ko-fi 上支持 Roxyz0501", "在 Ko-fi 上支援 Roxyz0501"),
        R("Support Roxyz0501's development", "Roxyz0501 の開発をサポートします", "Unterstützen Sie die Entwicklung von Roxyz0501", "Soutenir le développement de Roxyz0501", "Roxyz0501의 개발을 지원하세요", "支持Roxyz0501的开发", "支援Roxyz0501的開發"),
        R("Target character name", "対象キャラクター名", "Name des Zielcharakters", "Nom du personnage cible", "대상 캐릭터 이름", "目标角色名称", "目標角色名稱"),
        R("Test Webhook connection", "Webhook接続のテスト", "Testen Sie die Webhook-Verbindung", "Tester la connexion Webhook", "웹훅 연결 테스트", "测试 Webhook 连接", "測試 Webhook 連接"),
        R("Testing connection...", "接続をテストしています...", "Verbindung wird getestet...", "Test de connexion...", "연결 테스트 중...", "测试连接...", "測試連接..."),
        R("The browser could not be opened. Open this URL manually: {0}", "ブラウザを開けませんでした。この URL を手動で開きます: {0}", "Der Browser konnte nicht geöffnet werden. Öffnen Sie diese URL manuell: {0}", "Le navigateur n'a pas pu être ouvert. Ouvrez cette URL manuellement : {0}", "브라우저를 열 수 없습니다. 다음 URL을 수동으로 엽니다: {0}", "浏览器无法打开。手动打开此网址：{0}", "瀏覽器無法開啟。手動開啟此網址：{0}"),
        R("The bundled icon could not be loaded.", "同梱されているアイコンを読み込めませんでした。", "Das gebündelte Symbol konnte nicht geladen werden.", "L'icône fournie n'a pas pu être chargée.", "번들 아이콘을 로드할 수 없습니다.", "无法加载捆绑的图标。", "無法載入捆綁的圖示。"),
        R("The button can be dragged only while position editing is active.", "ボタンは、位置編集がアクティブな場合にのみドラッグできます。", "Die Schaltfläche kann nur gezogen werden, während die Positionsbearbeitung aktiv ist.", "Le bouton ne peut être déplacé que lorsque l'édition de position est active.", "위치 편집이 활성화된 동안에만 버튼을 끌 수 있습니다.", "仅当位置编辑处于活动状态时才能拖动该按钮。", "僅當位置編輯處於活動狀態時才能拖曳該按鈕。"),
        R("The character's glamour could not be read.", "キャラクターの魅力が読み取れなかった。", "Der Glamour der Figur war nicht zu erkennen.", "Le glamour du personnage ne pouvait pas être lu.", "캐릭터의 매력을 읽을 수 없었습니다.", "无法解读角色的魅力。", "無法解讀角色的魅力。"),
        R("The Discord connection timed out.", "Discord 接続がタイムアウトしました。", "Bei der Discord-Verbindung ist eine Zeitüberschreitung aufgetreten.", "La connexion Discord a expiré.", "Discord 연결 시간이 초과되었습니다.", "Discord 连接超时。", "Discord 連線逾時。"),
        R("The initial language follows the game client once. Your selection is then kept until you change it.", "初期言語はゲーム クライアントに一度続きます。選択内容は、変更するまで保持されます。", "Die Ausgangssprache folgt einmal dem Spielclient. Ihre Auswahl bleibt dann solange erhalten, bis Sie sie ändern.", "La langue initiale suit le client du jeu une fois. Votre sélection est alors conservée jusqu'à ce que vous la modifiiez.", "초기 언어는 게임 클라이언트를 한 번 따릅니다. 선택한 내용은 변경할 때까지 유지됩니다.", "初始语言跟随游戏客户端一次。然后，您的选择将被保留，直到您更改为止。", "初始語言跟隨遊戲客戶端一次。然後，您的選擇將被保留，直到您更改為止。"),
        R("The preview was reset to the default position. Press Save position to confirm.", "プレビューがデフォルトの位置にリセットされました。位置を保存を押して確定します。", "Die Vorschau wurde auf die Standardposition zurückgesetzt. Drücken Sie zur Bestätigung Position speichern.", "L'aperçu a été réinitialisé à la position par défaut. Appuyez sur Enregistrer la position pour confirmer.", "미리보기가 기본 위치로 재설정되었습니다. 위치 저장을 눌러 확인하세요.", "预览已重置为默认位置。按保存位置进行确认。", "預覽已重設為預設位置。按保存位置進行確認。"),
        R("The saved Webhook URL was deleted.", "保存されていたWebhook URLが削除されました。", "Die gespeicherte Webhook-URL wurde gelöscht.", "L'URL du Webhook enregistrée a été supprimée.", "저장된 웹훅 URL이 삭제되었습니다.", "保存的 Webhook URL 已删除。", "儲存的 Webhook URL 已刪除。"),
        R("The SendDiscord button position was saved.", "SendDiscordボタンの位置が保存されました。", "Die Position der SendDiscord-Schaltfläche wurde gespeichert.", "La position du bouton SendDiscord a été enregistrée.", "SendDiscord 버튼 위치가 저장되었습니다.", "SendDiscord 按钮位置已保存。", "SendDiscord 按鈕位置已儲存。"),
        R("The SendDiscord button was reset to its default position.", "SendDiscord ボタンがデフォルトの位置にリセットされました。", "Die SendDiscord-Schaltfläche wurde auf ihre Standardposition zurückgesetzt.", "Le bouton SendDiscord a été réinitialisé à sa position par défaut.", "SendDiscord 버튼이 기본 위치로 재설정되었습니다.", "SendDiscord 按钮已重置为默认位置。", "SendDiscord 按鈕已重設為預設位置。"),
        R("The URL is stored as plain text in the Dalamud Glamour Saver config, but is never written to logs or message content.", "URL は Dalamud Glamour Saver 設定にプレーン テキストとして保存されますが、ログやメッセージ コンテンツに書き込まれることはありません。", "Die URL wird als Klartext in der Dalamud Glamour Saver-Konfiguration gespeichert, aber niemals in Protokolle oder Nachrichteninhalte geschrieben.", "L'URL est stockée sous forme de texte brut dans la configuration de Dalamud Glamour Saver, mais n'est jamais écrite dans les journaux ou dans le contenu du message.", "URL은 Dalamud Glamour Saver 구성에 일반 텍스트로 저장되지만 로그나 메시지 콘텐츠에는 기록되지 않습니다.", "URL 以纯文本形式存储在 Dalamud Glamour Saver 配置中，但绝不会写入日志或消息内容。", "URL 以純文字儲存在 Dalamud Glamour Saver 配置中，但絕對不會寫入日誌或訊息內容。"),
        R("Usage: Right-click another character, open Examine, then press the displayed SendDiscord button.", "使用方法: 別のキャラクターを右クリックして「検査」を開き、表示される「SendDiscord」ボタンを押します。", "Verwendung: Klicken Sie mit der rechten Maustaste auf einen anderen Charakter, öffnen Sie „Untersuchen“ und klicken Sie dann auf die angezeigte Schaltfläche „Discord senden“.", "Utilisation : cliquez avec le bouton droit sur un autre personnage, ouvrez Examiner, puis appuyez sur le bouton SendDiscord affiché.", "사용법: 다른 캐릭터를 마우스 오른쪽 버튼으로 클릭하고 Examine을 연 다음 표시된 SendDiscord 버튼을 누릅니다.", "用法：右键单击另一个字符，打开“检查”，然后按显示的“SendDiscord”按钮。", "用法：右鍵單擊另一個字符，打開“檢查”，然後按顯示的“SendDiscord”按鈕。"),
        R("Waiting for character data", "キャラクターデータ待ち", "Warten auf Charakterdaten", "En attente des données du personnage", "문자 데이터를 기다리는 중", "等待字符数据", "等待字元數據"),
        R("Window detected: CharacterInspect ({0}x{1}) / {2}", "検出されたウィンドウ: CharacterInspect ({0}x{1}) / {2}", "Fenster erkannt: CharacterInspect ({0}x{1}) / {2}", "Fenêtre détectée : CharacterInspect ({0}x{1}) / {2}", "창 감지됨: CharacterInspect({0}x{1}) / {2}", "检测到窗口：CharacterInspect ({0}x{1}) / {2}", "偵測到視窗：CharacterInspect ({0}x{1}) / {2}"),
        R("Window detection: CharacterInspect is not currently visible.", "ウィンドウ検出: CharacterInspect は現在表示されません。", "Fenstererkennung: CharacterInspect ist derzeit nicht sichtbar.", "Détection de fenêtre : CharacterInspect n'est pas visible actuellement.", "창 감지: CharacterInspect는 현재 표시되지 않습니다.", "窗口检测：CharacterInspect 当前不可见。", "視窗偵測：CharacterInspect 目前不可見。"),
        ];
        return rows.ToDictionary(row => row[0], row => row, StringComparer.Ordinal);
    }
}
