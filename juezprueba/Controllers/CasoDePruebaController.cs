using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using juezprueba.Context;
using juezprueba.Models;

namespace juezprueba.Controllers
{
    public class CasoDePruebaController : Controller
    {
        private readonly AppDbContext _context;

        public CasoDePruebaController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> CasosDePruebaPorProblema(int id)
        {
            var problema = await _context.Problemas
                .Include(p => p.CasosDePrueba)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (problema == null)
            {
                return NotFound();
            }

            ViewBag.ProblemaId = id; // Necesario para el formulario
            return View(problema);
        }

        // GET: CasoDePrueba
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.CasosDePrueba.Include(c => c.Problema);
            return View(await appDbContext.ToListAsync());
        }

        // GET: CasoDePrueba/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var casoDePrueba = await _context.CasosDePrueba
                .Include(c => c.Problema)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (casoDePrueba == null)
                return NotFound();

            return View(casoDePrueba);
        }

        // GET: CasoDePrueba/Create
        public IActionResult Create()
        {
            ViewData["ProblemaId"] = new SelectList(_context.Problemas, "Id", "Id");
            return View();
        }

        // POST: CasoDePrueba/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Input,OutputEsperado,ProblemaId")] CasoDePrueba casoDePrueba)
        {
            _context.Add(casoDePrueba);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: CasoDePrueba/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var casoDePrueba = await _context.CasosDePrueba.FindAsync(id);
            if (casoDePrueba == null)
                return NotFound();

            ViewData["ProblemaId"] = new SelectList(_context.Problemas, "Id", "Id", casoDePrueba.ProblemaId);
            return View(casoDePrueba);
        }

        // POST: CasoDePrueba/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([Bind("Id,Input,OutputEsperado,ProblemaId")] CasoDePrueba casoDePrueba)
        {
            try
            {
                _context.Update(casoDePrueba);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CasoDePruebaExists(casoDePrueba.Id))
                    return NotFound();
                else
                    throw;
            }

            return RedirectToAction("CasosDePruebaPorProblema", new { id = casoDePrueba.ProblemaId });
        }

        // GET: CasoDePrueba/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var casoDePrueba = await _context.CasosDePrueba
                .Include(c => c.Problema)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (casoDePrueba == null)
                return NotFound();

            return View(casoDePrueba);
        }

        // POST: CasoDePrueba/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var casoDePrueba = await _context.CasosDePrueba.FindAsync(id);
            int problemaId = casoDePrueba?.ProblemaId ?? 0;

            if (casoDePrueba != null)
            {
                _context.CasosDePrueba.Remove(casoDePrueba);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("CasosDePruebaPorProblema", new { id = problemaId });
        }

        private bool CasoDePruebaExists(int id)
        {
            return _context.CasosDePrueba.Any(e => e.Id == id);
        }

        // POST: Agregar caso de prueba desde problema
        [HttpPost]
        public async Task<IActionResult> AgregarCasoDesdeProblema(string Input, string OutputEsperado, int ProblemaId)
        {
            var caso = new CasoDePrueba
            {
                Input = Input,
                OutputEsperado = OutputEsperado,
                ProblemaId = ProblemaId
            };

            _context.CasosDePrueba.Add(caso);
            await _context.SaveChangesAsync();

            return RedirectToAction("CasosDePruebaPorProblema", new { id = ProblemaId });
        }

        [HttpPost]
        public async Task<IActionResult> EliminarDesdeVista(int id)
        {
            var caso = await _context.CasosDePrueba.FindAsync(id);
            if (caso == null)
                return NotFound();

            _context.CasosDePrueba.Remove(caso);
            await _context.SaveChangesAsync();

            return Ok(); // Para AJAX
        }

        [HttpPost]
        public IActionResult EditarCasoDesdeProblema(CasoDePrueba caso)
        {
            var existente = _context.CasosDePrueba.FirstOrDefault(c => c.Id == caso.Id);
            if (existente != null)
            {
                existente.Input = caso.Input;
                existente.OutputEsperado = caso.OutputEsperado;
                _context.SaveChanges();
            }

            return RedirectToAction("CasosDePruebaPorProblema", new { id = caso.ProblemaId });
        }
    }
}
