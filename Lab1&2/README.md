# Laborator 1 & 2 - Primul agent AI in C#

Acest folder contine doua variante ale aceluiasi mic **agent conversational pentru o biblioteca de carti**:

| Proiect | Provider | Pachete NuGet |
|---|---|---|
| `Lab1/` | Azure OpenAI (`gpt-4.1-mini`) | `Microsoft.Agents.AI.OpenAI`, `Azure.AI.OpenAI` |
| `Lab1.Grupa2/` | Anthropic (`claude-haiku-4-5`) | `Anthropic`, `Microsoft.Agents.AI.OpenAI` |

Ambele sunt aplicatii de consola (.NET 10). Ideea principala: **doar crearea clientului difera**; restul codului (agent, istoric, bucla, parsare JSON) este identic.

> **Setup:** in `Program.cs` campurile `endpoint` si `key` sunt goale intentionat. Completati-le local (sau, mai bine, cititi-le din variabile de mediu / `dotnet user-secrets`). **Nu comitati niciodata chei in Git.**

---

## 1. Schema generala

```
Utilizator --> [ lista de mesaje ] --> Agent --> IChatClient --> Model (Azure OpenAI / Claude)
                 System + User +                                        |
                 Assistant + ...                                        v
Utilizator <-- obiect C# <-- JsonSerializer <-- response.Text <---------+
```

Patru lucruri conteaza in integrarea cu AI. Le luam pe rand.

---

## 2. Clientul: conectarea la model

### Lab1 (Azure OpenAI)

```csharp
IChatClient client = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(key))
    .GetChatClient(modelName)
    .AsIChatClient();
```

- `endpoint` + `key` = unde si cu ce credentiale ne conectam (din portalul Azure: *Keys and Endpoint*).
- `modelName` = numele **deployment-ului** creat in Azure, nu neaparat numele modelului.
- `AsIChatClient()` converteste clientul specific providerului in `IChatClient` din `Microsoft.Extensions.AI`.

### Lab1.Grupa2 (Anthropic)

```csharp
IChatClient client = new AnthropicClient() { ApiKey = key }
    .AsIChatClient(modelName);
```

Aici nu exista endpoint (SDK-ul il cunoaste) si nici deployment: se da direct numele modelului.

### De ce conteaza `IChatClient`?

Este o **interfata comuna** pentru orice provider. Tot ce urmeaza (agentul, mesajele, bucla) depinde doar de `IChatClient`. Schimbarea de la OpenAI la Claude inseamna schimbarea a 2-3 linii, nu rescrierea aplicatiei.

---

## 3. Agentul

```csharp
var agent = client.AsAIAgent();
```

`AsAIAgent()` (din **Microsoft Agent Framework**) imbraca `IChatClient` intr-un *agent*. In acest laborator agentul doar trimite mesajele la model si intoarce raspunsul, dar este punctul de extindere pentru lucrurile din laboratoarele urmatoare: tools, memorie, orchestrare.

Apelul efectiv:

```csharp
var response = await agent.RunAsync(messages);
// response.Text = raspunsul modelului, ca string
```

Este `async` pentru ca e un apel de retea (poate dura secunde si poate esua: cheie invalida, timeout, limita de cereri).

---

## 4. Mesajele si rolurile (contextul conversatiei)

Un model de limbaj **nu are memorie intre apeluri**. La fiecare `RunAsync` trebuie sa trimitem **tot istoricul** conversatiei:

```csharp
var messages = new List<ChatMessage>();
messages.Add(new ChatMessage(ChatRole.System, "...instructiuni..."));   // o singura data, la inceput

// in bucla:
messages.Add(new ChatMessage(ChatRole.User, input));                    // intrebarea
var response = await agent.RunAsync(messages);
messages.Add(new ChatMessage(ChatRole.Assistant, response.Text));       // raspunsul salvat in istoric
```

| Rol | Cine il scrie | Rol in conversatie |
|---|---|---|
| `System` | Noi (programatorul) | Defineste *cine este* agentul, regulile si formatul raspunsului |
| `User` | Utilizatorul | Intrebarea curenta |
| `Assistant` | Modelul | Raspunsurile anterioare (le adaugam noi inapoi, ca sa isi "aminteasca") |

Consecinte practice:
- Daca **nu** adaugam raspunsul `Assistant` in lista, modelul uita ce a spus (intrebari de tip *"si cartea asta cand a aparut?"* nu mai merg).
- Istoricul **creste** la fiecare tura: costa mai multi tokeni si, la un moment dat, depaseste fereastra de context. Problema se rezolva prin trunchiere / sumarizare (tema pentru mai tarziu).
- Fiecare apel este *stateless* pe partea serverului; "memoria" este doar lista noastra locala.

---

## 5. System prompt-ul

Aici se "programeaza" comportamentul agentului, in limbaj natural. Prompt-ul din lab are patru parti:

1. **Rol:** *"You are an AI Agent that works within a Book Library."*
2. **Capabilitati:** raspunde despre carti, autori, ani, descrieri, recomandari.
3. **Reguli anti-halucinatie:** *"If you don't know the answer, say 'I don't know'"*. Fara asta, modelul inventeaza raspunsuri plauzibile.
4. **Format de iesire:** doar JSON, cu o structura exacta.

---

## 6. Structured output: JSON din raspunsul modelului

Pentru ca aplicatia sa foloseasca raspunsul (nu doar sa-l afiseze), cerem modelului JSON, il deserializam si lucram cu obiecte C#.

**Pasul 1 - cerem structura in prompt:**

```json
{
  "Answer": "Your answer here",
  "Recommendations": [
    { "Title": "...", "Author": "...", "Year": 2023, "Description": "..." }
  ]
}
```

**Pasul 2 - clase C# care oglindesc structura** (`Output.cs` / `Book.cs`):

```csharp
public class Book { string Title; string Author; int Year; string? Description; }
public class AIResponse { string? Answer; List<Book>? Recommendations; }
```

**Pasul 3 - deserializam:**

```csharp
var structuredResponse = JsonSerializer.Deserialize<AIResponse>(response.Text);
Console.WriteLine("AI: " + structuredResponse?.Answer);
```

### Capcana principala: modelul nu garanteaza JSON valid

Formatul este doar **cerut** prin prompt, nu **impus**. Doua probleme reale pe care le veti vedea:

- Modelul poate inconjura JSON-ul cu markdown: ` ```json ... ``` `. Atunci `Deserialize` arunca `JsonException`.
- Poate adauga text in plus ("Iata raspunsul: {...}") sau poate omite campuri.

De aceea difera cele doua prompturi:

| | Lab1 | Lab1.Grupa2 |
|---|---|---|
| Instructiune de format | *"You can answer only in json format"* | *"...**raw json** (do not include the markdown quotes format, this is going to be parsed as JSON)"* |

Varianta din `Grupa2` este mai explicita: ii spunem **de ce** (va fi parsat) si **ce sa nu faca** (markdown). Acesta este un exemplu simplu de *prompt engineering* din Laboratorul 2: aceeasi sarcina, doua prompturi, rezultate diferite.

> In `Lab1.Grupa2/Program.cs` deserializarea este comentata, iar programul afiseaza `Raw AI Response`. Este util pentru a **vedea exact ce intoarce modelul** inainte de a-l parsa.

Solutii mai robuste (la nivel mai avansat): *structured outputs / JSON mode* din API (modelul e constrans de schema), validare + retry, sau curatarea textului inainte de parsare.

---

## 7. Bucla principala, pas cu pas

```csharp
while (true)
{
    var input = Console.ReadLine();
    if (input == "exit") break;

    messages.Add(new ChatMessage(ChatRole.User, input!));     // 1. adaugam intrebarea
    var response = await agent.RunAsync(messages);            // 2. apel catre model (tot istoricul)
    var structuredResponse =
        JsonSerializer.Deserialize<AIResponse>(response.Text);// 3. text JSON -> obiect C#
    messages.Add(new ChatMessage(ChatRole.Assistant, response.Text)); // 4. salvam raspunsul in istoric
    Console.WriteLine("AI: " + structuredResponse?.Answer);   // 5. afisam
}
```

In `Lab1`, partea comentata din `Program.cs` arata cum se folosesc recomandarile (`Recommendations`) ca obiecte `Book` si cum se pot colecta intr-o lista (`books`).

---

## 8. Ce NU acopera codul (din cerintele laboratoarelor)

Codul este versiunea esentiala. Nu include (si nu a fost cerut la ora):

- **Tratarea erorilor** (cheie invalida, timeout, raspuns gol, JSON invalid): in acest moment orice eroare opreste programul. Un `try/catch` in jurul `RunAsync` si `Deserialize` este primul pas.
- **Fallback multi-model** (bonus Lab1): daca un model esueaza, se incearca altul.
- **Few-shot prompting** (Lab2): exemple intrebare -> raspuns in prompt, ca modelul sa copieze formatul.
- **Validarea** structurii JSON primite si **compararea** mai multor prompturi.
- **Gestionarea cheilor** in siguranta (variabile de mediu, user-secrets, Key Vault).

## 9. Idei de explorat in laborator

1. Rulati cu intrebari variate: *"Cine a scris 1984?"*, *"Recomanda-mi ceva SF"*, o intrebare in afara domeniului, o intrebare care nu are raspuns cunoscut.
2. Scoateti linia `messages.Add(... Assistant ...)` si observati cum dispare memoria conversatiei.
3. Pe `Lab1` stricati regula de format din prompt (de ex. cereti raspuns "frumos") si urmariti `JsonException`.
4. Comparati raspunsul brut al celor doua modele (OpenAI vs Claude) la acelasi prompt.
5. Dezcomentati afisarea recomandarilor si completati lista `books`.
