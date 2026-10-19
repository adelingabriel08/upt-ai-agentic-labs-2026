using System.ClientModel;
using Anthropic;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI;

/// <summary>Providerii de modele suportati de template.</summary>
public enum LlmProvider
{
    AzureOpenAI,
    OpenAI,
    Claude,
    Gemini,
    Ollama,
    Groq,
    Mistral,
    OpenRouter
}

/// <summary>
/// Construieste un IChatClient pentru providerul ales.
/// Dupa aceasta clasa, restul aplicatiei (agent, tools, bucla) NU mai stie ce provider folosim.
/// </summary>
public static class ChatClientFactory
{
    public static IChatClient Create(LlmProvider provider, string endpoint, string model, string key)
    {
        switch (provider)
        {
            // Azure OpenAI: endpoint obligatoriu; "model" = numele deployment-ului din Azure.
            case LlmProvider.AzureOpenAI:
                return new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key))
                    .GetChatClient(model)
                    .AsIChatClient();

            // OpenAI: endpoint nefolosit (se foloseste api.openai.com).
            case LlmProvider.OpenAI:
                return new OpenAIClient(key)
                    .GetChatClient(model)
                    .AsIChatClient();

            // Claude: endpoint nefolosit, SDK-ul il cunoaste.
            case LlmProvider.Claude:
                return new AnthropicClient() { ApiKey = key }
                    .AsIChatClient(model);

            // Providerii de mai jos folosesc API-ul compatibil OpenAI.
            // Daca endpoint-ul e gol, se foloseste cel implicit al providerului.
            case LlmProvider.Gemini:
                return OpenAICompatible(endpoint, "https://generativelanguage.googleapis.com/v1beta/openai/", key, model);

            // Ollama ruleaza local si nu are nevoie de cheie (poate ramane goala).
            case LlmProvider.Ollama:
                return OpenAICompatible(endpoint, "http://localhost:11434/v1", key, model);

            case LlmProvider.Groq:
                return OpenAICompatible(endpoint, "https://api.groq.com/openai/v1", key, model);

            case LlmProvider.Mistral:
                return OpenAICompatible(endpoint, "https://api.mistral.ai/v1", key, model);

            case LlmProvider.OpenRouter:
                return OpenAICompatible(endpoint, "https://openrouter.ai/api/v1", key, model);

            default:
                throw new ArgumentOutOfRangeException(nameof(provider), provider, "Provider necunoscut.");
        }
    }

    // Clientul OpenAI poate vorbi cu orice server care implementeaza API-ul OpenAI,
    // daca ii schimbam Endpoint-ul. Asa acoperim mai multi provideri cu un singur pachet.
    private static IChatClient OpenAICompatible(string endpoint, string defaultEndpoint, string key, string model) =>
        new OpenAIClient(
                new ApiKeyCredential(string.IsNullOrEmpty(key) ? "none" : key), // clientul nu accepta cheie goala
                new OpenAIClientOptions { Endpoint = new Uri(string.IsNullOrWhiteSpace(endpoint) ? defaultEndpoint : endpoint) })
            .GetChatClient(model)
            .AsIChatClient();
}
