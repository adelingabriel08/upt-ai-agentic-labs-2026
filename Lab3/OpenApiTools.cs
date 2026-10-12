using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Plugins.OpenApi;

// OPTIONAL: transforma automat un API public in tool-uri, pornind de la specificatia lui OpenAPI (Swagger).
//
// O specificatie OpenAPI este un fisier JSON care descrie TOATE endpoint-urile unui API
// (cale, metoda HTTP, parametri). Biblioteca Semantic Kernel stie sa o citeasca si sa creeze
// cate o functie pentru fiecare endpoint, deci nu scriem noi nimic de mana.
//
// Exemplu: https://wizard-world-api.herokuapp.com/swagger/v1/swagger.json
//   GET /Spells         ->  tool "GetSpells"     (parametri: Name, Type, Incantation)
//   GET /Spells/{id}    ->  tool "GetSpellsId"   (parametru: id)
public static class OpenApiTools
{
    public static async Task<IList<AITool>> LoadAsync(string specUrl)
    {
        // Semantic Kernel are nevoie de un "Kernel" ca sa importe plugin-ul; il folosim doar ca suport.
        var kernel = Kernel.CreateBuilder().Build();

        var plugin = await kernel.ImportPluginFromOpenApiAsync(
            "api",
            new Uri(specUrl));

        // Fiecare functie din plugin devine un AIFunction, adica exact acelasi tip de tool ca in Tools.cs.
        // Numele incep cu metoda HTTP (GetSpells, PostFeedback...). Pastram doar GET (citire),
        // ca agentul sa nu poata modifica date in API.
        var tools = plugin
            .Where(function => function.Name.StartsWith("Get"))
            .Select(function => (AITool)new LoggingFunction(function.WithKernel(kernel)))
            .ToList();

        Console.WriteLine($"OpenAPI tools ({tools.Count}): " + string.Join(", ", tools.Select(t => ((AIFunction)t).Name)));
        return tools;
    }
}

// Invelim fiecare tool intr-un "decorator" care doar afiseaza apelul in consola
// (la tool-urile din Tools.cs afisam manual cu Console.WriteLine) si apoi apeleaza functia reala.
public class LoggingFunction(AIFunction inner) : DelegatingAIFunction(inner)
{
    protected override ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        Console.WriteLine($"   [tool] {Name}({string.Join(", ", arguments.Select(a => $"{a.Key}={a.Value}"))})");
        return base.InvokeCoreAsync(arguments, cancellationToken);
    }
}
