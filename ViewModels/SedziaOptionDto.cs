namespace Sedziowanie.ViewModels
{
    public record SedziaOptionDto
    {
        public int Id { get; init; }
        public string Imie { get; init; } = "";
        public string Nazwisko { get; init; } = "";
        public string Klasa { get; init; } = "";
        public string Status { get; init; } = "DOSTEPNY";
        public bool MaMeczTegoDnia { get; init; }          
        public string Miasto { get; init; } = "";
    }


}
