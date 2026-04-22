using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Sedziowanie.Models
{
    public class Mecz
    {
        [Key] public int Id { get; set; }

        [Display(Name = "Numer meczu")]
        [MaxLength(10)] public string NumerMeczu { get; set; }

        [Required(ErrorMessage = "Data meczu jest wymagana.")]
        public DateTime Data { get; set; }

        public bool UdostepnijGodzine { get; set; } = false;

        [Required(ErrorMessage = "Rozgrywki są wymagane.")]
        public int RozgrywkiId { get; set; }

        [MaxLength(100)] public string Gospodarz { get; set; }
        [MaxLength(100)] public string Gosc { get; set; }

        public int? SedziaIId { get; set; }
        public int? SedziaIIId { get; set; }
        public int? SedziaSekretarzId { get; set; }

        
        public int? SedziaLiniowyIId { get; set; }
        public int? SedziaLiniowyIIId { get; set; }
        public int? SedziaGlownyId { get; set; }

        [MaxLength(200)] public string? Turniej { get; set; }
        [MaxLength(250)] public string? Adres { get; set; }
        public string? DodatkoweInformacje { get; set; }

        [MaxLength(450)]
        public string? GospodarzKlubId { get; set; }

        [ForeignKey("GospodarzKlubId")]
        public virtual ApplicationUser? GospodarzKlub { get; set; }

        [MaxLength(450)]
        public string? GoscKlubId { get; set; }

        [ForeignKey("GoscKlubId")]
        public virtual ApplicationUser? GoscKlub { get; set; }

        public virtual Rozgrywki? Rozgrywki { get; set; }
        public virtual Sedzia? SedziaI { get; set; }
        public virtual Sedzia? SedziaII { get; set; }
        public virtual Sedzia? SedziaSekretarz { get; set; }

     
        public virtual Sedzia? SedziaLiniowyI { get; set; }
        public virtual Sedzia? SedziaLiniowyII { get; set; }
        public virtual Sedzia? SedziaGlowny { get; set; }
    }


}
