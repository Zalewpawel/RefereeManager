using System.ComponentModel.DataAnnotations;

namespace Sedziowanie.Models
{
    public class Komisja
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nazwa { get; set; } = string.Empty;

        public ICollection<KomisjaCzlonek> Czlonkowie { get; set; } = new List<KomisjaCzlonek>();
    }
}
