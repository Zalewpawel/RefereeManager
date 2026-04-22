using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sedziowanie.Models;

namespace Sedziowanie.ViewModels
{
    public class RegisterViewModel
    {
        [Display(Name = "Sędzia")]
        public int? SedziaId { get; set; }

        [Required]
        [Display(Name = "Rola")]
        public string Role { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Hasło")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Hasła nie są identyczne.")]
        [Display(Name = "Potwierdź hasło")]
        public string ConfirmPassword { get; set; }

        public List<Sedzia> AvailableSedziowie { get; set; } = new List<Sedzia>();

        [Display(Name = "Imię")]
        public string Imie { get; set; }

        [Display(Name = "Nazwisko")]
        public string Nazwisko { get; set; }
    }
}
