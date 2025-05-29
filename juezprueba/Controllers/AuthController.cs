using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using juezprueba.Models;
using juezprueba.ViewModels;
using System.Security.Claims;
using juezprueba.Services; // Asegúrate de importar tu servicio
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;

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

                    // Enviar correo de confirmación
                    await _emailSender.SendEmailAsync(user.Email, "Confirma tu correo",
                        $"Hola {user.Nombre},<br><br>Por favor confirma tu cuenta haciendo clic en el siguiente enlace:<br><a href='{HtmlEncoder.Default.Encode(confirmationLink)}'>Confirmar correo</a>");

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

            await _emailSender.SendEmailAsync(user.Email, "Confirma tu correo",
                        $"Hola {user.Nombre},<br><br>Por favor confirma tu cuenta haciendo clic en el siguiente enlace:<br><a href='{HtmlEncoder.Default.Encode(confirmationLink)}'>Confirmar correo</a>");

            ViewBag.Message = "Correo de confirmación reenviado. Por favor revisa tu bandeja de entrada.";
            return View();
        }

    }
}
