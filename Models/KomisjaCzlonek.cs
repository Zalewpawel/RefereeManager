using System.ComponentModel.DataAnnotations;

namespace Sedziowanie.Models
{
    public class KomisjaCzlonek
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KomisjaId { get; set; }
        public Komisja Komisja { get; set; } = null!;

        [Required]
        public int SedziaId { get; set; }
        public Sedzia Sedzia { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string Funkcja { get; set; } = "Członek";

        [MaxLength(150)]
        public string? Email { get; set; }
    }
}
