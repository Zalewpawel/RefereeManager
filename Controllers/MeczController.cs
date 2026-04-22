using Microsoft.AspNetCore.Mvc;
using Sedziowanie.Models;
using Sedziowanie.Services.Interfaces;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace Sedziowanie.Controllers
{
    public class MeczController : Controller
    {
        private readonly IMeczService _meczService;

        private readonly UserManager<ApplicationUser> _userManager;

        public MeczController(IMeczService meczService, UserManager<ApplicationUser> userManager)
        {
            _meczService = meczService;
            _userManager = userManager; 
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Add(DateTime? dataMeczu)
        {
            var data = dataMeczu ?? DateTime.Now;
            await PrzygotujListyAsync(data);
            return View(new Mecz { Data = data });
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Add(Mecz model)
        {
            if (model.RozgrywkiId == 0)
                ModelState.AddModelError(nameof(model.RozgrywkiId), "Rozgrywki są wymagane.");

            if (!ModelState.IsValid)
            {
                await PrzygotujListyAsync(model.Data);
                return View(model);
            }

            await _meczService.AddMecz(model.NumerMeczu, model.Data, model.RozgrywkiId,
                                       model.Gospodarz, model.Gosc,
                                       model.SedziaIId, model.SedziaIIId, model.SedziaSekretarzId,
                                       model.SedziaLiniowyIId, model.SedziaLiniowyIIId, model.SedziaGlownyId,
                                       model.Turniej, model.Adres, model.DodatkoweInformacje,
                                       model.GospodarzKlubId, model.GoscKlubId);

            return RedirectToAction(nameof(ListaMeczowAdmin));
        }

        private async Task PrzygotujListyAsync(DateTime data)
        {
            var sedziowie = _meczService.GetSedziowieByDateWithStatus(data).ToList();

            ViewBag.Rozgrywki = _meczService.GetRozgrywki();

            ViewBag.DataMeczu = data.ToString("yyyy-MM-ddTHH:mm");

            ViewData["SedziowieJson"] = JsonSerializer.Serialize(
                sedziowie,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );

            // only users in role 'User' for klub selects
            var clubUsers = await _userManager.GetUsersInRoleAsync("User");
            ViewBag.ClubUsersSelect = clubUsers
                .OrderBy(u => (u.Nazwisko ?? "").Trim())
                .ThenBy(u => (u.Imie ?? "").Trim())
                .Select(u => new SelectListItem { Value = u.Id, Text = string.IsNullOrWhiteSpace(u.Imie) && string.IsNullOrWhiteSpace(u.Nazwisko) ? u.UserName : ($"{u.Imie} {u.Nazwisko}") })
                .ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetSedziowieByDate(DateTime data)
        {
            var sedziowie = _meczService.GetSedziowieByDateWithStatus(data)
                .Select(s => new
                {
                    id = s.Id,
                    imie = s.Imie,
                    nazwisko = s.Nazwisko,
                    klasa = s.Klasa,
                    miasto = s.Miasto,
                    status = s.Status,
                    maMeczTegoDnia = s.MaMeczTegoDnia
                });
            return new JsonResult(sedziowie);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult ListaMeczowAdmin()
        {
            var mecze = _meczService.GetAllMecze();
            return View(mecze);
        }

        public IActionResult ListaMeczow()
        {
            var mecze = _meczService.GetAllMecze();
            return View(mecze);
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUdostepnijGodzine(int id, bool value, string? returnUrl)
        {
            await _meczService.SetUdostepnijGodzineAsync(id, value);
            return Redirect(returnUrl ?? Url.Action(nameof(ListaMeczowAdmin))!);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var mecz = _meczService.GetMeczById(id);
            if (mecz == null) return NotFound();

            await PrzygotujListyEditAsync(mecz.Data);
            return View(mecz);
        }
        private async Task PrzygotujListyEditAsync(DateTime data)
        {
            var sedziowie = _meczService.GetSedziowieByDateWithStatus(data).ToList();

            ViewBag.Rozgrywki = _meczService.GetRozgrywki();
            ViewBag.DataMeczu = data.ToString("yyyy-MM-ddTHH:mm");

            ViewData["SedziowieJson"] = JsonSerializer.Serialize(
                sedziowie,
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );

            var clubUsers = await _userManager.GetUsersInRoleAsync("User");
            ViewBag.ClubUsersSelect = clubUsers
                .OrderBy(u => (u.Nazwisko ?? "").Trim())
                .ThenBy(u => (u.Imie ?? "").Trim())
                .Select(u => new SelectListItem { Value = u.Id, Text = string.IsNullOrWhiteSpace(u.Imie) && string.IsNullOrWhiteSpace(u.Nazwisko) ? u.UserName : ($"{u.Imie} {u.Nazwisko}") })
                .ToList();
        }
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Edit(Mecz model)
        {
            if (!ModelState.IsValid)
            {
                await PrzygotujListyAsync(model.Data);
                return View(model);
            }

            await _meczService.UpdateMecz(model);
            return RedirectToAction(nameof(ListaMeczowAdmin));
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Delete(int id)
        {
            var mecz = _meczService.GetMeczById(id);

            if (mecz == null)
            {
                return NotFound();
            }

            return View(mecz);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            _meczService.DeleteMecz(id);
            return RedirectToAction("ListaMeczowAdmin");
        }

        
        [Authorize(Roles = "Sedzia")]
        [HttpGet]
        public async Task<IActionResult> MojeMecze()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.SedziaId is null)
            {
                TempData["Error"] = "Twoje konto nie jest powiązane z profilem sędziego.";
                return RedirectToAction("ListaMeczow", "Mecz");
            }

            return RedirectToAction(
                actionName: "MeczeSedziego",
                controllerName: "Sedzia",
                routeValues: new { sedziaId = user.SedziaId.Value }
            );
        }

        [HttpGet]
        public IActionResult Informacje(int id)
        {
            var mecz = _meczService.GetMeczById(id);
            if (mecz == null) return NotFound();

            return View(mecz);
        }

        [Authorize(Roles = "User")]
        [HttpGet]
        public async Task<IActionResult> MeczeKlubu()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var mecze = _meczService.GetAllMecze()
                .Where(m => m.GospodarzKlubId == user.Id || m.GoscKlubId == user.Id)
                .ToList();

            return View(mecze);
        }
    }
}
