# Light Copy Translator

Lightweight Windows copy-translate tool (WPF / .NET 10).

## Features
- Double-press Ctrl+C to trigger the popup
- Auto-hide when you click outside; no Alt-Tab entry
- Two-column layout: source on the left, cards on the right
- Configurable model sources via Settings (name, apiKey, model, baseUrl)

## Run
Check SDK:
```powershell
dotnet --version
```

Run:
```powershell
dotnet run --project src/LightCopyTranslator
```

## Config
You can edit models in Settings (tray icon -> Settings).

Config file (auto-created on first run):
```
%APPDATA%\\LightCopyTranslator\\config.json
```

DeepSeek key (recommended via env var):
```powershell
$env:OPENROUTER_API_KEY="sk-or-..."
```

Model config example: `config.sample.json`.

## Tray icon
Place your icon at `src/LightCopyTranslator/Resources/tray.ico` (copied to output on build).
