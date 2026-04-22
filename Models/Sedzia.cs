using System.ComponentModel.DataAnnotations;
using Sedziowanie.Models;

namespace Sedziowanie.Models
{
    using System.ComponentModel.DataAnnotations;

    public class Sedzia
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(50)]
        public string Imie { get; set; }

        [MaxLength(50)]
        public string Nazwisko { get; set; }

        [MaxLength(5)]
        public string Klasa { get; set; }

        [MaxLength(15)]
        public string Telefon { get; set; }

        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [MaxLength(100)]
        public string Miasto { get; set; }

        [Display(Name = "Na urlopie")]
        public bool CzyUrlop { get; set; }  

        public ICollection<Niedyspozycja> Niedyspozycje { get; set; } = new List<Niedyspozycja>();
        public ICollection<Mecz> MeczeJakoSedzia1 { get; set; } = new List<Mecz>();
        public ICollection<Mecz> MeczeJakoSedzia2 { get; set; } = new List<Mecz>();
        public ICollection<Mecz> MeczeJakoSedziaSekretarz { get; set; } = new List<Mecz>();
        public ICollection<Mecz> MeczeJakoSedziaLiniowyI { get; set; } = new List<Mecz>();
        public ICollection<Mecz> MeczeJakoSedziaLiniowyII { get; set; } = new List<Mecz>();
        public ICollection<Mecz> MeczeJakoSedziaGlowny { get; set; } = new List<Mecz>();
    }

}
