using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;

// OPTIONAL: tool-uri de pe un server MCP (Model Context Protocol).
// MCP este un standard pentru a expune tool-uri catre agenti. Ne conectam la un server MCP,
// ii cerem lista de tool-uri si le dam agentului exact ca pe cele locale (din Tools.cs).
//
// Folosim serverul public Microsoft Learn MCP: cauta in documentatia Microsoft (Azure, .NET etc.).
// Este un server remote (HTTP), gratuit si fara cheie / autentificare.
// Documentatie: https://learn.microsoft.com/en-us/training/support/mcp
public static class McpTools
{
    public static async Task<IList<AITool>> LoadAsync()
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Name = "microsoft-learn",
            Endpoint = new Uri("https://learn.microsoft.com/api/mcp")
        });

        var mcpClient = await McpClient.CreateAsync(transport);

        // Lista de tool-uri a serverului; fiecare este deja un AITool, deci poate fi dat direct agentului.
        // (microsoft_docs_search, microsoft_docs_fetch, microsoft_code_sample_search)
        var mcpTools = await mcpClient.ListToolsAsync();

        Console.WriteLine("MCP tools: " + string.Join(", ", mcpTools.Select(t => t.Name)));
        return mcpTools.Cast<AITool>().ToList();
    }
}
