using System.ComponentModel.DataAnnotations;

namespace Sedziowanie.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Podaj obecne hasło.")]
        [DataType(DataType.Password)]
        [Display(Name = "Obecne hasło")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Podaj nowe hasło.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        [MinLength(10, ErrorMessage = "Hasło musi mieć co najmniej 10 znaków.")]
        [RegularExpression(
            @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{10,}$",
            ErrorMessage = "Hasło musi zawierać: wielką literę, małą literę, cyfrę i znak specjalny.")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Potwierdź nowe hasło.")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        [Compare(nameof(NewPassword), ErrorMessage = "Hasła nie są takie same.")]
        public string ConfirmPassword { get; set; }
    }
}
