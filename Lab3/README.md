# Lab 3 - Template

Pornit din `Lab1&2/Lab1.Grupa2`, fara parsare JSON: agentul raspunde cu text liber.

## Alegerea providerului

La inceputul `Program.cs` se seteaza patru valori:

```csharp
var provider = LlmProvider.Claude;
var endpoint = "";
var model = "claude-haiku-4-5-20251001";
var key = "";
```

| Provider | Endpoint | Model (exemplu) |
|---|---|---|
| `AzureOpenAI` | `https://<resursa>.openai.azure.com/` (obligatoriu) | numele deployment-ului |
| `OpenAI` | gol | `gpt-4.1-mini` |
| `Claude` | gol | `claude-haiku-4-5-20251001` |
| `Gemini` | gol = implicit | `gemini-2.5-flash` |
| `Ollama` | gol = `http://localhost:11434/v1` | `llama3.1` (cheie goala) |
| `Groq` | gol = implicit | `llama-3.3-70b-versatile` |
| `Mistral` | gol = implicit | `mistral-small-latest` |
| `OpenRouter` | gol = implicit | `openai/gpt-4.1-mini` |

Logica este in `ChatClientFactory.cs`. Gemini, Ollama, Groq, Mistral si OpenRouter folosesc clientul OpenAI cu alt `Endpoint` (API compatibil OpenAI).

## Rulare

```bash
dotnet run
```

Nu comitati cheia in Git.
