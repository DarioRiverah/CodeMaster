using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using juezprueba.Models;
using juezprueba.Context;
using juezprueba.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace juezprueba.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public HomeController(
        ILogger<HomeController> logger,
        AppDbContext context,
        UserManager<ApplicationUser> userManager) // Añade este parámetro
    {
        _logger = logger;
        _context = context;
        _userManager = userManager; // Asigna la instancia
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult Nosotros()
    {
        return View();
    }

    public IActionResult PreguntasFrecuentes()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult ListaProblema()
    {
        return RedirectToAction("Index", "Problemas");
    }

    // ?? Este es el método nuevo
    public async Task<IActionResult> Ranking()
    {
        var ahora = DateTime.UtcNow;
        var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

        // Primero obtenemos los IDs de usuarios con sus puntos
        var usuariosConPuntos = await _context.ProblemasResueltos
            .Where(pr => pr.FechaResolucion >= inicioMes)
            .GroupBy(pr => pr.UsuarioId)
            .Select(g => new
            {
                UsuarioId = g.Key,
                PuntosTotales = g.Sum(pr => pr.Problema.Puntos)
            })
            .OrderByDescending(r => r.PuntosTotales)
            .ToListAsync();

        // Obtenemos todos los usuarios necesarios en una sola consulta
        var usuariosIds = usuariosConPuntos.Select(x => x.UsuarioId).ToList();
        var usuariosInfo = await _context.Users
            .Where(u => usuariosIds.Contains(u.Id))
            .Select(u => new
            {
                u.Id,
                NombreCompleto = u.Nombre + " " + u.Apellido,
                u.UserName
            })
            .ToListAsync();

        // Construimos el ranking final
        var rankingFinal = new List<juezprueba.ViewModels.RankingViewModel>();

        foreach (var item in usuariosConPuntos)
        {
            var userInfo = usuariosInfo.FirstOrDefault(u => u.Id == item.UsuarioId);
            var user = await _userManager.FindByIdAsync(item.UsuarioId);
            var claims = await _userManager.GetClaimsAsync(user);
            var imagenUrl = claims.FirstOrDefault(c => c.Type == "ImagenPerfilUrl")?.Value
                          ?? "https://cdn.pixabay.com/photo/2015/10/05/22/37/blank-profile-picture-973460_1280.png";

            rankingFinal.Add(new juezprueba.ViewModels.RankingViewModel
            {
                Usuario = userInfo?.NombreCompleto ?? "Usuario desconocido",
                PuntosTotales = item.PuntosTotales,
                ImagenPerfilUrl = imagenUrl
            });
        }

        return View(rankingFinal);
    }

}
