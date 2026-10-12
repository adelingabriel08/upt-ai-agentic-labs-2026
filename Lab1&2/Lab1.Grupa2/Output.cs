// Obiectul-radacina al raspunsului JSON cerut modelului (acelasi rol ca AIResponse din Lab1).
// Lista este initializata cu "new List<Book>()" ca sa nu fie niciodata null.
public class Output
{
    public string? Answer { get; set; }
    public List<Book> Recommendations { get; set; } = new List<Book>();
}