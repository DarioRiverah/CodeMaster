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
            {
                return NotFound();
            }

            var casoDePrueba = await _context.CasosDePrueba
                .Include(c => c.Problema)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (casoDePrueba == null)
            {
                return NotFound();
            }

            return View(casoDePrueba);
        }

        // GET: CasoDePrueba/Create
        public IActionResult Create()
        {
            ViewData["ProblemaId"] = new SelectList(_context.Problemas, "Id", "Id");
            return View();
        }

        // POST: CasoDePrueba/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
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
            {
                return NotFound();
            }

            var casoDePrueba = await _context.CasosDePrueba.FindAsync(id);
            if (casoDePrueba == null)
            {
                return NotFound();
            }
            ViewData["ProblemaId"] = new SelectList(_context.Problemas, "Id", "Id", casoDePrueba.ProblemaId);
            return View(casoDePrueba);
        }

        // POST: CasoDePrueba/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Input,OutputEsperado,ProblemaId")] CasoDePrueba casoDePrueba)
        {
            if (id != casoDePrueba.Id)
            {
                return NotFound();
            }

            try
            {
                _context.Update(casoDePrueba);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CasoDePruebaExists(casoDePrueba.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: CasoDePrueba/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var casoDePrueba = await _context.CasosDePrueba
                .Include(c => c.Problema)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (casoDePrueba == null)
            {
                return NotFound();
            }

            return View(casoDePrueba);
        }

        // POST: CasoDePrueba/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var casoDePrueba = await _context.CasosDePrueba.FindAsync(id);
            if (casoDePrueba != null)
            {
                _context.CasosDePrueba.Remove(casoDePrueba);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CasoDePruebaExists(int id)
        {
            return _context.CasosDePrueba.Any(e => e.Id == id);
        }
    }
}
