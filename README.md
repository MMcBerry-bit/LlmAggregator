# Gemini LLM Aggregator for Visual Studio

A Visual Studio extension that aggregates responses from multiple Gemini/Vertex AI endpoints and displays them in a tool window.

## Features
- Tool window with prompt input and aggregated results.
- Per-endpoint configuration with URL and optional API key / service-account JSON path.
- Vertex AI service account JSON support with token exchange and caching.
- Save and persist endpoint configuration via Tools → Options.
- Basic response parsing for common Vertex AI response shapes.

## Quick Start
1. Build and run the extension from Visual Studio (F5) to open an Experimental Instance.
2. In the Experimental Instance: Tools → Options → Gemini LLM Aggregator → General
   - Enter your global API key or path to a service-account JSON file.
3. Configure endpoints: Tools → ToolWindow1 → Configure Endpoints
   - Add one or more Vertex AI model predict URLs (example below) and optional per-endpoint keys.
   - Example endpoint URL:
	 `https://us-central1-aiplatform.googleapis.com/v1/projects/PROJECT_ID/locations/us-central1/models/MODEL_ID:predict`
4. Open Tools → ToolWindow1, enter a prompt and click Aggregate.

## Development
- Requires Visual Studio with VSIX development workload.
- Target framework: .NET Framework 4.7.2
- Uses Google.Apis.Auth for service account token exchange and Newtonsoft.Json for parsing.

## Packaging
- Build > Create VSIX Package or run:
  `msbuild /t:CreateVsixContainer /p:Configuration=Release LlmAggregator.csproj`

## Security
- Do NOT commit service-account JSON files or API keys into source control.
- Consider using Windows Credential Manager for storing secrets instead of the Options page.

## Limitations
- Response parsing is best-effort and may need adjustments for different Vertex models.
- Endpoint adapters are minimal; you may need to adjust request shape for advanced model features.

## License
MIT
