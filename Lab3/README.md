# Lab 3 - Tool use si function calling

Pornit din `Lab1&2/Lab1.Grupa2`. Agentul de biblioteca primeste acum **tool-uri**: functii C# pe care le poate apela singur.

## Fisiere

| Fisier | Ce contine |
|---|---|
| `Program.cs` | Alegerea providerului, inregistrarea tool-urilor, agentul si bucla de chat |
| `Tools.cs` | Tool-urile: `SearchBook`, `GetBookPrice` (poate esua), `GetCurrentDateTime`, `GetWeather`, `ConvertCurrency` |
| `McpTools.cs` | **Optional:** tool-uri de pe un server MCP |
| `OpenApiTools.cs` | **Optional:** tool-uri generate automat dintr-o specificatie OpenAPI (Swagger), prin Semantic Kernel |
| `ChatClientFactory.cs` | Switch intre providerii de modele (Azure OpenAI, OpenAI, Claude, Gemini, Ollama...) |

## Configurare

La inceputul `Program.cs` se seteaza `provider`, `endpoint`, `model` si `key` (nu comitati cheia in Git):

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

Modelul trebuie sa suporte tool calling (cele din tabel il suporta; pentru Ollama alegeti un model cu tools).

```bash
dotnet run
```

## 1. Ce este un tool si cum functioneaza function calling

Un tool este o functie C# obisnuita. **Modelul nu o executa**; el doar cere sa fie apelata.

```
Utilizator: "Cine a scris Dune?"
   |
   v
Agent --(intrebare + lista de tool-uri)--> Model
Model --("apeleaza SearchBook(title='Dune')")--> Agent
Agent ruleaza SearchBook in C#  ->  "Dune by Frank Herbert, 1965"
Agent --(rezultatul tool-ului)--> Model
Model --("Dune a fost scrisa de Frank Herbert.")--> Utilizator
```

Framework-ul (Microsoft Agent Framework) face singur bucla de mai sus; in cod vedem doar `agent.RunAsync(...)`.

Un tool se defineste in doi pasi:

```csharp
// Tools.cs - functia, cu [Description] ca modelul sa stie cand o foloseste
[Description("Searches the library for a book by title and returns its author and publication year.")]
public static string SearchBook([Description("The title of the book")] string title) { ... }

// Program.cs - inregistrarea si darea ei agentului
List<AITool> tools = new() { AIFunctionFactory.Create(BookTools.SearchBook) };
var agent = client.AsAIAgent(instructions: instructions, tools: tools);
```

Modelul alege tool-ul doar pe baza **numelui** si a **descrierilor**, deci ele conteaza la fel de mult ca si codul.
Fiecare tool afiseaza `[tool] ...` in consola, ca sa vedeti cand este apelat.

## 2. Tool-uri utile pentru un AI

Un model are limitari clare, iar tool-urile le acopera:

| Limitare a modelului | Tool | Sursa datelor |
|---|---|---|
| Nu stie ce zi / ora este (cunostintele lui se opresc la data antrenarii) | `GetCurrentDateTime` | ceasul calculatorului |
| Nu are informatii "live" (vreme) | `GetWeather` | API real [Open-Meteo](https://open-meteo.com), fara cheie |
| Nu are cursuri valutare actuale si gresete la calcule | `ConvertCurrency` | API real [Frankfurter](https://frankfurter.dev), fara cheie |
| Nu cunoaste datele private ale aplicatiei | `SearchBook`, `GetBookPrice` | lista din `Tools.cs` |

Incercati: *"Ce zi este azi?"*, *"Cate zile mai sunt pana la 1 decembrie?"*, *"Cum e vremea in Timisoara?"*,
*"Cat fac 250 EUR in RON?"*, *"Ce ora este acum in Tokyo?"*. Observati cum modelul combina tool-uri
(de ex. `SearchBook` + `GetBookPrice` pentru o singura intrebare).

## 3. Robustete: tool-uri care esueaza

`GetBookPrice` (si `GetCurrentDateTime`, la o zona orara inexistenta) arunca o exceptie pentru carti necunoscute. Framework-ul prinde exceptia si ii spune modelului ca tool-ul a esuat.
Instructiunile agentului cer sa spuna onest ce s-a intamplat, nu sa inventeze un pret.

Incercati: *"Cat costa 1984?"* (merge) si *"Cat costa Hamlet?"* (esueaza).
Bucla din `Program.cs` are si `try/catch` pentru erori de retea, cheie invalida etc.

## 4. MCP (optional)

MCP (Model Context Protocol) e un standard prin care un server extern expune tool-uri catre agenti.
Folosim serverul public [Microsoft Learn MCP](https://learn.microsoft.com/en-us/training/support/mcp)
(`https://learn.microsoft.com/api/mcp`): gratuit, fara cheie, doar conexiune la internet.

In `Program.cs` puneti `var useMcp = true;`. Agentul primeste, pe langa tool-urile locale, tool-urile serverului:
`microsoft_docs_search`, `microsoft_docs_fetch` si `microsoft_code_sample_search`.
Incercati: *"Cum fac function calling cu Azure OpenAI? Cauta in documentatia Microsoft."*

Observati ca din punctul de vedere al agentului tool-urile MCP arata la fel ca cele locale;
diferenta e doar ca functia ruleaza pe alt server.

## 5. Tool-uri din OpenAPI (optional)

Multe API-uri publice au o specificatie **OpenAPI / Swagger**: un fisier JSON care descrie toate endpoint-urile
(cale, metoda HTTP, parametri). Nu mai scriem tool-urile de mana: biblioteca **Semantic Kernel**
(`Microsoft.SemanticKernel.Plugins.OpenApi`) citeste fisierul si genereaza cate o functie per endpoint,
iar functiile se transforma direct in tool-uri pentru agent.

In `Program.cs` puneti `var useOpenApi = true;`. Se incarca [Wizard World API](https://wizard-world-api.herokuapp.com/swagger/index.html)
(lumea Harry Potter) din `https://wizard-world-api.herokuapp.com/swagger/v1/swagger.json`:

```
GET /Spells        -> tool api_GetSpells     (parametri: Name, Type, Incantation)
GET /Spells/{id}   -> tool api_GetSpellsId   (parametru: id)
... 12 tool-uri: Elixirs, Houses, Ingredients, Magicalcreature, Spells, Wizards
```

Tot codul necesar (`OpenApiTools.cs`) este:

```csharp
var kernel = Kernel.CreateBuilder().Build();
var plugin = await kernel.ImportPluginFromOpenApiAsync("api", new Uri(specUrl));   // citeste spec-ul
var tools = plugin.Where(f => f.Name.StartsWith("Get"))                            // doar GET (citire)
                  .Select(f => (AITool)f.WithKernel(kernel));                      // functie SK -> tool
```

Pentru alt API schimbati doar URL-ul spec-ului (cautati un `swagger.json` / `openapi.json`).
`LoggingFunction` din acelasi fisier doar afiseaza apelurile in consola (`[tool] api_GetSpells(Name=...)`).

Observatii:
- Sunt pastrate doar endpoint-urile `GET`, ca agentul sa nu poata modifica date (`POST /Feedback` este ignorat).
- Raspunsurile mari costa tokeni; de aceea prompt-ul agentului cere filtre in loc de liste complete.
- Descrierile tool-urilor vin din spec; Wizard World nu are, deci modelul se ghideaza dupa nume si parametri.

Incercati: *"Ce fel de spell este Opening Charm?"*, *"Da-mi 3 potiuni de dificultate Advanced"*,
*"Gaseste ingredientul Wolfsbane si da-mi detalii despre el"* (lant: lista, apoi dupa id).

## Exercitii

1. Adaugati un tool nou (de ex. `GetBooksByAuthor`, sau unul care apeleaza alt API public) in `Tools.cs` si inregistrati-l in `Program.cs`.
2. Stergeti `[Description]` de pe un tool si observati cum se schimba comportamentul modelului.
3. Intrebati *"Ce zi este azi?"* cu si fara tool-ul `GetCurrentDateTime` si comparati raspunsurile.
4. Faceti `GetBookPrice` sa esueze doar uneori si cereti agentului sa reincerce.
5. Incarcati alt API public (cautati un `swagger.json` / `openapi.json`) cu `OpenApiTools.LoadAsync(...)`.
