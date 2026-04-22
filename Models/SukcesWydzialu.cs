using System.ComponentModel.DataAnnotations;

namespace Sedziowanie.Models
{
    public class SukcesWydzialu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Zawody { get; set; } = string.Empty;

        [Required]
        [MaxLength(300)]
        public string Osiagniecie { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Sklad { get; set; } = string.Empty;
    }
}
