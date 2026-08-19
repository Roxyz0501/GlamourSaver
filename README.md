# Glamour Saver

[日本語](README.ja.md)

Glamour Saver is a Dalamud plugin that adds a `SendDiscord` button beside another character's Examine window and lets you save the displayed glamour to a Discord Webhook.

Author: `Roxyz0501`

## Features

- Shows `SendDiscord` while the Character Inspect window is visible.
- Captures the Character Inspect window as a cropped PNG.
- Posts the target character name, appearance item names, equipment slots, and Eorzea Database search links.
- Lets you drag the real on-screen button to configure its position.
- Provides explicit actions to test the Webhook and apply the bundled icon to it.
- Supports English and Japanese throughout the settings UI, notifications, errors, tooltips, support tab, and Discord post labels.
- Includes an optional Ko-fi support tab for Roxyz0501.

## Requirements and dependencies

- Windows version of FINAL FANTASY XIV
- XIVLauncher and Dalamud
- Dalamud API Level 15 with .NET 10 support
- Required plugins: none
- Bundled library: `SixLabors.ImageSharp 3.1.12`

## Installation

### Shared custom repository

After publication, add the shared Roxyz0501 custom repository URL under Dalamud's Experimental settings, then install `Glamour Saver` from the plugin installer.

> The shared repository URL has not been assigned yet. Replace this notice with the real URL before publication.

### Local development build

1. Build the project in Release mode.
2. Add `GlamourSaver/bin/Release/GlamourSaver.dll` to Dalamud's development plugin locations.
3. Enable `Glamour Saver` under installed development plugins.

## Usage

1. Run `/glamoursaver` and configure a Discord Webhook URL.
2. Right-click another character and choose Examine.
3. Open the Coordinate/appearance view if needed and wait for the equipment data to load.
4. Press `SendDiscord` beside the Examine window.

Nothing is sent automatically. Posting occurs only after the user explicitly presses `SendDiscord`.

## Command

- `/glamoursaver` — Open the settings window.

## Settings

- `Display language`: Choose `English` or `日本語`. On the first launch only, Japanese is selected for a Japanese game client; all other or undetectable languages select English. The saved selection is never overwritten by later client-language detection.
- `Discord Webhook URL`: Accepts Discord `https://discord.com/api/webhooks/...` and legacy `discordapp.com` Webhook URLs.
- `Include equipment slot names`: Include slot labels in the posted equipment list.
- `Test Webhook connection`: Explicitly post one fixed test message.
- `Apply bundled icon to Webhook`: Explicitly replace the Webhook avatar with the bundled icon.
- `Change button position`: Drag the preview button beside the live Examine window, then save or cancel.
- `Support`: Open Roxyz0501's Ko-fi page only after pressing the button.

## Data sent to Discord

When `SendDiscord` is pressed, the plugin sends:

- Target character name
- PNG crop of the Examine window
- Equipment slot names, unless disabled
- Appearance item names
- Eorzea Database search URLs generated from the item names
- Capture date and time

The connection test sends only a fixed test message. Applying the Webhook icon sends the bundled icon as Base64 data.

## Storage and privacy

- Network requests are sent only to the user-configured Discord Webhook.
- Eorzea Database URLs are placed in the Discord message; the plugin does not fetch Lodestone pages.
- The support tab opens `https://ko-fi.com/roxyz0501` only after an explicit click.
- There is no telemetry, analytics, advertising, automatic posting, or background data collection.
- The Webhook URL is stored as plain text in the Dalamud-managed `GlamourSaver` configuration JSON. It is masked in the UI and is not included in logs, exception messages, Discord message content, or the Release ZIP.
- Use `Delete saved Webhook URL` to clear the value. For full removal, disable the plugin and delete its Dalamud configuration JSON.
- Character names, equipment, and screenshots are not written to local files; they are generated in memory for the explicit Discord request.

A Discord Webhook URL is a secret that grants posting access. Do not include it in issues, chat, screenshots, or logs. Delete and recreate the Webhook in Discord if it is exposed.

## Known limitations

- Screen capture uses Windows GDI and is not supported on non-Windows systems.
- Any overlay visibly covering the Character Inspect rectangle may appear in the captured image.
- FFXIV, Dalamud, or FFXIVClientStructs updates can temporarily break window detection or equipment reading.
- Database links are search links based on item names, not direct item-page links.
- Discord message-size, upload-size, and rate limits apply.
- Character names and screenshots may be personal or privacy-sensitive information. Verify the destination and audience before posting.

## Troubleshooting

- **The button does not appear:** Confirm that another character's Examine window is currently visible and the plugin is enabled.
- **The button says Loading:** Wait for inspect data to finish loading. Open the Coordinate view and try again.
- **Position editing reports an error:** Keep the Examine window open until `Save position` is pressed.
- **Discord posting fails:** Recheck the Webhook URL, test it from settings, and confirm that the Webhook still exists and can post to the channel.
- **The wrong language is shown:** Open `/glamoursaver` and select `English` or `日本語`. Client-language detection runs only before a language has been saved.
- **The screenshot includes unwanted content:** Move or disable overlays that cover the Examine window before posting.

## Uninstallation

1. Disable and remove `Glamour Saver` in the Dalamud plugin installer.
2. Delete the `GlamourSaver` configuration JSON from Dalamud's configuration directory if you also want to remove saved settings.
3. Delete the Discord Webhook from the channel settings if it is no longer needed.
4. Remove the shared custom repository URL from Dalamud only if you no longer use any plugins from that repository.

## Building and packaging

```powershell
dotnet restore .\GlamourSaver\GlamourSaver.csproj --locked-mode
dotnet build .\GlamourSaver\GlamourSaver.csproj -c Release --no-restore
```

The SDK is pinned by `global.json`, and NuGet dependencies are pinned by `packages.lock.json`. The versioned release archive is generated at `GlamourSaver/bin/Release/GlamourSaver/GlamourSaver-0.5.0.0.zip`.

## Optional support

If you would like to support Roxyz0501's development, visit [Ko-fi: Roxyz0501](https://ko-fi.com/roxyz0501). Support is entirely optional and does not unlock or restrict any features.

## AI usage

OpenAI Codex and image generation tools assisted with the code, documentation, release preparation, and project icon. Human code review, rights review, in-game testing, and a real Discord posting test are required before publication.

## License, third-party references, and attribution

Glamour Saver is licensed under the MIT License. See `LICENSE`.

- `SixLabors.ImageSharp 3.1.12` is used to encode captured bitmaps as PNG and is redistributed under the terms documented in `THIRD_PARTY_NOTICES.md` and `licenses/SixLabors.ImageSharp-LICENSE.txt`.
- Dalamud, Dalamud.Bindings.ImGui, Lumina, FFXIVClientStructs, and InteropGenerator.Runtime are runtime/build API dependencies and are not redistributed in the Release ZIP.
- The project icon was generated specifically for this project with OpenAI's image generation tool; it was not copied from another plugin or asset pack.
- No source code, data, assets, IPC contract, or implementation from another third-party plugin is referenced or reused by this standalone plugin.

This software is provided without warranty. The project is not affiliated with or endorsed by Square Enix, XIVLauncher, Dalamud, Discord, or Ko-fi.
