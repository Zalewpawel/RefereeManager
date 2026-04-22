using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sedziowanie.Data;
using Sedziowanie.Models;
using Sedziowanie.Services.Extensions;
using Sedziowanie.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Linq;

namespace Sedziowanie.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly DBObsadyContext _context;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, DBObsadyContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
        }


        [HttpGet]
        public IActionResult Register()
        {
            var model = new RegisterViewModel
            {
                AvailableSedziowie = _context.Sedziowie
                    .AsNoTracking()
                    .OrderByNazwiskoImie()
                    .ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, EmailConfirmed = true };

                if (model.Role == "Sedzia" && model.SedziaId.HasValue)
                {
                    var sedzia = _context.Sedziowie.Find(model.SedziaId.Value);
                    if (sedzia != null)
                    {
                        user.Imie = sedzia.Imie;
                        user.Nazwisko = sedzia.Nazwisko;
                        user.Email = sedzia.Email;
                        user.SedziaId = sedzia.Id;
                    }
                }
                else
                {
                    user.Imie = model.Imie;
                    user.Nazwisko = model.Nazwisko;
                }

                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, model.Role);
                    return RedirectToAction("ShowAll", "Sedzia");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            model.AvailableSedziowie = _context.Sedziowie
                .AsNoTracking()
                .OrderByNazwiskoImie()
                .ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            if (TempData["Success"] is string successMessage)
            {
                ViewData["Success"] = successMessage;
            }

            if (TempData["LoginError"] is string errorMessage)
            {
                ViewData["LoginError"] = errorMessage;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (!result.Succeeded)
                {
                    ViewData["LoginError"] = "Nieprawidłowy email lub hasło.";
                    return View(model);
                }
                else
                {
                    return RedirectToAction("Index", "Start");
                }

               
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("ShowBezDanych", "Sedzia");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult GetSedziaData(int sid)
        {
            var sedzia = _context.Sedziowie
                .Where(s => s.Id == sid)
                .Select(s => new { s.Imie, s.Nazwisko, s.Email })
                .FirstOrDefault();

            if (sedzia == null)
            {
                return NotFound();
            }

            return Json(sedzia);
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(bool forced = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.Forced = forced || user.MustChangePassword;
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            ViewBag.Forced = user.MustChangePassword;

            if (!ModelState.IsValid) return View(model);

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                return View(model);
            }

            if (user.MustChangePassword)
            {
                user.MustChangePassword = false;
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            TempData["Success"] = "Hasło zostało zmienione.";
            return RedirectToAction(nameof(ChangePassword));
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordByAdmin(string userId, string token)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
            {
                TempData["LoginError"] = "Link resetujący jest nieprawidłowy.";
                return RedirectToAction(nameof(Login));
            }

            var model = new AdminResetPasswordViewModel
            {
                UserId = userId,
                Token = token
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPasswordByAdmin(AdminResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                TempData["LoginError"] = "Link resetujący jest nieprawidłowy lub wygasł.";
                return RedirectToAction(nameof(Login));
            }

            model.Token = model.Token?.Replace(" ", "+");
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(model);
            }

            TempData["Success"] = "Nowe hasło zostało ustawione. Możesz się zalogować.";
            return RedirectToAction(nameof(Login));
        }

    }
}
