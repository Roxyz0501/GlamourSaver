# Glamour Saver — persistence repair, 2026-10-22

Release validation for 1.0.0.2. No installed DLL, production config, or shared template was modified.

Commands below run from the repository root.

## Changes

- `ConfigurationStore.cs`: one lock-protected writer; UI mutations update a revision in memory; one monotonic 60-second framework schedule serializes only dirty settings. Equal-to-last-save values produce zero writes. Logout, normal game-shutdown token, Dalamud-unload token and Dispose flush pending edits. Duplicate flushes do not rewrite.
- Existing `ConfigFile` JSON is read directly with safe JSON deserialization; legacy `$type` metadata does not activate types. Version/language/offset/Webhook values remain compatible. Configs above the supported schema or with invalid required fields are blocked rather than overwritten. Migration runs in memory and persists at a flush boundary.
- Validated temporary file, durable flush and atomic replacement. Previous valid main moves into a monotonically numbered backup; retain three valid generations after commit. Failed replacement does not rotate them. Damaged originals are retained separately; invalid backups are skipped and preserved. Cleanup failure may temporarily leave additional generations, with pruning retried on the next successful changed save.
- Main file wins when valid. Missing/corrupt main restores the newest valid backup and reports recovery; no usable copy or inaccessible storage blocks saving and asks for restoration/reload. Failure details never include the Webhook URL or raw serialized data.
- Three consecutive save failures stop automatic attempts; pending state remains in memory and settings offers an explicit retry. Shutdown does not restart an already stopped retry loop. A failure at shutdown emits one generic log message without exception contents. Forced termination can lose the unsaved interval.
- `PluginUi.cs`: field edits and language changes are atomic in-memory mutations. Explicit `Save position` still flushes immediately. Deleting the current Webhook value follows normal autosave, with an explicit warning about old backups. README explains removal of all matching secret-bearing files.
- `OwnedOperation.cs`: one bounded worker shared by posting/testing/icon upload; no accumulating request queue. Dispose stops acceptance, cancels and joins before HttpClient disposal. No worker waits for the game thread. Chat completion callbacks are drained by Draw and discarded on shutdown.
- `ScreenCaptureService.cs`: GDI pixel capture remains on the invoking UI thread, PNG compression moves to the tracked worker with cancellation. Images remain memory-only; explicit Discord actions and their payload are retained.
- New notices translated into all seven languages. Existing standard tabs remain byte-identical to the canonical template.

## Tests and measurements

Commands from the canonical task directory:

```powershell
dotnet run --project PersistenceSmoke -c Release
dotnet run --project LocalizationSmoke -c Release --no-restore
dotnet build GlamourSaver/GlamourSaver.csproj -c Release --no-restore -t:Rebuild
```

Persistence smoke: **40 assertions passed**. Tiny synthetic JSON and isolated `.scratch-<GUID>` directories only; temporary test directories removed in `finally`.

| Scenario | Committed writes | Logical payload bytes |
| --- | ---: | ---: |
| 600 field edits before the first 60-second boundary | 0 before boundary; 1 at boundary | 38 |
| 480 unchanged minute intervals (8 hours simulated) | 0 additional | 0 additional |
| Edit then return to saved value | 0 additional | 0 additional |
| Initial save, logout edit and Dispose edit combined | 3 | 114 |
| Access denial before opening a temp, then manual recovery | 2 including seed/retry; 3 failed attempts | 72 |
| Injected disk-full error after temp write, then manual recovery | 2 including seed/retry; 3 failed attempts | 180 including failed temps |
| Injected replacement failure, then manual recovery | 2 including seed/retry; 3 failed attempts | 180 including failed temps |

Additional checks: actual Windows exclusive file lock and recovery; three previous generations and exact values; corrupt-main quarantine; skipping corrupt newest backup; missing-main recovery; healthy-main precedence; unrecoverable config preservation; existing backup bytes unchanged after failed replacement; change during save remains dirty; concurrent flush single writer; concurrent edits/flush agree; no edits/writes accepted after Dispose; monotonic deadline spanning midnight; cancel/drain and rejection of a second operation.

Localization smoke also passed: old numeric language values and `$type` JSON fixture migrate and reload without resetting other values; valid restart writes zero; seven-language catalogue consistency; canonical tab source byte comparison/order; synthetic 2x2 PNG encoding and cancellation.

Counts are application commits and UTF-8 bytes passed successfully to temp-file `Write`, including failed later flush/replacement. They exclude filesystem metadata, OS buffering, NTFS amplification and SSD NAND writes. The synthetic 38-byte config is a counting fixture, not a measurement of a user's config. Midnight uses simulated monotonic time; no calendar-based setting filenames exist.

## Applicability and limits

No character/equipment/screenshot history is persisted by Glamour Saver. Date-partitioned history, late history events, history migration, all-date lookup and HistoryFiles tab are therefore not applicable. `StandardHistoryFilesTab.cs` was not copied and no empty history tab was added, as permitted by PERSISTENCE_SPEC.md. Shared templates/specifications were left intact. No image/preset export file flow exists to migrate; explicit position confirmation remains immediate.

Actual FFXIV UI, GDI capture, fonts, Discord requests, host shutdown callback ordering, real power loss, real disk exhaustion, OS File I/O traces and SSD physical writes remain unverified. Test disk-full and rename errors are injected. Normal successful file I/O and Windows lock behavior are exercised on local synthetic files.

## Local artifacts

- Release build: **0 warnings / 0 errors** (including clean Rebuild).
- DLL: `GlamourSaver/bin/Release/GlamourSaver.dll`.
- ZIP: `GlamourSaver/bin/Release/GlamourSaver/GlamourSaver-1.0.0.2.zip` — 1,771,604 bytes.
- SHA-256: `17697A84599D97346127E615E88926E8333949DE364354F3E7B6D150BB9FC097`.
- Package: 10 allowlisted files (plugin DLL/deps/manifest, ImageSharp DLL, icon, English/Japanese README, LICENSE, third-party notices/license); no tests, production settings, PDBs or scratch files. Manifest is 1.0.0.2 / Roxyz0501 / API 15 with existing RepoUrl and IconUrl.
- `CUSTOM_REPOSITORY_ENTRY.template.json` provides the shared custom-repository entry for 1.0.0.2.
