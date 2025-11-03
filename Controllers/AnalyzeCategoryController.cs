using Labaratory.DbContext;
using Labaratory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Labaratory.Controllers
{
    public class AnalyzeCategoryController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<AnalyzeCategoryController> _logger;

        public AnalyzeCategoryController(ApplicationContext context, ILogger<AnalyzeCategoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var categories = await _context.AnalyzeCategories!.ToListAsync();
            return View(categories);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCategory(AnalyzeCategory model)
        {
            if (ModelState.IsValid)
            {
                model.AddDate = DateTime.Now;
                _context.AnalyzeCategories!.Add(model);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EditCategory(AnalyzeCategory model)
        {
            if (ModelState.IsValid)
            {
                var existing = await _context.AnalyzeCategories!.FindAsync(model.Id);
                if (existing != null)
                {
                    existing.CategoryName = model.CategoryName;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _context.AnalyzeCategories!.FindAsync(id);
                if (category != null)
                {
                    _context.AnalyzeCategories.Remove(category);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении категории");
                return RedirectToAction("Index");
            }
        }
    }
}
