# Glamour Saver

[English](README.md)

`Glamour Saver` は、FINAL FANTASY XIVで他キャラクターの「調べる」画面に `SendDiscord` ボタンを追加し、表示中のコーディネートを任意のDiscord Webhookへ保存するDalamudプラグインです。

作成者: `Roxyz0501`

## 主な機能

- 「調べる」画面が表示されている間、その横へ `SendDiscord` ボタンを表示
- 「調べる」画面だけをPNG画像として切り出して投稿
- 対象キャラクター名、見た目装備名、装備部位、エオルゼアデータベース検索リンクを投稿
- 実際の画面上のボタンをドラッグして表示位置を調整
- Webhook接続テストと、同梱アイコンのWebhookへの適用
- 設定UI、通知、エラー、ツールチップ、支援タブ、Discord投稿ラベルの英語・日本語対応
- Roxyz0501への任意のKo-fi支援タブ

## 必要環境と依存関係

- Windows版FINAL FANTASY XIV
- XIVLauncherとDalamud
- .NET 10対応のDalamud API Level 15環境
- 依存プラグイン: なし
- 同梱ライブラリ: `SixLabors.ImageSharp 3.1.12`

## インストール方法

### 共通カスタムリポジトリ

Dalamud設定の「試験的機能」から下記のRoxyz0501共通カスタムリポジトリURLを追加し、プラグインインストーラーで `Glamour Saver` を選択します。

`https://raw.githubusercontent.com/Roxyz0501/DalamudPluginRepo/main/repo.json`

### 開発版をローカル導入

1. Releaseビルドを実行します。
2. Dalamudの開発プラグイン設定へ `GlamourSaver/bin/Release/GlamourSaver.dll` を追加します。
3. 開発プラグイン一覧から `Glamour Saver` を有効にします。

## 利用方法

1. `/glamoursaver` を実行し、Discord Webhook URLを設定します。
2. 他キャラクターを右クリックして「調べる」を開きます。
3. 必要に応じて「コーディネート」を開き、装備データの読み込みを待ちます。
4. 「調べる」画面の横に表示された `SendDiscord` を押します。

自動送信はありません。ユーザーが `SendDiscord` を明示的に押した場合だけ投稿します。

## コマンド

- `/glamoursaver` — 設定画面を開きます。

## 設定

- `表示言語`: `English` または `日本語` を選びます。初回起動時だけ、日本語クライアントなら日本語、それ以外または検出不能ならEnglishを選択して保存します。その後、保存した選択をクライアント言語の再検出で上書きしません。
- `Discord Webhook URL`: Discordの `https://discord.com/api/webhooks/...` または旧 `discordapp.com` 形式を受け付けます。
- `装備部位名も投稿する`: 投稿する装備一覧へ部位名を付けます。
- `Webhook接続テスト`: 明示操作で固定のテストメッセージを1件投稿します。
- `作成したアイコンをWebhookへ適用`: 明示操作でWebhookのアバターを同梱アイコンへ変更します。
- `ボタンの位置を変更`: 「調べる」画面上のプレビューボタンをドラッグし、保存またはキャンセルします。
- `支援`: ボタンを押した場合だけRoxyz0501のKo-fiページを開きます。

## Discordへ送信する内容

`SendDiscord` を押すと、次の内容を送信します。

- 取得対象キャラクター名
- 「調べる」画面の矩形を切り出したPNG画像
- 各装備の部位名（設定で省略可能）
- 各装備のアイテム名
- アイテム名を検索語にしたエオルゼアデータベース検索URL
- 取得日時

接続テストでは固定のテスト文だけを送信します。Webhookアイコン適用では同梱アイコンをBase64化してDiscordへ送信します。

## 保存データとプライバシー

- 通信先はユーザーが設定したDiscord Webhookだけです。
- 装備リンクは投稿本文へ含めますが、プラグイン自体がLodestoneを取得することはありません。
- 支援タブは明示クリック時だけ `https://ko-fi.com/roxyz0501` を既定ブラウザで開きます。
- テレメトリ、アクセス解析、広告、自動投稿、バックグラウンド収集はありません。
- Webhook URLはDalamudが管理する `GlamourSaver` の設定JSONへ平文で保存されます。UIでは伏せ字表示し、ログ、例外、Discord本文、Release ZIPへ出力しません。
- 「保存済みWebhook URLを削除」で値を消せます。完全に削除する場合はプラグイン停止後にDalamudの設定JSONも削除してください。
- キャラクター名、装備、画像はローカルファイルへ保存せず、明示送信時にメモリ上で生成します。

Webhook URLは投稿権限を持つ秘密情報です。Issue、チャット、スクリーンショット、ログへ掲載しないでください。漏えい時はDiscord側でWebhookを削除・再作成してください。

## 既知の制限

- Windows GDIを使うため、Windows以外では動作しません。
- Character Inspectの範囲へ別オーバーレイが重なっていると画像へ写る場合があります。
- FFXIV、Dalamud、FFXIVClientStructsの更新で画面検出や装備取得が一時的に動かなくなる場合があります。
- データベースリンクはアイテム名による検索リンクで、個別ページへの直リンクではありません。
- Discordの文字数、アップロード容量、レート制限を受けます。
- キャラクター名と画像は個人情報・プライバシー情報になり得ます。送信先と共有範囲を確認してください。

## トラブルシューティング

- **ボタンが表示されない:** 他キャラクターの「調べる」画面が表示中で、プラグインが有効か確認してください。
- **「読み込み中」と表示される:** 調べるデータの読み込みを待ち、コーディネート画面を開いて再確認してください。
- **位置変更でエラーになる:** 「位置を保存」を押すまで「調べる」画面を閉じないでください。
- **Discordへ投稿できない:** Webhook URLを確認し、設定画面の接続テストとDiscord側のWebhook権限を確認してください。
- **表示言語が意図と違う:** `/glamoursaver` で `English` または `日本語` を選んでください。クライアント言語の検出は言語未設定時の1回だけです。
- **画像へ不要な表示が入る:** 「調べる」画面に重なるオーバーレイを移動または無効化してから送信してください。

## アンインストール

1. Dalamudのプラグインインストーラーで `Glamour Saver` を無効化・削除します。
2. 保存設定も消す場合は、Dalamudの設定ディレクトリにある `GlamourSaver` の設定JSONを削除します。
3. Webhookが不要ならDiscordのチャンネル設定から削除します。
4. 共通カスタムリポジトリ内の他プラグインも使わない場合だけ、そのリポジトリURLをDalamud設定から削除します。

## ビルドとパッケージ

```powershell
dotnet restore .\GlamourSaver\GlamourSaver.csproj --locked-mode
dotnet build .\GlamourSaver\GlamourSaver.csproj -c Release --no-restore
```

SDKは `global.json`、NuGet依存は `packages.lock.json` で固定しています。公開用ZIPは `GlamourSaver/bin/Release/GlamourSaver/GlamourSaver-0.5.0.0.zip` に生成されます。

## 任意支援

Roxyz0501の開発を任意で支援する場合は、[Ko-fi: Roxyz0501](https://ko-fi.com/roxyz0501) を利用できます。支援しない場合の機能制限はありません。

## AI利用

コード、文書、公開準備、アイコンの作成にはOpenAI Codexと画像生成機能を使用しました。公開前に人間によるコードレビュー、権利確認、FFXIV実機テスト、Discord実送信テストが必要です。

## ライセンス、第三者参照、帰属

Glamour Saver本体はMIT Licenseです。`LICENSE` を参照してください。

- `SixLabors.ImageSharp 3.1.12` をPNGエンコードに使用し、`THIRD_PARTY_NOTICES.md` と `licenses/SixLabors.ImageSharp-LICENSE.txt` の条件で再配布します。
- Dalamud、Dalamud.Bindings.ImGui、Lumina、FFXIVClientStructs、InteropGenerator.Runtimeはランタイム・ビルドAPI依存で、Release ZIPへ再配布しません。
- アイコンはOpenAI画像生成機能で本プロジェクト専用に作成し、他プラグインや素材集からコピーしていません。
- この独立プラグインは、別の第三者プラグインのソースコード、データ、アセット、IPC契約、実装を参照・再利用していません。

本ソフトウェアは無保証です。本プロジェクトはSquare Enix、XIVLauncher、Dalamud、Discord、Ko-fiと提携・承認関係にありません。
