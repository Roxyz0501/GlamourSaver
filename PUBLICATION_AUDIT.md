# Publication audit

監査日: 2026-08-18

## タスク開始時の区分

区分: **新規独立プラグイン**

この区分はタスク開始時のユーザー判断で確定しています。以後、類似機能、公開API、アルゴリズム、参考実装の存在を理由に再分類しません。本プロジェクトは個別GitHubリポジトリと、Roxyz0501の共通Dalamudカスタムリポジトリ向けに公開準備を進めます。

## コード・データ・アセットの由来

- プラグイン固有コード: 本タスクで新規作成。
- Dalamud、Lumina、FFXIVClientStructs、InteropGenerator.Runtime: 公開API・ゲーム連携構造体の参照。Release ZIPには非同梱。
- `SixLabors.ImageSharp 3.1.12`: NuGet直接依存。PNGエンコードに使用し、DLLをRelease ZIPへ同梱。パッケージ同梱のSplit Licenseを `licenses/` へ収録。
- アイコン: OpenAI画像生成機能で本プロジェクト向けに新規生成。外部画像の入力・転用なし。
  - SHA-256: `55D8261A1E51BE371EC12D9729DB747997F449839FF83ABE06A9365395E37DAE`
  - 生成プロンプト要旨: FFXIV DalamudプラグインとDiscord Webhook向けの、衣類ハンガー、結晶、送信モチーフを組み合わせた透明背景の正方形ファンタジーUIアイコン。文字、商標、透かしなし。
- FFXIVゲームデータ: 実行時にDalamud/Lumina APIからアイテム名を参照。データファイル自体は同梱しない。
- キャラクター情報・画像: 実行時にユーザーの明示操作で取得しDiscordへ送信。ソース・成果物へ実データを同梱しない。

## AI利用

コード、文書、公開準備、およびアイコン生成にOpenAI Codex/ImageGenを使用しました。人間によるレビューと実機確認は未完了です。公式Dalamudリポジトリへの提出を検討する場合は、その時点のAI利用ポリシーを再確認してください。

## 秘密情報監査

ソース、文書、manifest、NuGet lock file、Release ZIPについて、Webhook実値、APIキー、トークン、Cookie、Authorizationヘッダー、実キャラクター名の混入を検索しました。検出は0件でした。Release ZIP内のローカル絶対パスも0件でした。

Webhook URLはDalamudのローカル設定JSONにのみ保存し、プロジェクトやRelease ZIPへは含めません。HttpClient由来の例外が秘密URLを含む可能性を避けるため、ログへ渡す前に秘密値を含まないエラーへ置換しています。

## ライセンス方針

- Glamour Saver本体: MIT License、Copyright (c) 2026 Roxyz0501
- SixLabors.ImageSharp: パッケージ同梱のSix Labors Split License 1.0。オープンソース配布条件に基づきApache License 2.0の資格を参照。
- ランタイム提供API: DLLを再配布しない。ビルド依存の `Dalamud.NET.Sdk 15.0.0` はMIT。

これは法的助言ではありません。公開主体であるRoxyz0501が最終的なライセンス適合性を確認してください。
