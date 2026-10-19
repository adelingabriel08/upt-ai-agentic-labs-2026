using System.ComponentModel;
using System.Text.Json;

// Un TOOL este o functie C# obisnuita pe care agentul o poate apela.
// Modelul NU executa codul: el doar decide ce functie vrea si cu ce argumente.
// Programul nostru ruleaza functia si trimite rezultatul inapoi la model.
//
// Doua lucruri sunt importante pentru ca modelul sa foloseasca bine un tool:
//   1. numele functiei si al parametrilor (sunt vazute de model)
//   2. atributul [Description] - spune modelului CAND si CUM sa foloseasca functia

public static class BookTools
{
    // "Baza de date" a bibliotecii: o lista simpla in memorie (titlu, autor, an, pret).
    private static readonly (string Title, string Author, int Year, double Price)[] Library =
    {
        ("1984", "George Orwell", 1949, 39.90),
        ("Dune", "Frank Herbert", 1965, 54.50),
        ("The Hobbit", "J.R.R. Tolkien", 1937, 44.00),
    };

    // Tool 1: functioneaza mereu. Daca nu gaseste cartea, intoarce un text (nu arunca eroare).
    [Description("Searches the library for a book by title and returns its author and publication year.")]
    public static string SearchBook(
        [Description("The title of the book, e.g. 'Dune'")] string title)
    {
        Console.WriteLine($"   [tool] SearchBook(\"{title}\")");

        foreach (var book in Library)
        {
            if (book.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                return $"{book.Title} by {book.Author}, published in {book.Year}";
            }
        }
        return "Book not found in the library.";
    }

    // Tool 2: poate ESUA. Pentru carti necunoscute arunca o exceptie.
    // Framework-ul prinde exceptia si ii spune modelului ca tool-ul a esuat;
    // un agent bun trebuie sa raspunda onest, nu sa inventeze un pret.
    [Description("Returns the price (in RON) of a book from the library.")]
    public static double GetBookPrice(
        [Description("The title of the book, e.g. 'Dune'")] string title)
    {
        Console.WriteLine($"   [tool] GetBookPrice(\"{title}\")");

        foreach (var book in Library)
        {
            if (book.Title.Equals(title, StringComparison.OrdinalIgnoreCase))
            {
                return book.Price;
            }
        }
        throw new InvalidOperationException($"Price service has no price for '{title}'.");
    }
}

public static class UtilityTools
{
    // Un singur HttpClient, refolosit pentru toate apelurile catre API-uri externe.
    private static readonly HttpClient Http = new();

    // Tool 3: data si ora. Un model NU stie ce zi este azi (cunostintele lui se opresc la data antrenarii),
    // deci orice intrebare despre "azi", "maine" sau "cate zile pana la..." are nevoie de acest tool.
    [Description("Gets the current date and time. Use it whenever the user asks about today's date, " +
                 "the current time, the day of the week, or anything relative to 'now'.")]
    public static string GetCurrentDateTime(
        [Description("Optional IANA time zone, e.g. 'Europe/Bucharest' or 'America/New_York'. Defaults to the local time zone.")]
        string? timeZone = null)
    {
        Console.WriteLine($"   [tool] GetCurrentDateTime(\"{timeZone}\")");

        var zone = string.IsNullOrWhiteSpace(timeZone)
            ? TimeZoneInfo.Local
            : TimeZoneInfo.FindSystemTimeZoneById(timeZone); // arunca exceptie daca zona nu exista
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone);

        return $"{now:dddd, yyyy-MM-dd HH:mm:ss} (time zone: {zone.Id}, UTC offset {now:zzz})";
    }

    // Tool 4: informatii "live" dintr-un API real (Open-Meteo, gratuit, fara cheie).
    // Doi pasi: 1) orasul -> coordonate, 2) coordonate -> vremea curenta.
    [Description("Gets the current weather (temperature, humidity, wind, precipitation) for a city.")]
    public static async Task<string> GetWeather(
        [Description("The city name, e.g. 'Timisoara'")] string city)
    {
        Console.WriteLine($"   [tool] GetWeather(\"{city}\")");

        var geoJson = await Http.GetStringAsync(
            $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1");
        using var geo = JsonDocument.Parse(geoJson);
        if (!geo.RootElement.TryGetProperty("results", out var results))
        {
            return $"City '{city}' was not found.";
        }
        var place = results[0];
        var lat = place.GetProperty("latitude").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var lon = place.GetProperty("longitude").GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture);

        var weatherJson = await Http.GetStringAsync(
            $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}" +
            "&current=temperature_2m,relative_humidity_2m,wind_speed_10m,precipitation");
        using var weather = JsonDocument.Parse(weatherJson);
        var current = weather.RootElement.GetProperty("current");

        return $"{place.GetProperty("name").GetString()}, {place.GetProperty("country").GetString()}: " +
               $"{current.GetProperty("temperature_2m").GetDouble()} C, " +
               $"humidity {current.GetProperty("relative_humidity_2m").GetDouble()}%, " +
               $"wind {current.GetProperty("wind_speed_10m").GetDouble()} km/h, " +
               $"precipitation {current.GetProperty("precipitation").GetDouble()} mm";
    }

    // Tool 5: cursuri valutare actuale (Frankfurter, gratuit, fara cheie).
    // Modelul nu are cursuri actuale si nici nu e de incredere la inmultiri; tool-ul face conversia exact.
    [Description("Converts an amount of money between two currencies using the latest exchange rate.")]
    public static async Task<string> ConvertCurrency(
        [Description("The amount to convert, e.g. 100")] double amount,
        [Description("The source currency ISO code, e.g. 'EUR'")] string from,
        [Description("The target currency ISO code, e.g. 'RON'")] string to)
    {
        Console.WriteLine($"   [tool] ConvertCurrency({amount}, \"{from}\", \"{to}\")");

        var json = await Http.GetStringAsync(
            $"https://api.frankfurter.dev/v1/latest?base={from.ToUpper()}&symbols={to.ToUpper()}");
        using var doc = JsonDocument.Parse(json);
        var rate = doc.RootElement.GetProperty("rates").GetProperty(to.ToUpper()).GetDouble();
        var date = doc.RootElement.GetProperty("date").GetString();

        return $"{amount} {from.ToUpper()} = {Math.Round(amount * rate, 2)} {to.ToUpper()} (rate {rate}, date {date})";
    }
}
