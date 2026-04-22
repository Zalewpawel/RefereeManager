using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sedziowanie.Models;
using Sedziowanie.Services.Interfaces;

namespace Sedziowanie.Controllers
{
    public class RozgrywkiController : Controller
    {
        private readonly IRozgrywkiService _rozgrywkiService;

        public RozgrywkiController(IRozgrywkiService rozgrywkiService)
        {
            _rozgrywkiService = rozgrywkiService;
        }

        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Add(Rozgrywki rozgrywki)
        {
            if (!ModelState.IsValid)
            {
                return View(rozgrywki);
            }

            _rozgrywkiService.AddRozgrywki(rozgrywki);
            return RedirectToAction("ListaRozgrywek");
        }

        public IActionResult ListaRozgrywek()
        {
            var rozgrywki = _rozgrywkiService.GetAllRozgrywki();
            return View(rozgrywki);
        }

        [HttpGet]
        public IActionResult MeczeRozgrywek(int rozgrywkiId)
        {
            var mecze = _rozgrywkiService.GetMeczeForRozgrywki(rozgrywkiId);
            ViewBag.Rozgrywka = _rozgrywkiService.GetRozgrywkaName(rozgrywkiId);
            return View(mecze);
        }
        [Authorize(Roles = "Admin")]
        public IActionResult ListaRozgrywekAdmin()
        {
            var rozgrywki = _rozgrywkiService.GetAllRozgrywki();
            return View("ListaRozgrywekAdmin", rozgrywki);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult MeczeRozgrywekAdmin(int rozgrywkiId)
        {
            var mecze = _rozgrywkiService.GetMeczeForRozgrywki(rozgrywkiId);
            ViewBag.Rozgrywka = _rozgrywkiService.GetRozgrywkaName(rozgrywkiId);
            return View(mecze);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            var model = _rozgrywkiService.GetById(id);
            if (model == null) return NotFound();
            return View(model); // widok Edit.cshtml dla Rozgrywki
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, Sedziowanie.Models.Rozgrywki model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            _rozgrywkiService.Update(model);
            return RedirectToAction(nameof(ListaRozgrywekAdmin));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            _rozgrywkiService.Delete(id);
            return RedirectToAction(nameof(ListaRozgrywekAdmin));
        }
    }
}
