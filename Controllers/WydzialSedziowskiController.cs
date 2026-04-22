using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sedziowanie.Data;
using Sedziowanie.Models;
using Sedziowanie.Services.Extensions;
using Sedziowanie.ViewModels;

namespace Sedziowanie.Controllers
{
    public class WydzialSedziowskiController : Controller
    {
        private readonly DBObsadyContext _context;

        public WydzialSedziowskiController(DBObsadyContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Sklad()
        {
            var komisje = _context.Komisje
                .Include(k => k.Czlonkowie)
                    .ThenInclude(c => c.Sedzia)
                .AsNoTracking()
                .ToList();

            komisje = SortKomisjeAndMembers(komisje);

            return View(komisje);
        }

        [HttpGet]
        public IActionResult Sukcesy()
        {
            var sukcesy = _context.SukcesyWydzialu
                .AsNoTracking()
                .OrderByDescending(s => s.Id)
                .ToList();

            return View(sukcesy);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddSukces(string zawody, string osiagniecie, string sklad)
        {
            if (string.IsNullOrWhiteSpace(zawody) || string.IsNullOrWhiteSpace(osiagniecie) || string.IsNullOrWhiteSpace(sklad))
            {
                TempData["Error"] = "Uzupełnij wszystkie pola sukcesu.";
                return RedirectToAction(nameof(Sukcesy));
            }

            _context.SukcesyWydzialu.Add(new SukcesWydzialu
            {
                Zawody = zawody.Trim(),
                Osiagniecie = osiagniecie.Trim(),
                Sklad = sklad.Trim()
            });

            _context.SaveChanges();
            TempData["Success"] = "Dodano sukces.";
            return RedirectToAction(nameof(Sukcesy));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Edit()
        {
            return View(BuildEditViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddKomisja(WydzialSedziowskiEditViewModel vm)
        {
            if (string.IsNullOrWhiteSpace(vm.NowaKomisjaNazwa))
            {
                TempData["Error"] = "Podaj nazwę komisji.";
                return RedirectToAction(nameof(Edit));
            }

            var nazwa = vm.NowaKomisjaNazwa.Trim();
            var exists = _context.Komisje.Any(k => k.Nazwa == nazwa);
            if (exists)
            {
                TempData["Error"] = "Komisja o tej nazwie już istnieje.";
                return RedirectToAction(nameof(Edit));
            }

            _context.Komisje.Add(new Komisja { Nazwa = nazwa });
            _context.SaveChanges();

            TempData["Success"] = "Dodano komisję.";
            return RedirectToAction(nameof(Edit));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCzlonek(WydzialSedziowskiEditViewModel vm)
        {
            var komisja = _context.Komisje.FirstOrDefault(k => k.Id == vm.KomisjaId);
            var sedzia = _context.Sedziowie.FirstOrDefault(s => s.Id == vm.SedziaId);

            if (komisja == null || sedzia == null)
            {
                TempData["Error"] = "Nieprawidłowa komisja lub sędzia.";
                return RedirectToAction(nameof(Edit));
            }

            var funkcja = string.IsNullOrWhiteSpace(vm.Funkcja) ? "Członek" : vm.Funkcja.Trim();
            var email = string.IsNullOrWhiteSpace(vm.Email) ? sedzia.Email : vm.Email.Trim();

            _context.KomisjaCzlonkowie.Add(new KomisjaCzlonek
            {
                KomisjaId = komisja.Id,
                SedziaId = sedzia.Id,
                Funkcja = funkcja,
                Email = email
            });

            _context.SaveChanges();
            TempData["Success"] = "Dodano członka komisji.";
            return RedirectToAction(nameof(Edit));
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCzlonek(int id)
        {
            var item = _context.KomisjaCzlonkowie.FirstOrDefault(x => x.Id == id);
            if (item != null)
            {
                _context.KomisjaCzlonkowie.Remove(item);
                _context.SaveChanges();
                TempData["Success"] = "Usunięto członka komisji.";
            }

            return RedirectToAction(nameof(Edit));
        }

        private WydzialSedziowskiEditViewModel BuildEditViewModel()
        {
            var komisje = _context.Komisje
                .Include(k => k.Czlonkowie)
                    .ThenInclude(c => c.Sedzia)
                .AsNoTracking()
                .ToList();

            komisje = SortKomisjeAndMembers(komisje);

            var sedziowie = _context.Sedziowie
                .AsNoTracking()
                .OrderByNazwiskoImie()
                .Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Nazwisko + " " + s.Imie
                })
                .ToList();

            return new WydzialSedziowskiEditViewModel
            {
                Komisje = komisje,
                Sedziowie = sedziowie
            };
        }

        private static List<Komisja> SortKomisjeAndMembers(List<Komisja> komisje)
        {
            var preferredOrder = new List<string>
            {
                "Wydział Sędziowski",
                "Komisja Szkolenia i Kwalifikacji",
                "Referat Obsad",
                "Komisja Dyscypliny i Etyki",
                "Komisja Szkolenia i Obsad Piłki Siatkowej Plażowej"
            };

            var orderedKomisje = komisje
                .OrderBy(k =>
                {
                    var idx = preferredOrder.FindIndex(x => string.Equals(x, k.Nazwa, StringComparison.OrdinalIgnoreCase));
                    return idx < 0 ? int.MaxValue : idx;
                })
                .ThenBy(k => k.Nazwa)
                .ToList();

            foreach (var komisja in orderedKomisje)
            {
                komisja.Czlonkowie = komisja.Czlonkowie
                    .OrderBy(c => RoleRank(c.Funkcja))
                    .ThenBy(c => c.Sedzia?.Nazwisko)
                    .ThenBy(c => c.Sedzia?.Imie)
                    .ToList();
            }

            return orderedKomisje;
        }

        private static int RoleRank(string? funkcja)
        {
            if (string.IsNullOrWhiteSpace(funkcja)) return 2;

            var value = funkcja.Trim();

            if (value.StartsWith("Przewodniczący", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Przewodniczacy", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            if (value.StartsWith("Członek", StringComparison.OrdinalIgnoreCase) ||
                value.StartsWith("Czlonek", StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            return 2;
        }
    }
}
