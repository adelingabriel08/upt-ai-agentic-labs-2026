// See https://aka.ms/new-console-template for more information
using System.ClientModel;
using System.Text.Json;
using Anthropic;
using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;

// Varianta Anthropic: nu avem endpoint de configurat, SDK-ul stie singur adresa API-ului.
// var endpoint = "";
// Cheia API (console.anthropic.com). NU o comitem in Git - in practica: variabila de mediu / user-secrets.
var key = "";
// Aici punem direct numele modelului (nu exista "deployment" ca in Azure).
var modelName = "claude-haiku-4-5-20251001";

// Singura diferenta fata de Lab1: clientul. Dupa AsIChatClient(...) avem acelasi IChatClient,
// deci restul programului (agent, mesaje, bucla) ramane identic - asta este avantajul abstractiei.
IChatClient client = new AnthropicClient() {ApiKey = key}
    .AsIChatClient(modelName);

// Istoricul conversatiei: trimis integral la fiecare apel (modelul nu are memorie proprie).
var messages = new List<ChatMessage>();

// Instructiunile agentului (system prompt). Observati diferenta fata de Lab1:
// cerem explicit "raw json, fara markdown", pentru ca Claude tinde sa inconjoare JSON-ul cu ```json ... ```,
// iar asta ar strica parsarea cu JsonSerializer.
var instructions = @"You are an AI Agent that works within a Book Library. 
    You can answer questions about books, authors, and publication years.
    You can also provide a brief description of a book if requested.
    If you don't know the answer, please say 'I don't know' instead of making up information.
    Please provide your answers in a clear and concise manner.
    You can provide book recommendations based on user preferences, 
    but make sure to clarify that they are suggestions and not definitive answers.
    You can answer only in raw json format (do not include the markdown quotes format in the response, this is going to be parsed as JSON) of the following structure:
    {
        ""Answer"": ""Your answer here"",
        ""Recommendations"": [
            {
                ""Title"": ""Book Title"",
                ""Author"": ""Author Name"",
                ""Year"": 2023,
                ""Description"": ""Brief description of the book""
            }
        ]
    }";

// System = primul mesaj din istoric, defineste comportamentul agentului.
messages.Add(new ChatMessage(ChatRole.System, instructions));

// Agentul (Microsoft Agent Framework) invelit peste IChatClient.
var agent = client.AsAIAgent();

while (true)
{
    var input = Console.ReadLine();
    if (input == "exit")
    {
        break;
    }

    // Adaugam mesajul utilizatorului si trimitem tot istoricul la model.
    messages.Add(new ChatMessage(ChatRole.User, input!));
    var response = await agent.RunAsync(messages);
    // Afisam raspunsul brut (textul JSON) ca sa vedem exact ce a returnat modelul.
    // Partea de deserializare in clasa Output este comentata - exercitiu pentru studenti.
    Console.WriteLine("Raw AI Response: " + response.Text);
    // var structuredResponse = JsonSerializer.Deserialize<Output>(response.Text);

    // Console.WriteLine("AI: " + structuredResponse!.Answer);

    // messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));

    // foreach (var recommendation in structuredResponse.Recommendations ?? new List<Book>())
    // {
    //     Console.WriteLine($"Recommendation: {recommendation.Title} by {recommendation.Author} ({recommendation.Year})");
    //     if (!string.IsNullOrEmpty(recommendation.Description))
    //     {
    //         Console.WriteLine($"Description: {recommendation.Description}");
    //     }
    // }
}