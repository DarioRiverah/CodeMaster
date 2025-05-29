using juezprueba.Context;
using juezprueba.Models;
using juezprueba.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace juezprueba.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AppDbContext _context;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsPrincipalFactory;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        public ProfileController(
            UserManager<ApplicationUser> userManager,
            AppDbContext context,
            IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache)
        {
            _userManager = userManager;
            _context = context;
            _userClaimsPrincipalFactory = userClaimsPrincipalFactory;
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        public async Task<IActionResult> Edit()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var cacheKey = $"Perfil_{user.Id}";
            if (!_cache.TryGetValue(cacheKey, out PerfilUsuario perfil))
            {
                perfil = await _context.PerfilesUsuario
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);

                _cache.Set(cacheKey, perfil, TimeSpan.FromMinutes(10));
            }

            return View(new EditarPerfilViewModel
            {
                Nombre = user.Nombre,
                Apellido = user.Apellido,
                Biografia = perfil?.Biografia,
                Direccion = perfil?.Direccion,
                ImagenActualUrl = perfil?.ImagenPerfilUrl,
                FechaNacimiento = perfil?.FechaNacimiento ?? DateTime.MinValue
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditarPerfilViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Actualizar usuario
                user.Nombre = model.Nombre;
                user.Apellido = model.Apellido;
                await _userManager.UpdateAsync(user);

                // Manejar perfil
                var perfil = await _context.PerfilesUsuario
                    .FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id)
                    ?? new PerfilUsuario { ApplicationUserId = user.Id };

                perfil.Biografia = model.Biografia;
                perfil.Direccion = model.Direccion;
                perfil.FechaNacimiento = model.FechaNacimiento;

                // Subir imagen si es nueva
                if (model.ImagenPerfil != null && model.ImagenPerfil.Length > 0)
                {
                    perfil.ImagenPerfilUrl = await UploadImageToImgBB(model.ImagenPerfil)
                        ?? perfil.ImagenPerfilUrl;
                }

                if (perfil.Id == 0) _context.Add(perfil);
                await _context.SaveChangesAsync();

                // Actualizar claims (incluyendo nombre y apellido)
                await UpdateUserClaims(user, perfil.ImagenPerfilUrl);

                await transaction.CommitAsync();
                _cache.Remove($"Perfil_{user.Id}");

                TempData["SuccessMessage"] = "Perfil actualizado correctamente";
                return RedirectToAction("Edit");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Error al guardar. Intente nuevamente.");
                return View(model);
            }
        }

        private async Task<string> UploadImageToImgBB(IFormFile imageFile)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(15);

                using var form = new MultipartFormDataContent();
                await using var imageStream = imageFile.OpenReadStream();

                form.Add(new StreamContent(imageStream), "image", imageFile.FileName);

                var response = await httpClient.PostAsync(
                    "https://api.imgbb.com/1/upload?key=487afc84fe6511e96d614ad56e258577",
                    form);

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
                return json.RootElement.GetProperty("data").GetProperty("url").GetString();
            }
            catch
            {
                return null;
            }
        }

        private async Task UpdateUserClaims(ApplicationUser user, string imageUrl)
        {
            // Obtener todos los claims actuales
            var existingClaims = await _userManager.GetClaimsAsync(user);

            // Eliminar claims que vamos a actualizar
            var claimsToRemove = existingClaims
                .Where(c => c.Type == "ImagenPerfilUrl" || c.Type == "Nombre" || c.Type == "Apellido")
                .ToList();

            foreach (var claim in claimsToRemove)
            {
                await _userManager.RemoveClaimAsync(user, claim);
            }

            // Agregar claims actualizados
            var newClaims = new List<Claim>
            {
                new Claim("Nombre", user.Nombre ?? ""),
                new Claim("Apellido", user.Apellido ?? ""),
                new Claim("ImagenPerfilUrl",
                    imageUrl ?? "https://cdn.pixabay.com/photo/2015/10/05/22/37/blank-profile-picture-973460_960_720.png")
            };

            await _userManager.AddClaimsAsync(user, newClaims);

            // Refrescar autenticación para que los cambios surtan efecto inmediato
            var principal = await _userClaimsPrincipalFactory.CreateAsync(user);
            await HttpContext.SignInAsync(IdentityConstants.ApplicationScheme, principal);
        }
    }
}