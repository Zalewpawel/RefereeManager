using System.ComponentModel.DataAnnotations;

namespace Sedziowanie.ViewModels
{
    public class AdminResetPasswordViewModel
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        public string Token { get; set; }

        [Required(ErrorMessage = "Podaj nowe hasło.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nowe hasło")]
        [StringLength(100, ErrorMessage = "Hasło musi mieć co najmniej {2} znaków.", MinimumLength = 6)]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Potwierdź nowe hasło.")]
        [DataType(DataType.Password)]
        [Display(Name = "Potwierdź nowe hasło")]
        [Compare(nameof(NewPassword), ErrorMessage = "Hasła nie są takie same.")]
        public string ConfirmPassword { get; set; }
    }
}
