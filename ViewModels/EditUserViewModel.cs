using System.ComponentModel.DataAnnotations;

namespace Sedziowanie.ViewModels
{
    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [Display(Name = "Imię")]
        public string Imie { get; set; }

        [Required]
        [Display(Name = "Nazwisko")]
        public string Nazwisko { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Rola")]
        public string Role { get; set; }

        public List<string> AvailableRoles { get; set; } = new List<string>();
    }
}
