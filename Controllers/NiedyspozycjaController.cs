using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Sedziowanie.Models;
using Sedziowanie.Services.Interfaces;
using Sedziowanie.ViewModels;
using System;

namespace Sedziowanie.Controllers
{
    public class NiedyspozycjaController : Controller
    {
        private readonly INiedyspozycjaService _niedyspozycjaService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NiedyspozycjaController(
            INiedyspozycjaService niedyspozycjaService,
            UserManager<ApplicationUser> userManager)
        {
            _niedyspozycjaService = niedyspozycjaService;
            _userManager = userManager;
        }

        
        [HttpGet]
        public IActionResult Show()
        {
            var niedyspozycje = _niedyspozycjaService.GetAllNiedyspozycje();
            return View(niedyspozycje);
        }

        [HttpGet]
        public async Task<IActionResult> AddForSedzia()
        {
            var user = await _userManager.GetUserAsync(User);
            var sedzia = user != null ? _niedyspozycjaService.GetSedziaByUserId(user.Id) : null;
            if (sedzia == null)
            {
                TempData["Error"] = "Twoje konto nie jest powiązane z profilem sędziego.";
                return RedirectToAction("Show"); // lub gdzie chcesz
            }

            ViewBag.SedziaId = sedzia.Id;
            return View(new NiedyspozycjaCreateViewModel { IsAllDay = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddForSedzia(NiedyspozycjaCreateViewModel vm)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var sedzia = user != null ? _niedyspozycjaService.GetSedziaByUserId(user.Id) : null;
                if (sedzia == null)
                {
                    TempData["Error"] = "Twoje konto nie jest powiązane z profilem sędziego.";
                    return RedirectToAction("Show");
                }

                DateTime poczatek, koniec;

                if (vm.IsAllDay)
                {
                    if (vm.StartDate is null || vm.EndDate is null)
                    {
                        ModelState.AddModelError("", "Podaj datę początku i końca.");
                        ViewBag.SedziaId = sedzia.Id;
                        return View(vm);
                    }

                    poczatek = vm.StartDate.Value.Date;
                   
                    koniec = vm.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                }
                else
                {
                    if (vm.StartDateTime is null || vm.EndDateTime is null)
                    {
                        ModelState.AddModelError("", "Podaj datę i godzinę początku i końca.");
                        ViewBag.SedziaId = sedzia.Id;
                        return View(vm);
                    }

                    poczatek = vm.StartDateTime.Value;
                    koniec = vm.EndDateTime.Value;
                }

                if (koniec < poczatek)
                {
                    ModelState.AddModelError("", "Data końca nie może być wcześniejsza niż początek.");
                    ViewBag.SedziaId = sedzia.Id;
                    return View(vm);
                }

                _niedyspozycjaService.AddNiedyspozycja(sedzia.Id, poczatek, koniec);

                TempData["Success"] = "Dodano pomyślnie";
                return RedirectToAction(nameof(AddForSedzia));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);

                var user = await _userManager.GetUserAsync(User);
                var sedzia = user != null ? _niedyspozycjaService.GetSedziaByUserId(user.Id) : null;
                ViewBag.SedziaId = sedzia?.Id;

                return View(vm);
            }
        }


        [Authorize(Roles = "Sedzia")]
        [HttpGet]
        public async Task<IActionResult> MojeNiedyspozycje()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SedziaId is null)
            {
                TempData["Error"] = "Twoje konto nie jest powiązane z profilem sędziego.";
                return RedirectToAction("Show"); 
            }

            var model = _niedyspozycjaService.GetForSedzia(user.SedziaId.Value);
            return View(model); 
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult AddForAdmin()
        {
            ViewBag.Sedziowie = _niedyspozycjaService.GetSedziowieList();
            return View("AddForAdmin");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddForAdmin(int sedziaId, DateTime poczatek, DateTime koniec)
        {
            try
            {
                _niedyspozycjaService.AddNiedyspozycja(sedziaId, poczatek, koniec);
                TempData["Success"] = "Niedyspozycja dodana.";
                return RedirectToAction(nameof(Show));
            }
            catch (ArgumentException ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.Sedziowie = _niedyspozycjaService.GetSedziowieList();
                return View("AddForAdmin");
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
          
            var ok = _niedyspozycjaService.DeleteNiedyspozycja(id);

            if (!ok)
                TempData["Error"] = "Niedyspozycja nie istnieje lub nie mogła zostać usunięta.";
            else
                TempData["Success"] = "Niedyspozycja została usunięta.";

            return RedirectToAction(nameof(Show));
        }
    }
}


