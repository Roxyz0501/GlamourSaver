# Public release checklist

## 現在ローカルで準備済み

- [x] Author/Authorsを `Roxyz0501` に統一
- [x] 旧作者表記をソース・文書・manifestから除去
- [x] 公開用READMEを作成
- [x] MIT LICENSEを追加
- [x] 第三者通知とImageSharp同梱ライセンスを追加
- [x] 共通repo.jsonへ渡す1エントリのテンプレートを作成
- [x] Webhook送信内容と明示操作をUI/READMEへ表示
- [x] Webhook URL削除ボタンを追加
- [x] Webhook URLを通信例外・ログへ含めない処理を追加
- [x] Ko-fiのみを使用する任意支援タブを追加
- [x] アンロード時の通信キャンセル、HttpClient解放、イベント解除を実装
- [x] SDK、NuGet依存、バージョンを固定
- [x] `bin/`、`obj/`、ログ、ローカル設定を `.gitignore` へ追加
- [x] `<InternalName>-<AssemblyVersion>.zip` をReleaseビルドで生成するターゲットを追加
- [x] 英語READMEと日本語READMEを相互リンク
- [x] 初回のみクライアント言語からEnglish/日本語を確定し、以後は保存値を維持
- [x] UI、通知、エラー、ツールチップ、支援タブ、Discord投稿ラベルを英日対応
- [x] `clean → restore --locked-mode → Release build --no-restore` が警告・エラーなしで成功
- [x] ソースとRelease ZIPの秘密情報パターン検出0件
- [x] Release ZIP内のローカル絶対パス検出0件
- [x] 旧作者名、旧内部名、未承認の支援先表記の検出0件

## 検証済みRelease成果物

- ファイル名: `GlamourSaver-0.5.1.0.zip`
- SHA-256: `8E8E79917D1F08814335A14E88924844DEAC86B14B84C2A409C6773A156A2F12`
- ZIPエントリ数: 10
- `latest.zip` と版番号付きZIPのSHA-256一致を確認

## 人間による実機確認が必要

- [ ] FFXIV上でプラグインをロード・アンロード・再ロードして例外が出ない
- [ ] 「調べる」画面の表示中だけ `SendDiscord` が表示される
- [ ] 位置変更、ドラッグ、保存、キャンセル、画面閉鎖時キャンセルが仕様通り動く
- [ ] `SendDiscord` を押した場合だけDiscordへ投稿される
- [ ] 投稿画像にDalamudオーバーレイや意図しない個人情報が写っていない
- [ ] キャラクター名、全装備名、リンクが正しい
- [ ] Webhook接続テストとアイコン適用が明示操作時だけ動く
- [ ] 保存済みWebhook URL削除後に設定ファイルから値が消える
- [ ] 無効なWebhook、タイムアウト、Discordエラー時にURLがDalamudログへ出ない
- [ ] 支援タブの通常・ホバー・選択状態が読みやすく、スタイルが他タブへ漏れない
- [ ] Ko-fiボタンを押した場合だけ `https://ko-fi.com/roxyz0501` が開く
- [ ] 100%、高DPI、異なるゲーム解像度で設定UIとボタン位置を確認

## 個別GitHubリポジトリ公開前のTODO

- [ ] Roxyz0501管理の `Glamour Saver` 用公開GitHubリポジトリを新規作成する
- [ ] manifestへ実在する `RepoUrl` を追加する
- [ ] 個別リポジトリでGit履歴全体のsecret scanを実行する
- [ ] Roxyz0501がソース、AI生成物、ライセンスを最終承認する
- [ ] 実機テスト結果と既知の不具合をREADME/Release notesへ反映する
- [ ] `GlamourSaver-0.5.1.0.zip` をGitHub Releaseへ添付する
- [ ] git init、GitHubリポジトリ作成、push、Release公開は、別途明示承認を受けてから行う

## 共通カスタムリポジトリ統合時のTODO

- [ ] `CUSTOM_REPOSITORY_ENTRY.template.json` の `RepoUrl` に個別ソースリポジトリURLを注入する
- [ ] `DownloadLinkInstall` と `DownloadLinkUpdate` に個別GitHub Releaseの `GlamourSaver-0.5.1.0.zip` URLを注入する
- [ ] `LastUpdate` に公開時のUNIX時刻を注入する
- [ ] 共通repo.jsonの配列へ1エントリとして統合する
- [ ] `AssemblyVersion` とZIP内manifest/DLLのバージョンが一致することを検証する
- [ ] 共通repo.jsonは認証不要のHTTPS GETで取得可能にする

## 更新手順

1. `GlamourSaver.csproj` と `GlamourSaver.json` の4桁バージョンを同時に上げる。
2. `CUSTOM_REPOSITORY_ENTRY.template.json` の `AssemblyVersion` と必要に応じて文言を更新する。
3. `dotnet restore GlamourSaver/GlamourSaver.csproj --locked-mode` を実行する。
4. `dotnet build GlamourSaver/GlamourSaver.csproj -c Release --no-restore` を実行する。
5. `GlamourSaver/bin/Release/GlamourSaver/GlamourSaver-<version>.zip` の内容、ハッシュ、秘密情報を検査する。
6. 人間がFFXIVとDiscordで実機テストする。
7. 個別GitHubリポジトリのバージョンタグReleaseへZIPを添付する。
8. ZIPの公開完了後、共通repo.jsonの当該エントリを最後に更新する。
