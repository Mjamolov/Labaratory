using Labaratory.Models;
using Labaratory.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Labaratory.Controllers
{
    public class PatientController : Controller
    {
        private readonly IRequestDbService _requestDbService;

        public PatientController(IRequestDbService requestDbService)
        {
            _requestDbService = requestDbService;
        }

        public IActionResult Index() 
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(Patient pacient) 
        {
            pacient.GuidId = Guid.NewGuid().ToString();
            _requestDbService.AddNewPatient(pacient);


            return RedirectToAction("PatientList", "Application");
        }
    }
}
