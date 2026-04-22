using Sedziowanie.Models;
using System.ComponentModel.DataAnnotations;

namespace Sedziowanie.Models
{
    public class Rozgrywki
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nazwa { get; set; }
        public ICollection<Mecz> Mecze { get; set; } = new List<Mecz>();
    }
}
