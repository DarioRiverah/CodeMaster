﻿using juezprueba.Context;
using juezprueba.Models;
using juezprueba.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace juezprueba.Controllers
{
    public class ProblemasController : Controller
    {
        private readonly Judge0Service _judge0Service;
        private readonly AppDbContext _context;

        public ProblemasController(Judge0Service judge0Service, AppDbContext context)
        {
            _judge0Service = judge0Service;
            _context = context;
        }

        public async Task<IActionResult> Index(string dificultad)
        {
            var problemas = _context.Problemas.AsQueryable();
            string dificultadSeleccionada = dificultad;

            if (!string.IsNullOrEmpty(dificultad))
            {
                problemas = problemas.Where(p => p.Dificultad == dificultad);
            }

            ViewBag.DificultadSeleccionada = dificultadSeleccionada;


            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    int racha = await CalcularRachaActual(userId);
                    ViewBag.Racha = racha;
                }
            }

            return View(await problemas.ToListAsync());
        }

        // 2. Detalles
        public async Task<IActionResult> Details(int id)
        {
            var problema = await _context.Problemas
                .Include(p => p.CasosDePrueba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problema == null)
                return NotFound();

            return View(problema);
        }

        // 3. Crear
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Problema problema)
        {
            _context.Add(problema);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // 4. Editar
        public async Task<IActionResult> Edit(int id)
        {
            var problema = await _context.Problemas.FindAsync(id);
            if (problema == null)
                return NotFound();

            return View(problema);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Problema problema)
        {
            if (id != problema.Id)
                return NotFound();
            try
            {
                _context.Update(problema);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Problemas.Any(e => e.Id == id))
                    return NotFound();
                else
                    throw;
            }
            return RedirectToAction(nameof(Index));
        }

        // Acción para eliminar directamente (usado por AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var problema = await _context.Problemas.FindAsync(id);
            if (problema == null)
            {
                return NotFound();
            }

            _context.Problemas.Remove(problema);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // Acción para mostrar vista de confirmación de eliminación
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var problema = await _context.Problemas
                .Include(p => p.CasosDePrueba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problema == null)
            {
                return NotFound();
            }

            return View(problema);
        }

        // 6. Resolver (envío de código)
        public async Task<IActionResult> Resolver(int id)
        {
            var problema = await _context.Problemas
                .Include(p => p.CasosDePrueba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problema == null)
                return NotFound();

            return View(problema);
        }

        [HttpPost]
        public async Task<IActionResult> Resolver(int id, string sourceCode, int languageId)
        {
            var problema = await _context.Problemas
                .Include(p => p.CasosDePrueba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problema == null)
                return NotFound();

            var resultados = new List<string>();

            bool todosCorrectos = true;
            foreach (var caso in problema.CasosDePrueba)
            {
                var resultadoJson = await _judge0Service.EnviarCodigoAsync(sourceCode, languageId, caso.Input);
                var salida = _judge0Service.ExtraerSalida(resultadoJson);

                bool esCorrecto = salida.Trim() == caso.OutputEsperado.Trim();
                resultados.Add($"Caso {caso.Id}: {(esCorrecto ? "✅ Correcto" : $"❌ Incorrecto (Esperado: '{caso.OutputEsperado}', Obtenido: '{salida}')")}");

                if (!esCorrecto)
                    todosCorrectos = false;
            }

            ViewBag.Resultados = resultados;

            if (todosCorrectos)
            {
                // Obtener id del usuario actual
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

                if (!string.IsNullOrEmpty(userId))
                {
                    var ahora = DateTime.UtcNow;
                    var inicioMes = new DateTime(ahora.Year, ahora.Month, 1);

                    // Verificar si ya resolvio este problema este mes
                    bool yaResuelto = await _context.ProblemasResueltos
                        .AnyAsync(pr => pr.UsuarioId == userId && pr.ProblemaId == id && pr.FechaResolucion >= inicioMes);

                    if (!yaResuelto)
                    {
                        var nuevoRegistro = new ProblemaResuelto
                        {
                            UsuarioId = userId,
                            ProblemaId = id,
                            FechaResolucion = ahora
                        };
                        _context.ProblemasResueltos.Add(nuevoRegistro);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            return View(problema);
        }

        private async Task<int> CalcularRachaActual(string userId)
        {
            var hoy = DateTime.UtcNow.Date;

            var fechas = await _context.ProblemasResueltos
                .Where(p => p.UsuarioId == userId)
                .Select(p => p.FechaResolucion.Date)
                .Distinct()
                .OrderByDescending(f => f)
                .ToListAsync();

            int racha = 0;
            for (int i = 0; i < fechas.Count; i++)
            {
                if (fechas[i] == hoy.AddDays(-racha))
                {
                    racha++;
                }
                else
                {
                    break;
                }
            }

            return racha;
        }

        private bool ProblemaExists(int id)
        {
            return _context.Problemas.Any(e => e.Id == id);
        }
    }
}