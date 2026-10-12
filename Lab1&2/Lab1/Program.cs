// See https://aka.ms/new-console-template for more information
using System.ClientModel;
using System.Text.Json;
using Azure.AI.OpenAI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

Console.WriteLine("Hello, World!");

// Datele de conectare la Azure OpenAI (din portalul Azure: resursa OpenAI -> "Keys and Endpoint").
// NU le comitem in Git! In aplicatii reale se citesc din variabile de mediu / user-secrets / Key Vault.
string endpoint = "";
string key = "";

// Numele *deployment-ului* din Azure (nu numele generic al modelului).
var modelName = "gpt-4.1-mini";

// 1) AzureOpenAIClient = clientul "brut" pentru Azure (endpoint + cheie).
// 2) GetChatClient(...) alege deployment-ul de chat.
// 3) AsIChatClient() il imbraca in IChatClient (Microsoft.Extensions.AI) - o interfata
//    comuna pentru orice provider. Restul codului nu mai stie ca e Azure OpenAI.
IChatClient client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key))
    .GetChatClient(modelName)
    .AsIChatClient();

// Agentul (Microsoft Agent Framework) este un wrapper peste IChatClient:
// aici adauga gestionarea apelului catre model; ulterior se pot adauga tools, memorie etc.
var agent = client.AsAIAgent();

Console.WriteLine("Ask me anything about AI and programming (or type 'exit' to quit):");

// Istoricul conversatiei. Modelul NU tine minte nimic intre apeluri,
// deci trimitem de fiecare data TOATA lista de mesaje.
var messages = new List<ChatMessage>();

// Mesajul de tip System = instructiunile agentului (rol, reguli, format de raspuns).
// Il punem primul in lista si ramane valabil pe toata conversatia.
// Atentie: formatul JSON este doar "cerut" prin prompt, modelul nu e obligat tehnic sa il respecte.
messages.Add(
    new ChatMessage(ChatRole.System,@"You are an AI Agent that works within a Book Library. 
    You can answer questions about books, authors, and publication years.
    You can also provide a brief description of a book if requested.
    If you don't know the answer, please say 'I don't know' instead of making up information.
    Please provide your answers in a clear and concise manner.
    You can provide book recommendations based on user preferences, 
    but make sure to clarify that they are suggestions and not definitive answers.
    You can answer only in json format of the following structure:
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
    }"));

List<Book> books = new List<Book> {};

while (true)
{
    var input = Console.ReadLine();
    if (input == "exit")
    {
        break;
    }
    // Adaugam intrebarea utilizatorului in istoric...
    messages.Add(new ChatMessage(ChatRole.User, input!));
    // ...si trimitem intregul istoric la model. response.Text contine raspunsul ca text.
    var response = await agent.RunAsync(messages);

    // Transformam textul JSON primit de la model intr-un obiect C#.
    // Daca modelul nu returneaza JSON valid (ex: il pune in ```json ... ```), aici apare JsonException.
    var structuredResponse = JsonSerializer.Deserialize<AIResponse>(response.Text);

    // Console.WriteLine("AI: " + structuredResponse?.Answer);
    // if (structuredResponse?.Recommendations != null && structuredResponse.Recommendations.Count > 0)
    // {
    //     Console.WriteLine("Book Recommendations:");
    //     foreach (var book in structuredResponse.Recommendations)
    //     {
    //         Console.WriteLine($"- {book.Title} by {book.Author} ({book.Year})");
    //         if (!string.IsNullOrEmpty(book.Description))
    //         {
    //             Console.WriteLine($"  Description: {book.Description}");
    //         }
    //     }
    //     books.AddRange(structuredResponse.Recommendations);
    // }

    // Salvam si raspunsul modelului (rol Assistant) in istoric, ca la urmatoarea
    // intrebare modelul sa "vada" ce a spus deja - asa apare memoria conversatiei.
    messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
    Console.WriteLine("AI: " + structuredResponse?.Answer);
}

// foreach (var book in books)
// {
//     Console.WriteLine($"Book: {book.Title}, Author: {book.Author}, Year: {book.Year}");
//     if (!string.IsNullOrEmpty(book.Description))
//     {
//         Console.WriteLine($"Description: {book.Description}");
//     }
// }
