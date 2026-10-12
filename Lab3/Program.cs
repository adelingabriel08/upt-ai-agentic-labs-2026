using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// 1) Alegeti providerul si completati endpoint, model si key. NU comitati cheia in Git!
//
//    Provider      Endpoint                     Model (exemplu)
//    AzureOpenAI   https://<resursa>.openai.azure.com/   numele deployment-ului (ex: gpt-4.1-mini)
//    OpenAI        (gol)                        gpt-4.1-mini
//    Claude        (gol)                        claude-haiku-4-5-20251001
//    Gemini        (gol = implicit)             gemini-2.5-flash
//    Ollama        (gol = localhost:11434)      llama3.1  (cheie goala; ollama serve + ollama pull)
//    Groq          (gol = implicit)             llama-3.3-70b-versatile
//    Mistral       (gol = implicit)             mistral-small-latest
//    OpenRouter    (gol = implicit)             openai/gpt-4.1-mini
var provider = LlmProvider.AzureOpenAI;
var endpoint = "https://<resursa>.openai.azure.com/";
var model = "gpt-4.1-mini";
var key = "";

IChatClient client = ChatClientFactory.Create(provider, endpoint, model, key);

// 2) Instructiunile agentului (system prompt). Raspunsul este text liber, fara format JSON.
var instructions = @"You are an AI Agent that works within a Book Library.
    You can answer questions about books, authors, and publication years.
    If you don't know the answer, say 'I don't know' instead of making up information.
    Provide your answers in a clear and concise manner.";

// 3) Agentul (Microsoft Agent Framework) peste IChatClient.
//    TODO Lab 3: aici se vor adauga tools / alte capabilitati ale agentului.
var agent = client.AsAIAgent();

// 4) Istoricul conversatiei: modelul nu are memorie, deci trimitem tot istoricul la fiecare apel.
var messages = new List<ChatMessage>
{
    new(ChatRole.System, instructions)
};

Console.WriteLine($"Provider: {provider}");
Console.WriteLine("Ask me anything (or type 'exit' to quit):");

while (true)
{
    var input = Console.ReadLine();
    if (input == "exit")
    {
        break;
    }
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    messages.Add(new ChatMessage(ChatRole.User, input));

    var response = await agent.RunAsync(messages);
    Console.WriteLine("AI: " + response.Text);

    // Salvam raspunsul in istoric ca modelul sa "tina minte" conversatia.
    messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
}
