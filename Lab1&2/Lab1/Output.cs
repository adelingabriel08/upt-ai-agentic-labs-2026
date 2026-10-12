// Clasele de mai jos oglindesc structura JSON ceruta modelului in system prompt.
// Numele proprietatilor trebuie sa corespunda cu cheile din JSON (Answer, Recommendations, Title...).
public class Book
{
    public string Title { get; set; }
    public string Author { get; set; }
    public int Year { get; set; }

    public string? Description { get; set; }

}

// Obiectul-radacina in care deserializam raspunsul complet al modelului.
public class AIResponse
{
    public string? Answer { get; set; }
    public List<Book>? Recommendations { get; set; }
}