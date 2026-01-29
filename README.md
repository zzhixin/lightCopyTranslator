# Light Copy Translator

Lightweight Windows copy-translate tool (WPF / .NET 10).

## Motivation
[CopyTranslator](https://github.com/CopyTranslator/CopyTranslator) is convenient but it does not support LLM-based models and tends to be heavy on memory. This project aims to provide a lightweight alternative with modern LLM translation sources while keeping resource usage low and responsiveness high.

## Features
- Double-press Ctrl+C to trigger the popup
- Auto-hide when you click outside; no Alt-Tab entry
- Two-column layout: source on the left, cards on the right
- Configurable model sources via Settings (name, apiKey, model, baseUrl)

![screenshot](doc/screenshot.png)

## Tutorial
1. Download the portable ZIP in the releases page and extract to any loc.
2. Run `LightCopyTranslator.exe` inside the extracted folder.
3. Right click the tray icon and open the setting, add your llm api key; recommand using [openrouter](https://openrouter.ai/), see config section.
4. Select the text and ctrl CC to enjoy the translation!


## Build From Source
If you want to build from source:
1. Install .NET 10 SDK.
2. Open a terminal in this repo.
3. Run:
```
dotnet build src/LightCopyTranslator/LightCopyTranslator.csproj -c Release
```

The config file will be created at:
```
%APPDATA%\LightCopyTranslator\config.json
```

## Config
This app uses OpenRouter for LLM translation. Follow these steps:
1. Go to OpenRouter and create an API key.
2. Run the app, right-click the tray icon, and open **Settings**.
3. Add a model entry (name, model, baseUrl).
4. Paste your OpenRouter API key into **ApiKey** (leave blank only if you already set `OPENROUTER_API_KEY` in Windows).
5. Click **Save** and try Ctrl+C twice to translate.

The config file is auto-created here:
```
%APPDATA%\LightCopyTranslator\config.json
```

If you are not sure what to fill:
- Name: `DeepSeek`
- Model: `deepseek/deepseek-chat`
- BaseUrl: `https://openrouter.ai/api/v1`

Model config example: `config.sample.json`.
