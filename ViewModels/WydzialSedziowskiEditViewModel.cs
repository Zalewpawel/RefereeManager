using Microsoft.AspNetCore.Mvc.Rendering;
using Sedziowanie.Models;

namespace Sedziowanie.ViewModels
{
    public class WydzialSedziowskiEditViewModel
    {
        public List<Komisja> Komisje { get; set; } = new();
        public List<SelectListItem> Sedziowie { get; set; } = new();

        public string? NowaKomisjaNazwa { get; set; }
        public int KomisjaId { get; set; }
        public int SedziaId { get; set; }
        public string Funkcja { get; set; } = "Członek";
        public string? Email { get; set; }
    }
}
