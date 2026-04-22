using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Sedziowanie.Models;
using Sedziowanie.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace Sedziowanie.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _role_manager;
        private readonly IEmailService _emailService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IEmailService emailService,
            ILogger<AdminController> logger)
        {
            _userManager = userManager;
            _role_manager = roleManager;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ShowUsers()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserViewModel
                {
                    Id = user.Id,
                    Imie = user.Imie,
                    Nazwisko = user.Nazwisko,
                    Email = user.Email,
                    Role = roles.FirstOrDefault() ?? "Brak roli"
                });
            }

            var ordered = userList
                .OrderBy(u => u.Role)
                .ThenBy(u => u.Nazwisko)
                .ThenBy(u => u.Imie)
                .ToList();

            return View(ordered);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var roles = _role_manager.Roles.Select(r => r.Name).ToList();

            var model = new EditUserViewModel
            {
                Id = user.Id,
                Imie = user.Imie,
                Nazwisko = user.Nazwisko,
                Email = user.Email,
                Role = userRoles.FirstOrDefault(),
                AvailableRoles = roles
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Imie = model.Imie;
                user.Nazwisko = model.Nazwisko;
                user.Email = model.Email;

                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, userRoles);
                }

                await _userManager.AddToRoleAsync(user, model.Role);

                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("ShowUsers");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            model.AvailableRoles = _role_manager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                return RedirectToAction("ShowUsers");
            }

            // Fallback: build full user list with roles and return ordered by Role, Nazwisko, Imie
            var users = _userManager.Users.ToList();
            var userList = new List<UserViewModel>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                userList.Add(new UserViewModel
                {
                    Id = u.Id,
                    Imie = u.Imie,
                    Nazwisko = u.Nazwisko,
                    Email = u.Email,
                    Role = roles.FirstOrDefault() ?? "Brak roli"
                });
            }

            var ordered = userList
                .OrderBy(u => u.Role)
                .ThenBy(u => u.Nazwisko)
                .ThenBy(u => u.Imie)
                .ToList();

            return View("ShowUsers", ordered);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceChangePassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Nie znaleziono użytkownika.";
                return RedirectToAction(nameof(ShowUsers));
            }

            user.MustChangePassword = true;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"Użytkownik {user.Imie} {user.Nazwisko} będzie musiał zmienić hasło przy następnym logowaniu.";
                _logger.LogInformation("Admin {Admin} forced password change for user {UserId}", User.Identity?.Name, id);
            }
            else
            {
                TempData["Error"] = "Nie udało się ustawić flagi zmiany hasła.";
            }

            return RedirectToAction(nameof(ShowUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForceChangePasswordAll()
        {
            var users = _userManager.Users.ToList();
            int count = 0;
            foreach (var user in users)
            {
                user.MustChangePassword = true;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded) count++;
            }

            TempData["Success"] = $"Ustawiono wymuszoną zmianę hasła dla {count} użytkowników.";
            _logger.LogInformation("Admin {Admin} forced password change for all {Count} users", User.Identity?.Name, count);
            return RedirectToAction(nameof(ShowUsers));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForcePasswordReset(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["Error"] = "Nie udało się wymusić resetu hasła.";
                return RedirectToAction(nameof(ShowUsers));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "Nie udało się wymusić resetu hasła.";
                _logger.LogWarning("Admin attempted password reset for missing user id {UserId}", id);
                return RedirectToAction(nameof(ShowUsers));
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                TempData["Error"] = "Użytkownik nie ma przypisanego adresu e-mail.";
                return RedirectToAction(nameof(ShowUsers));
            }

            if (await _userManager.HasPasswordAsync(user))
            {
                var removePasswordResult = await _userManager.RemovePasswordAsync(user);
                if (!removePasswordResult.Succeeded)
                {
                    TempData["Error"] = "Nie udało się wymusić resetu hasła.";
                    _logger.LogWarning("RemovePasswordAsync failed for user {UserId}: {Errors}",
                        user.Id,
                        string.Join("; ", removePasswordResult.Errors.Select(e => e.Description)));
                    return RedirectToAction(nameof(ShowUsers));
                }
            }

            await _userManager.UpdateSecurityStampAsync(user);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetUrl = Url.Action(
                "ResetPasswordByAdmin",
                "Account",
                new { userId = user.Id, token },
                Request.Scheme);

            if (string.IsNullOrWhiteSpace(resetUrl))
            {
                TempData["Error"] = "Nie udało się wygenerować linku resetu hasła.";
                return RedirectToAction(nameof(ShowUsers));
            }

            var emailBody = $@"
                <p>Dzień dobry,</p>
                <p>Administrator wymusił reset Twojego hasła.</p>
                <p>Aby ustawić nowe hasło, kliknij poniższy link:</p>
                <p><a href='{resetUrl}'>Ustaw nowe hasło</a></p>
                <p>Jeśli nie rozpoznajesz tej operacji, skontaktuj się z administratorem.</p>";

            try
            {
                await _emailService.SendEmailAsync(user.Email, "Wymuszony reset hasła", emailBody, true);
                TempData["Success"] = "Wysłano e-mail z linkiem do ustawienia nowego hasła.";
                _logger.LogInformation("Admin {AdminName} forced password reset for user {UserId}", User.Identity?.Name, user.Id);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Hasło zostało unieważnione, ale nie udało się wysłać e-maila resetującego.";
                _logger.LogError(ex, "Sending forced reset email failed for user {UserId}", user.Id);
            }

            return RedirectToAction(nameof(ShowUsers));
        }
    }
}
