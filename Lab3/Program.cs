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

// 2) Tool-urile agentului (vezi Tools.cs).
//    AIFunctionFactory.Create transforma o functie C# intr-un tool pe care modelul il poate apela.
List<AITool> tools = new()
{
    AIFunctionFactory.Create(BookTools.SearchBook),
    AIFunctionFactory.Create(BookTools.GetBookPrice),
    AIFunctionFactory.Create(UtilityTools.GetCurrentDateTime),
    AIFunctionFactory.Create(UtilityTools.GetWeather),
    AIFunctionFactory.Create(UtilityTools.ConvertCurrency),
};

// OPTIONAL: puneti true ca sa adaugati si tool-urile serverului MCP Microsoft Learn (vezi McpTools.cs).
// Nu necesita cheie, doar conexiune la internet.
var useMcp = false;
if (useMcp)
{
    tools.AddRange(await McpTools.LoadAsync());
}

// OPTIONAL: puneti true ca sa transformati automat un API public in tool-uri, pornind de la specificatia lui
// OpenAPI/Swagger (vezi OpenApiTools.cs). Exemplu: Wizard World API (spelluri, potiuni, case Hogwarts etc.).
var useOpenApi = false;
if (useOpenApi)
{
    tools.AddRange(await OpenApiTools.LoadAsync("https://wizard-world-api.herokuapp.com/swagger/v1/swagger.json"));
}

// 3) Instructiunile agentului (system prompt).
var instructions = @"You are an AI Agent that works within a Book Library.
    Use the available tools instead of guessing:
    - SearchBook for author and publication year,
    - GetBookPrice for prices,
    - GetCurrentDateTime for the current date, time or day of the week (you do not know today's date),
    - GetWeather for the current weather in a city,
    - ConvertCurrency for currency conversions with up-to-date rates,
    - the Microsoft Learn tools (if available) for questions about Microsoft/Azure/.NET documentation,
    - the Get* tools (if available) for the Wizard World API (Harry Potter spells, elixirs, houses, wizards...).
      Prefer filters over listing everything; the *Id tools need an id taken from a previous result.
    If a tool fails or returns an error, tell the user honestly what went wrong. Never make up data.
    Provide your answers in a clear and concise manner.";

// 4) Agentul primeste lista de tool-uri.
//    Cand modelul cere un tool, framework-ul il apeleaza automat si trimite rezultatul inapoi la model,
//    apoi repeta pana cand modelul are un raspuns final. Noi vedem doar raspunsul final.
var agent = client.AsAIAgent(instructions: instructions, tools: tools);

var messages = new List<ChatMessage>();

Console.WriteLine($"Provider: {provider}");
Console.WriteLine("Incercati: 'Cine a scris Dune?', 'Cat costa 1984?', 'Cat costa Hamlet?',");
Console.WriteLine("           'Ce zi este azi?', 'Cum e vremea in Timisoara?', 'Cat fac 250 EUR in RON?'");
Console.WriteLine("           (scrieti 'exit' pentru iesire)");

while (true)
{
    var input = Console.ReadLine();
    if (input == null || input == "exit") // null = Ctrl+D / sfarsit de input
    {
        break;
    }
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    messages.Add(new ChatMessage(ChatRole.User, input));

    try
    {
        var response = await agent.RunAsync(messages);
        Console.WriteLine("AI: " + response.Text);
        messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));
    }
    catch (Exception ex)
    {
        // Cheie invalida, retea cazuta, timeout etc. Nu lasam aplicatia sa se opreasca.
        Console.WriteLine("Eroare: " + ex.Message);
        messages.RemoveAt(messages.Count - 1); // scoatem intrebarea care a esuat din istoric
    }
}
