using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Labaratory.DbContext;
using Labaratory.Models;
using Microsoft.AspNetCore.Identity;
using X.PagedList.Extensions;


namespace Labaratory.Controllers
{
    public class AnalyzeTypesController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<AnalyzeTypesController> _logger;

        public AnalyzeTypesController(ApplicationContext context, UserManager<User> userManager, ILogger<AnalyzeTypesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }
        public async Task<IActionResult> Index(int? page)
        {
            try 
            {
                int pageNumber = page ?? 1;
                int pageSize = 20;

                var analyzeTypesList = await _context.AnalyzeTypes!
                    .Include(at => at.AnalyzeCategory)
                    .ToListAsync();

                var pagedAnalyzeTypes = analyzeTypesList.ToPagedList(pageNumber, pageSize);

                ViewBag.AnalyzeCategories = await _context.AnalyzeCategories!.ToListAsync();
                ViewBag.Doctors = await _userManager.Users.ToListAsync();

                return View(pagedAnalyzeTypes);
            } catch (Exception ex) 
            {
                _logger.LogError(ex, "Не удалось вывести список типов анализов");
                return RedirectToAction("Error", "Home");
            }
            
        }


        // Создание анализа: GET
        public async Task<IActionResult> Create()
        {
            ViewBag.AnalyzeCategories = await _context.AnalyzeCategories!.ToListAsync();
            ViewBag.Doctors = await _userManager.Users.ToListAsync();
            return View();
        }

        // Создание анализа: POST
        [HttpPost]
        public async Task<IActionResult> Create(AnalyzeType analyzeType)
        {
            if (analyzeType.AnalyzeCategoryId == 0)
            {
                ModelState.AddModelError("AnalyzeCategoryId", "Выберите категорию анализа.");
            }

            if (string.IsNullOrEmpty(analyzeType.DoctorId))
            {
                ModelState.AddModelError("DoctorId", "Выберите врача.");
            }

            if (ModelState.IsValid)
            {
                var doctor = await _userManager.FindByIdAsync(analyzeType.DoctorId);
                if (doctor != null)
                {
                    analyzeType.DoctorName = $"{doctor.FirsName} {doctor.LastName}";
                }

                analyzeType.AddDate = DateTime.Now; // ✅ Устанавливаем дату создания
                _context.Add(analyzeType);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AnalyzeCategories = await _context.AnalyzeCategories!.ToListAsync();
            ViewBag.Doctors = await _userManager.Users.ToListAsync();
            return View(analyzeType);
        }

        // Редактирование анализа: GET
        public async Task<IActionResult> Edit(int id)
        {
            var analyzeType = await _context.AnalyzeTypes!
                .Include(a => a.AnalyzeCategory)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (analyzeType == null) return NotFound();

            ViewBag.AnalyzeCategories = await _context.AnalyzeCategories!.ToListAsync();
            ViewBag.Doctors = await _context.Users!.ToListAsync();
            return View(analyzeType);
        }

        // Редактирование анализа: POST
        [HttpPost]
        public async Task<IActionResult> Edit(AnalyzeType analyzeType)
        {
            if (ModelState.IsValid)
            {
                var existingAnalyze = await _context.AnalyzeTypes!.FindAsync(analyzeType.Id);
                if (existingAnalyze == null) return NotFound();

                existingAnalyze.AnalyzeName = analyzeType.AnalyzeName;
                existingAnalyze.Price = analyzeType.Price;
                existingAnalyze.Status = analyzeType.Status;
                existingAnalyze.DoctorId = analyzeType.DoctorId;
                existingAnalyze.DoctorPayoutPercentage = analyzeType.DoctorPayoutPercentage;
                existingAnalyze.TypeAnalyzeID = analyzeType.TypeAnalyzeID;
                existingAnalyze.TextResult = analyzeType.TextResult;
                existingAnalyze.NormalResult = analyzeType.NormalResult;
                existingAnalyze.AnalyzeCategoryId = analyzeType.AnalyzeCategoryId;
                existingAnalyze.Unit = analyzeType.Unit;

                var doctor = await _userManager.FindByIdAsync(analyzeType.DoctorId);
                if (doctor != null)
                {
                    existingAnalyze.DoctorName = $"{doctor.FirsName} {doctor.LastName}";
                }
                existingAnalyze.AddDate = existingAnalyze.AddDate != DateTime.MinValue ? existingAnalyze.AddDate : DateTime.Now;

                _context.Update(existingAnalyze);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.AnalyzeCategories = await _context.AnalyzeCategories!.ToListAsync();
            ViewBag.Doctors = await _context.Users!.ToListAsync();
            return View(analyzeType);
        }

        // Удаление анализа: GET
        [HttpGet]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try {
                var analyzeType = await _context.AnalyzeTypes!.FindAsync(id);
                if (analyzeType != null)
                {
                    _context.AnalyzeTypes.Remove(analyzeType);
                    await _context.SaveChangesAsync();
                }
                return RedirectToAction(nameof(Index));
            } 
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Не удалось удалить тип анализа");
                return RedirectToAction("Error", "Home");
            }
            
        }


        [HttpPost]
        public async Task<IActionResult> CreateCategory(AnalyzeCategory analyzeCategory)
        {
            analyzeCategory.AddDate = DateTime.Now;

            _context.AnalyzeCategories!.Add(analyzeCategory);
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> AddSubItem(int analyzeTypeId, string name, string? unit, string? normalRange)
        {
            var analyze = await _context.AnalyzeTypes!.FindAsync(analyzeTypeId);
            if (analyze == null)
                return Json(new { success = false, message = "Анализ не найден." });

            var subItem = new AnalyzeSubItem
            {
                AnalyzeTypeId = analyzeTypeId,
                Name = name,
                Unit = unit,
                NormalRange = normalRange,
                AddDate = DateTime.Now
            };

            _context.AnalyzeSubItems!.Add(subItem);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetSubItems(int analyzeTypeId)
        {
            var items = await _context.AnalyzeSubItems!
                .Where(s => s.AnalyzeTypeId == analyzeTypeId)
                .Select(s => new { s.Id, s.Name, s.Unit, s.NormalRange })
                .ToListAsync();

            return Json(items);
        }



    }
}
