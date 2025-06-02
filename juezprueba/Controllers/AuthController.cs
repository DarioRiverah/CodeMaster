using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using juezprueba.Models;
using juezprueba.ViewModels;
using System.Security.Claims;
using juezprueba.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;

namespace juezprueba.Controllers
{
    public class AuthController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        // ========== REGISTRO ==========
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Nombre = model.Nombre,
                    Apellido = model.Apellido
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    if (!user.Email.Equals("admin1@gmail.com", StringComparison.OrdinalIgnoreCase))
                        await _userManager.AddToRoleAsync(user, "Usuario");

                    // Generar token de confirmación
                    var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var tokenBytes = Encoding.UTF8.GetBytes(token);
                    var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);

                    var confirmationLink = Url.Action(
                        "ConfirmEmail",
                        "Auth",
                        new { userId = user.Id, token = encodedToken },
                        protocol: HttpContext.Request.Scheme);

                    await _emailSender.SendEmailWithTemplateAsync(
                        user.Email,
                        "Confirma tu correo",
                        "email-confirmation",
                        new
                        {
                            Nombre = user.Nombre,
                            Link = confirmationLink,
                            AppName = "CodeMaster",
                            Year = DateTime.Now.Year
                        });

                    return View("ConfirmEmailSent");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // ========== CONFIRMAR CORREO ==========
        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return RedirectToAction("Index", "Home");

            // DECODIFICAR EL TOKEN
            var decodedBytes = WebEncoders.Base64UrlDecode(token);
            token = Encoding.UTF8.GetString(decodedBytes);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound($"No se encontró el usuario con ID '{userId}'.");

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
                return View("EmailConfirmed");

            return View("Error");
        }



        // ========== LOGIN ==========
        public IActionResult Login() => View();

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);

                if (user != null && !user.EmailConfirmed)
                {
                    ModelState.AddModelError(string.Empty, "Debes confirmar tu correo antes de iniciar sesión.");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);
                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");

                ModelState.AddModelError(string.Empty, "Login inválido.");
            }
            return View(model);
        }

        // ========== LOGIN EXTERNO (Google) ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Auth", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");

            if (remoteError != null)
            {
                ModelState.AddModelError(string.Empty, $"Error del proveedor externo: {remoteError}");
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
            if (result.Succeeded)
                return LocalRedirect(returnUrl);

            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName) ?? "Nombre";
            var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname) ?? "Apellido";

            if (email == null)
            {
                ModelState.AddModelError(string.Empty, "No se pudo obtener el email del proveedor externo.");
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    Nombre = firstName,
                    Apellido = lastName,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                        ModelState.AddModelError(string.Empty, error.Description);
                    return RedirectToAction(nameof(Login));
                }

                await _userManager.AddToRoleAsync(user, "Usuario");
            }

            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, false);
            return LocalRedirect(returnUrl);
        }

        // ========== LOGOUT ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ResendEmailConfirmation()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ResendEmailConfirmation(ResendEmailConfirmationViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "No se encontró ningún usuario con ese correo.");
                return View(model);
            }

            if (user.EmailConfirmed)
            {
                ModelState.AddModelError(string.Empty, "El correo ya ha sido confirmado.");
                return View(model);
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);

            var confirmationLink = Url.Action(nameof(ConfirmEmail), "Auth", new { userId = user.Id, token = encodedToken }, Request.Scheme);

            await _emailSender.SendEmailWithTemplateAsync(
                user.Email,
                "Confirma tu correo",
                "email-confirmation",
                new
                {
                    Nombre = user.Nombre,
                    Link = confirmationLink,
                    AppName = "CodeMaster",
                    Year = DateTime.Now.Year
                });

            ViewBag.Message = "Correo de confirmación reenviado. Por favor revisa tu bandeja de entrada.";
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                // No revelar que el usuario no existe o no confirmó el email
                return View("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);

            var callbackUrl = Url.Action(nameof(ResetPassword), "Auth",
                new { token = encodedToken, email = model.Email },
                protocol: Request.Scheme);

            await _emailSender.SendEmailWithTemplateAsync(
                model.Email,
                "Restablece tu contraseña",
                "password-reset",
                new
                {
                    Nombre = user.Nombre,
                    Link = callbackUrl,
                    AppName = "CodeMaster",
                    Year = DateTime.Now.Year
                });

            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]   
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
                return RedirectToAction("Index", "Home");

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return RedirectToAction("ResetPasswordConfirmation");

            var decodedTokenBytes = WebEncoders.Base64UrlDecode(model.Token);
            var decodedToken = Encoding.UTF8.GetString(decodedTokenBytes);

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (result.Succeeded)
                return View("ResetPasswordConfirmation");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        // ========== CAMBIO DE CONTRASEÑA ==========
        [HttpGet]
        public async Task<IActionResult> ChangePassword()
        {
            var user = await _userManager.GetUserAsync(User);

            var hasPassword = await _userManager.HasPasswordAsync(user);
            if (!hasPassword)
            {
                TempData["Error"] = "Tu cuenta no tiene contraseña local, probablemente iniciaste sesión con un proveedor externo como Google.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.GetUserAsync(User);

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                ViewBag.Message = "Tu contraseña ha sido cambiada exitosamente.";
                return View();
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }


    }
}
