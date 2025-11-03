using Labaratory.DbContext;
using Labaratory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;

namespace Labaratory.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationContext _context; 

        public HomeController(ILogger<HomeController> logger, ApplicationContext context)
        {
            _logger = logger;
            _context = context;
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}

        public async Task<IActionResult> Index()
        {
            var doctorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(doctorId))
                return Unauthorized();

            var pendingResults = await _context.PatientApplications!
                .Include(pa => pa.Patient)
                .Where(pa => pa.IsFullyPaid && !pa.AnalysisResults.Any())
                .OrderByDescending(pa => pa.AddDate)
                .ToListAsync();

            var filtered = pendingResults
                .Where(pa => pa.SelectedDoctors?.Split(',').Contains(doctorId) == true)
                .ToList();

            return View(filtered);
        }



        public IActionResult Privacy()
        {
            return View();
        }

        [Route("Home/Error")]
        public IActionResult Error()
        {
            return View();
        }
    }
}