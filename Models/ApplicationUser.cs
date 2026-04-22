using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sedziowanie.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? Imie { get; set; }
        public string? Nazwisko { get; set; }
        public int? SedziaId { get; set; }

        [ForeignKey("SedziaId")]
        public virtual Sedzia? Sedzia { get; set; }

        public bool MustChangePassword { get; set; } = false;
    }
}
