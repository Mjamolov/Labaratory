using Labaratory.DbContext;
using Labaratory.Models;
using Labaratory.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using X.PagedList.Extensions;
using Labaratory.Services;
using Spire.Doc.Documents;
using Spire.Doc.Fields;
using Spire.Doc;
using QRCoder;
using System.Drawing;
using Paragraph = Spire.Doc.Documents.Paragraph;
using Table = Spire.Doc.Table;
using Spire.Pdf.Graphics;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Spire.Doc.Interface;
using System.Diagnostics;

namespace Labaratory.Controllers
{
    public class ApplicationController : Controller
    {
        private readonly IRequestDbService _requestDbService;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ApplicationContext _context;
        private readonly ShowPatientAnalyzeService _analyzeService = new();
        private readonly ILogger<ApplicationController> _logger;

        public ApplicationController(IRequestDbService requestDbService, UserManager<User> userManager, SignInManager<User> signInManager,ApplicationContext context, ILogger<ApplicationController> logger)
        {
            _requestDbService = requestDbService;
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }


        [HttpPost]
        public IActionResult Index(int id)
        {
            try 
            {
                var patient = _context?.Patients?.FirstOrDefault(i => i.Id == id);

                var analyzeType = _context?.AnalyzeTypes?.ToList();
                var Doctors = _userManager.Users.ToList();

                var response = new PatientAnalyzeView
                {
                    Patient = patient,
                    AnalyzeType = analyzeType,
                    Doctor = Doctors
                };

                return View(response);
            } 
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Ошибка при получении данных");
                return RedirectToAction("Error", "Home");
            }
            
        }

        [HttpPost]
        public IActionResult GetAnalyzesByDoctors([FromBody] List<string> doctorIds)
        {
            if (doctorIds == null || !doctorIds.Any())
            {
                return Json(new List<object>());
            }

            var analyzes = _context.AnalyzeTypes!
                .Where(a => doctorIds.Contains(a.DoctorId!)) 
                .Select(a => new { a.Id, a.AnalyzeName })
                .ToList();

            return Json(analyzes);
        }

        //public IActionResult PatientList(int? page, string searchQuery, string sortOrder)
        //{
        //    int pageSize = 10;
        //    int pageNumber = page ?? 1;

        //    ViewBag.CurrentSort = sortOrder;
        //    ViewBag.DateSortParam = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";

        //    var query = _context.Patients!
        //        .AsNoTracking()
        //        .AsQueryable();

        //    // Поиск
        //    if (!string.IsNullOrWhiteSpace(searchQuery))
        //    {
        //        searchQuery = searchQuery.Trim();
        //        query = query.Where(app =>
        //            app.Id.ToString().Contains(searchQuery) ||
        //            (!string.IsNullOrEmpty(app.PhoneNumber) && app.PhoneNumber.Contains(searchQuery)) ||
        //            (!string.IsNullOrEmpty(app.Surname) && app.Surname.Contains(searchQuery)) ||
        //            (!string.IsNullOrEmpty(app.Name) && app.Name.Contains(searchQuery)) ||
        //            (!string.IsNullOrEmpty(app.Lastname) && app.Lastname.Contains(searchQuery)) ||
        //            (!string.IsNullOrEmpty(app.Email) && app.Email.Contains(searchQuery))
        //        );

        //        ViewBag.SearchQuery = searchQuery;
        //    }

        //    // Сортировка по CreatedAt
        //    query = sortOrder switch
        //    {
        //        "date_desc" => query.OrderByDescending(p => p.CreatedAt),
        //        _ => query.OrderBy(p => p.CreatedAt)
        //    };

        //    var pagedUsers = query.ToPagedList(pageNumber, pageSize);
        //    return View(pagedUsers);
        //}



        public IActionResult PatientList(int? page, string searchQuery)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;
            //var users = _context?.Patients?.ToList();
            var users = _context?.Patients?
            .OrderByDescending(p => p.CreatedAt)
            .ToList();


            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                users = users!.Where(app =>
                    app.Id.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrEmpty(app.PhoneNumber) && app.PhoneNumber.Contains(searchQuery)) ||
                    (!string.IsNullOrEmpty($"{app?.Name} {app?.Surname} {app?.Lastname}") &&
                     $"{app?.Name} {app?.Surname} {app?.Lastname}".Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                ViewBag.SearchQuery = searchQuery;
            }

            var pagedUsers = users?.ToPagedList(pageNumber, pageSize);

            return View(pagedUsers);
        }



        public IActionResult Confirm(PatientAnalyzeView model, List<string> SelectedDoctors, List<int> SelectedAnalyzeTypes)
        {
            var totalAnalyzeTypes = _context?.AnalyzeTypes?.Where(at => SelectedAnalyzeTypes.Contains(at.Id)).ToList();

            var TotalCost = totalAnalyzeTypes?.Sum(at => at.Price) ?? 0;

            var FinalCost = TotalCost;

            if (model.Discount.HasValue)
            {
                FinalCost = TotalCost * (1 - model.Discount.Value / 100.0);
            }

            if (model.PaymentType == "Partial" && model.PaymentAmount.HasValue)
            {
                FinalCost = model.PaymentAmount.Value;
            }

            var application = new PatientApplication
            {
                PatientId = model.Patient?.Id ?? 0,
                SelectedDoctors = string.Join(",", SelectedDoctors),

                SelectedAnalyzeTypes = string.Join(",", SelectedAnalyzeTypes),
                PaymentType = model.PaymentType,
                PaymentAmount = model.PaymentAmount,
                Discount = model.Discount,
                TotalCost = TotalCost,
                FinalCost = FinalCost,
                UniqId = Guid.NewGuid().ToString()
            };

            _context?.PatientApplications?.Add(application);
            _context?.SaveChanges();

            return RedirectToAction("PatientApplications", new { patientId = application.PatientId });
        }

        public async Task<IActionResult> PatientApplications(int patientId, int? page)
        {
            int pageSize = 10;
            int pageNumber = page ?? 1;

            var patient = _context.Patients?.FirstOrDefault(p => p.Id == patientId);
            if (patient == null)
            {
                return NotFound("Пациент не найден.");
            }

            ViewBag.PatientName = $"{patient.Surname} {patient.Name} {patient.Lastname}";
            ViewBag.PatientPhone = patient.PhoneNumber;

            var currentUser = await _userManager.GetUserAsync(User);
            var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

            var applications = _context.PatientApplications?
                .Where(app => app.PatientId == patientId)
                .Include(app => app.Patient)
                .Include(app => app.AnalysisResults)
                    .ThenInclude(ar => ar.AnalyzeType)
                .OrderByDescending(app => app.AddDate)
                .ToList();

            if (!isAdmin && applications != null)
            {
                var doctorAnalyzeIds = _context.AnalyzeTypes!
                    .Where(a => a.DoctorId == currentUser.Id)
                    .Select(a => a.Id)
                    .ToList();

                applications = applications
                    .Where(app =>
                        !string.IsNullOrEmpty(app.SelectedAnalyzeTypes) &&
                        app.SelectedAnalyzeTypes.Split(',')
                            .Select(id => int.TryParse(id, out var aid) ? aid : -1)
                            .Any(id => doctorAnalyzeIds.Contains(id))
                    )
                    .ToList();
            }

            if (applications == null || !applications.Any())
            {
                return NotFound("Для данного пациента заявок не найдено.");
            }

            var users = _userManager.Users.ToList();
            var analyzeTypes = _context.AnalyzeTypes?.ToList();

            foreach (var app in applications)
            {
                if (!string.IsNullOrEmpty(app.SelectedDoctors))
                {
                    app.DoctorsList = GetDoctorsList(app.SelectedDoctors);
                }
                if (!string.IsNullOrEmpty(app.SelectedAnalyzeTypes))
                {
                    var analyzeIds = app.SelectedAnalyzeTypes.Split(',').Select(id => int.Parse(id.Trim())).ToList();

                    app.AnalyzeNames = analyzeTypes
                        .Where(at => analyzeIds.Contains(at.Id))
                        .Select(at => at.AnalyzeName)
                        .ToList();

                    app.AnalyzeTypesList = analyzeTypes
                        .Where(at => analyzeIds.Contains(at.Id))
                        .ToList();
                }
            }

            // Обеспечим сортировку по дате заявок (самые новые сверху)
            applications = applications.OrderByDescending(app => app.AddDate).ToList();

            var pagedApplications = applications.ToPagedList(pageNumber, pageSize);
            ViewBag.PatientId = patientId;

            return View(pagedApplications);
        }

        public IActionResult BulkAddAnalysisResults(int applicationId)
        {
            try
            {
                var application = _context.PatientApplications?
                    .Include(a => a.Patient)
                    .Include(a => a.AnalysisResults)
                        .ThenInclude(ar => ar.AnalyzeType)
                    .FirstOrDefault(a => a.Id == applicationId);

                if (application == null)
                {
                    _logger.LogError("Заявка с ID {ApplicationId} не найдена.", applicationId);
                    return NotFound("Заявка не найдена.");
                }

                var selectedAnalyzeIds = application.SelectedAnalyzeTypes?
                    .Split(',')
                    .Select(id => int.TryParse(id, out var parsedId) ? parsedId : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList() ?? new List<int>();

                var analyzeTypes = _context.AnalyzeTypes!
                    .Where(at => selectedAnalyzeIds.Contains(at.Id))
                    .ToList();

                var subItems = _context.AnalyzeSubItems!
                    .Where(s => selectedAnalyzeIds.Contains(s.AnalyzeTypeId))
                    .ToList();

                var resultItems = _context.SubItemResults!
                    .Where(r => r.PatientApplicationId == applicationId)
                    .ToList();

                foreach (var analyzeType in analyzeTypes)
                {
                    var hasSubItems = subItems.Any(s => s.AnalyzeTypeId == analyzeType.Id);
                    if (!hasSubItems)
                    {
                        var alreadyExists = application.AnalysisResults.Any(r => r.AnalyzeTypeId == analyzeType.Id);
                        if (!alreadyExists)
                        {
                            application.AnalysisResults.Add(new AnalysisResult
                            {
                                AnalyzeTypeId = analyzeType.Id,
                                AnalyzeType = analyzeType,
                                NormalRange = analyzeType.NormalResult,
                                ResultDate = DateTime.Now
                            });
                        }
                    }
                }

                application.AnalyzeTypesList = analyzeTypes;
                ViewBag.SubItems = subItems;
                ViewBag.SubItemResults = resultItems;
                return View(application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке данных для ввода результатов. ApplicationId: {ApplicationId}", applicationId);
                return RedirectToAction("Error", "Home");
            }
        }





        [HttpPost]
        public IActionResult SaveBulkAnalysisResults(int applicationId, Dictionary<int, AnalysisResult> AnalysisResults)
        {
            try 
            {
                var application = _context.PatientApplications?
                .Include(a => a.AnalysisResults)
                .FirstOrDefault(a => a.Id == applicationId);

                if (application == null)
                    return NotFound("Заявка не найдена.");

                // Загружаем список AnalyzeTypeId, у которых есть подпоказатели
                var analyzeTypeIdsWithSubItems = _context.AnalyzeSubItems!
                    .Select(s => s.AnalyzeTypeId)
                    .Distinct()
                    .ToHashSet();

                foreach (var entry in AnalysisResults)
                {
                    int typeId = entry.Key;

                    // ❗ Пропускаем, если у анализа есть подпоказатели — их нельзя сохранять как основной результат
                    if (analyzeTypeIdsWithSubItems.Contains(typeId))
                        continue;

                    var input = entry.Value;

                    var existing = application.AnalysisResults
                        .FirstOrDefault(r => r.AnalyzeTypeId == typeId);

                    if (existing != null)
                    {
                        existing.Result = input.Result;
                        existing.NormalRange = input.NormalRange;
                        existing.ResultDate = DateTime.Now;
                    }
                    else
                    {
                        application.AnalysisResults.Add(new AnalysisResult
                        {
                            PatientApplicationId = applicationId,
                            AnalyzeTypeId = typeId,
                            Result = input.Result,
                            NormalRange = input.NormalRange,
                            ResultDate = DateTime.Now
                        });
                    }
                }

                _context.SaveChanges();
                return RedirectToAction("PatientApplications", new { patientId = application.PatientId });
            } catch (Exception ex) 
            {
                _logger.LogError(ex, "Ошибка при сохранении подрезультатов");
                return RedirectToAction("Error", "Home");
            }
            
        }

        [HttpPost]
        public IActionResult SaveSubItemResults(int applicationId, IFormCollection form)
        {
            var application = _context.PatientApplications!.FirstOrDefault(a => a.Id == applicationId);
            if (application == null) return NotFound();

            var results = new List<SubItemResult>();

            // Собираем данные из формы
            foreach (var key in form.Keys.Where(k => k.StartsWith("SubItemResults[") && k.EndsWith("].Result")))
            {
                var idPart = key.Split('[', ']')[1];
                if (!int.TryParse(idPart, out int subItemId)) continue;

                var result = form[$"SubItemResults[{subItemId}].Result"];
                var normalRange = form[$"SubItemResults[{subItemId}].NormalRange"];

                results.Add(new SubItemResult
                {
                    AnalyzeSubItemId = subItemId,
                    PatientApplicationId = applicationId,
                    Result = result,
                    NormalRange = normalRange
                });
            }

            // Удаляем старые записи, если они есть
            var existing = _context.SubItemResults!.Where(r => r.PatientApplicationId == applicationId).ToList();
            _context.SubItemResults!.RemoveRange(existing);
            _context.SubItemResults.AddRange(results);

            _context.SaveChanges();

            return RedirectToAction("PatientApplications", new { patientId = application.PatientId });
        }



        public class SubItemResultDto
        {
            public int AnalyzeSubItemId { get; set; }
            public string? Result { get; set; }
            public string? NormalRange { get; set; }
        }



        [HttpPost]
        public IActionResult AddAnalysisResult(int applicationId, int analyzeTypeId, string result, string normalRange)
        {
            try
            {
                var application = _context.PatientApplications?
                    .Include(a => a.AnalysisResults)
                    .FirstOrDefault(a => a.Id == applicationId);

                if (application == null)
                {
                    return NotFound("Заявка не найдена.");
                }

                var analyzeType = _context.AnalyzeTypes?.FirstOrDefault(at => at.Id == analyzeTypeId);
                if (analyzeType == null)
                {
                    return NotFound("Тип анализа не найден.");
                }

                var existingResult = application.AnalysisResults.FirstOrDefault(ar => ar.AnalyzeTypeId == analyzeTypeId);

                if (existingResult != null)
                {
                    existingResult.Result = result;
                    existingResult.NormalRange = normalRange;
                    existingResult.ResultDate = DateTime.Now;
                }
                else
                {
                    var analysisResult = new AnalysisResult
                    {
                        PatientApplicationId = applicationId,
                        AnalyzeTypeId = analyzeTypeId,
                        Result = result,
                        NormalRange = normalRange,
                        ResultDate = DateTime.Now
                    };

                    application.AnalysisResults.Add(analysisResult);
                }

                _context.SaveChanges();
                return RedirectToAction("PatientApplications", new { patientId = application.PatientId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении анализа:");
                return RedirectToAction("Error", "Home");
            }
        }

        public async Task<IActionResult> ApplicationList(int? page, string searchQuery)
        {
            try 
            {
                int pageSize = 10;
                int pageNumber = page ?? 1;

                var currentUser = await _userManager.GetUserAsync(User);
                var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");

                var applications = _context.PatientApplications?
                    .Include(app => app.Patient)
                    .Include(app => app.AnalysisResults)
                        .ThenInclude(r => r.AnalyzeType)
                    .ToList();

                if (!isAdmin)
                {
                    var doctorAnalyzeIds = _context.AnalyzeTypes!
                        .Where(a => a.DoctorId == currentUser.Id)
                        .Select(a => a.Id)
                        .ToList();

                    applications = applications!
                        .Where(app =>
                            app.SelectedAnalyzeTypes?.Split(',').Any(id =>
                                int.TryParse(id, out var aid) && doctorAnalyzeIds.Contains(aid)) ?? false
                        )
                        .ToList();
                }

                // Группировка по пациенту: оставляем только последнюю заявку
                applications = applications!
                    .GroupBy(a => a.PatientId)
                    .Select(g => g.OrderByDescending(a => a.AddDate).First())
                    .OrderByDescending(a => a.AddDate)
                    .ToList();

                // Поиск
                //if (!string.IsNullOrWhiteSpace(searchQuery))
                //{
                //    applications = applications.Where(app =>
                //        app.Id.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                //        (!string.IsNullOrEmpty(app.Patient?.PhoneNumber) && app.Patient.PhoneNumber.Contains(searchQuery)) ||
                //        (!string.IsNullOrEmpty($"{app.Patient?.Name} {app.Patient?.Surname} {app.Patient?.Lastname}") &&
                //         $"{app.Patient?.Name} {app.Patient?.Surname} {app.Patient?.Lastname}".Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                //    ).ToList();
                //}
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    applications = applications.Where(app =>
                        app.Id.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        app.PatientId.ToString().Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrEmpty(app.Patient?.PhoneNumber) && app.Patient.PhoneNumber.Contains(searchQuery)) ||
                        (!string.IsNullOrEmpty($"{app.Patient?.Name} {app.Patient?.Surname} {app.Patient?.Lastname}") &&
                         $"{app.Patient?.Name} {app.Patient?.Surname} {app.Patient?.Lastname}".Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    ViewBag.SearchQuery = searchQuery;
                }


                // Загружаем врачей
                var users = _userManager.Users.ToList();
                foreach (var app in applications)
                {
                    if (!string.IsNullOrEmpty(app.SelectedDoctors))
                    {
                        var doctorIds = app.SelectedDoctors.Split(',').Select(id => id.Trim()).ToList();
                        app.DoctorsList = users.Where(u => doctorIds.Contains(u.Id)).ToList();
                    }
                    else
                    {
                        app.DoctorsList = new List<User>();
                    }
                }

                var pagedApplications = applications.ToPagedList(pageNumber, pageSize);
                return View(pagedApplications);
            } 
            catch(Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выводе списка");
                return RedirectToAction("Error", "Home");
            }
        }
        //public IActionResult ShowApplication(string id, string unId, string format = "docx")
        //{
        //    try
        //    {
        //        var application = _context.PatientApplications?
        //            .Include(a => a.Patient)
        //            .Include(a => a.AnalysisResults)
        //                .ThenInclude(ar => ar.AnalyzeType)
        //                    .ThenInclude(at => at.AnalyzeCategory)
        //            .Include(a => a.Prescriptions)
        //            .FirstOrDefault(a => a.UniqId == unId);

        //        if (application == null) return NotFound();

        //        var selectedIds = application.SelectedAnalyzeTypes?
        //            .Split(',', StringSplitOptions.RemoveEmptyEntries)
        //            .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
        //            .Where(id => id.HasValue)
        //            .Select(id => id!.Value)
        //            .ToList() ?? new();

        //        var analyzeTypes = _context.AnalyzeTypes!
        //            .Include(at => at.AnalyzeCategory)
        //            .Where(at => selectedIds.Contains(at.Id))
        //            .ToList();

        //        var subItemResults = _context.SubItemResults!
        //            .Include(r => r.AnalyzeSubItem)
        //                .ThenInclude(s => s.AnalyzeType)
        //            .Where(r => r.PatientApplicationId == application.Id)
        //            .ToList();

        //        string doctorNames = GetDoctorsNames(application.SelectedDoctors);
        //        string fileName;
        //        Document document = new();
        //        Section section = null;

        //        bool onlyPrescription = analyzeTypes.All(at => at.TypeAnalyzeID == false);

        //        if (onlyPrescription)
        //        {
        //            fileName = GeneratePrescriptionDocument(document, application, doctorNames);
        //            section = document.Sections[0];
        //        }
        //        else
        //        {
        //            fileName = GenerateAnalyzeDocument(document, application, doctorNames, analyzeTypes, subItemResults, out section);
        //        }

        //        AddQrCode(document, application, unId);

        //        // 🔹 Если нужен PDF
        //        if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
        //        {
        //            var stream = new MemoryStream();
        //            document.SaveToStream(stream, FileFormat.PDF);
        //            stream.Position = 0;

        //            Response.Headers.Add("Content-Disposition", $"inline; filename={fileName}.pdf");
        //            return File(stream, "application/pdf");
        //        }
        //        else
        //        {
        //            string tempDocx = Path.Combine(Path.GetTempPath(), $"app_{Guid.NewGuid()}.docx");

        //            document.SaveToFile(tempDocx, FileFormat.Docx2019);

        //            var fileBytes = System.IO.File.ReadAllBytes(tempDocx);
        //            return File(fileBytes,
        //                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        //                $"{fileName}.docx");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Не удалось определить тип документа");
        //        return RedirectToAction("Error", "Home");
        //    }
        //}
        public IActionResult ShowApplication(string id, string unId, string format = "docx")
        {
            string? tempDocx = null;
            string? tempPdf = null;

            try
            {
                var application = _context.PatientApplications?
                     .Include(a => a.Patient)
                     .Include(a => a.AnalysisResults).ThenInclude(ar => ar.AnalyzeType).ThenInclude(at => at.AnalyzeCategory)
                     .Include(a => a.Prescriptions)
                     .FirstOrDefault(a => a.UniqId == unId);

                if (application == null) return NotFound();

                var selectedIds = application.SelectedAnalyzeTypes?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => int.TryParse(x, out var p) ? p : (int?)null)
                    .Where(v => v.HasValue).Select(v => v!.Value).ToList() ?? new();

                var analyzeTypes = _context.AnalyzeTypes!
                    .Include(at => at.AnalyzeCategory)
                    .Where(at => selectedIds.Contains(at.Id))
                    .ToList();

                var subItemResults = _context.SubItemResults!
                    .Include(r => r.AnalyzeSubItem).ThenInclude(s => s.AnalyzeType)
                    .Where(r => r.PatientApplicationId == application.Id)
                    .ToList();

                string doctorNames = GetDoctorsNames(application.SelectedDoctors);
                string fileName;
                var document = new Document();
                Section? section;

                bool onlyPrescription = analyzeTypes.All(at => at.TypeAnalyzeID == false);

                if (onlyPrescription)
                {
                    fileName = GeneratePrescriptionDocument(document, application, doctorNames);
                    section = document.Sections[0];
                }
                else
                {
                    fileName = GenerateAnalyzeDocument(document, application, doctorNames, analyzeTypes, subItemResults, out section);
                }

                AddQrCode(document, application, unId);

                // Всегда сохраняем во временный DOCX (универсальная точка)
                tempDocx = Path.Combine(Path.GetTempPath(), $"app_{Guid.NewGuid()}.docx");
                document.SaveToFile(tempDocx, FileFormat.Docx2019);

                if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    // Конвертация только через LibreOffice (никакого Spire PDF)
                    tempPdf = Path.ChangeExtension(tempDocx, ".pdf");

                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = @"C:\Program Files\LibreOffice\program\soffice.exe",
                            Arguments = $"--headless --convert-to pdf \"{tempDocx}\" --outdir \"{Path.GetDirectoryName(tempDocx)}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    p.Start();
                    p.WaitForExit();

                    if (!System.IO.File.Exists(tempPdf))
                        return BadRequest("Ошибка конвертации DOCX → PDF через LibreOffice.");

                    var pdfBytes = System.IO.File.ReadAllBytes(tempPdf);
                    Response.Headers["Content-Disposition"] = $"inline; filename={fileName}.pdf";
                    return File(pdfBytes, "application/pdf");
                }
                else
                {
                    var fileBytes = System.IO.File.ReadAllBytes(tempDocx);
                    return File(
                        fileBytes,
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                        $"{fileName}.docx"
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при формировании документа");
                return RedirectToAction("Error", "Home");
            }
            finally
            {
                try
                {
                    if (tempDocx != null && System.IO.File.Exists(tempDocx)) System.IO.File.Delete(tempDocx);
                    if (tempPdf != null && System.IO.File.Exists(tempPdf)) System.IO.File.Delete(tempPdf);
                }
                catch { /* ignore */ }
            }
        }


        // ЧАСТЬ 2: Генерация назначения
        private string GeneratePrescriptionDocument(Document doc, PatientApplication app, string doctorNames)
        {
            var templatePath = Path.Combine("wwwroot/LayoutFile", "template2.docx");
            doc.LoadFromFile(templatePath);

            ReplaceTextInDocument(doc, "{{FIO}}", $"{app.Patient?.Surname} {app.Patient?.Name} {app.Patient?.Lastname}");
            ReplaceTextInDocument(doc, "{{PatientId}}", app.Patient?.Id.ToString() ?? "—");
            ReplaceTextInDocument(doc, "{{Birth}}", app.Patient?.BirthDay?.ToString("yyyy") ?? "Не указано");
            ReplaceTextInDocument(doc, "{{Address}}", app.Patient?.Adress ?? "Не указано");
            ReplaceTextInDocument(doc, "{{Doctor}}", doctorNames);
            ReplaceTextInDocument(doc, "{{Date}}", app.AddDate.ToString("dd.MM.yyyy"));

            var textToInsert = app.Prescriptions.FirstOrDefault()?.Text ?? "";
            foreach (Section sec in doc.Sections)
            {
                foreach (Paragraph para in sec.Paragraphs)
                {
                    if (para.Text.Contains("{{PrescriptionText}}"))
                    {
                        para.Text = para.Text.Replace("{{PrescriptionText}}", "");
                        var range = para.AppendText(textToInsert);
                        range.CharacterFormat.FontName = "Times New Roman";
                        range.CharacterFormat.FontSize = 12;
                        range.CharacterFormat.Bold = false;
                    }
                }
            }

            if (!app.IsFullyPaid)
            {
                Section section = doc.Sections[0];
                var summaryTable = section.AddTable(true);
                summaryTable.ResetCells(1, 2);

                summaryTable.Rows[0].Cells[0].AddParagraph().AppendText("Показатель").CharacterFormat.Bold = true;
                summaryTable.Rows[0].Cells[1].AddParagraph().AppendText("Цена").CharacterFormat.Bold = true;

                var selectedIds = app.SelectedAnalyzeTypes?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList() ?? new();

                var analyzeList = _context.AnalyzeTypes!
                    .Where(at => selectedIds.Contains(at.Id))
                    .ToList();

                double totalSum = 0;

                foreach (var at in analyzeList)
                {
                    var row = summaryTable.AddRow();
                    row.Cells[0].AddParagraph().AppendText(at.AnalyzeName ?? "—");
                    double price = at.Price ?? 0; 
                    row.Cells[1].AddParagraph().AppendText($"{price:N2} сум");
                    totalSum += price;
                }

                var paid = app.PaymentAmount ?? 0;
                var debt = totalSum - paid;

                var rowTotal = summaryTable.AddRow();
                rowTotal.Cells[0].AddParagraph().AppendText("Итого к оплате:").CharacterFormat.Bold = true;
                rowTotal.Cells[1].AddParagraph().AppendText($"{totalSum:N2} сум");

                var rowPaid = summaryTable.AddRow();
                rowPaid.Cells[0].AddParagraph().AppendText("Оплачено:");
                rowPaid.Cells[1].AddParagraph().AppendText($"{paid:N2} сум");

                var rowDebt = summaryTable.AddRow();
                rowDebt.Cells[0].AddParagraph().AppendText("Долг:").CharacterFormat.Bold = true;
                rowDebt.Cells[1].AddParagraph().AppendText($"{debt:N2} сум");

            }

            return $"prescription_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        }

        private string GenerateAnalyzeDocument(Document doc, PatientApplication app, string doctorNames, List<AnalyzeType> analyzeTypes, List<SubItemResult> subItemResults, out Section section)
        {
            var templatePath = Path.Combine("wwwroot/LayoutFile", "template.docx");
            doc.LoadFromFile(templatePath);
            section = doc.Sections[0];

            // Заполняем шапку
            ReplaceTextInDocument(doc, "{{FIO}}", $"{app.Patient?.Surname} {app.Patient?.Name} {app.Patient?.Lastname}");
            ReplaceTextInDocument(doc, "{{PatientId}}", app.Patient?.Id.ToString() ?? "—");
            ReplaceTextInDocument(doc, "{{Birth}}", app.Patient?.BirthDay?.ToString("yyyy") ?? "Не указано");
            ReplaceTextInDocument(doc, "{{Address}}", app.Patient?.Adress ?? "Не указано");
            ReplaceTextInDocument(doc, "{{Doctor}}", doctorNames);
            ReplaceTextInDocument(doc, "{{Total}}", app.TotalCost.ToString("N2"));
            ReplaceTextInDocument(doc, "{{Date}}", app.AddDate.ToString("dd.MM.yyyy"));

            var analysisResults = app.AnalysisResults.ToList();

            var allTypes = analysisResults.Select(r => r.AnalyzeType)
                .Union(subItemResults
                    .Where(s => s.AnalyzeSubItem?.AnalyzeType != null)
                    .Select(s => s.AnalyzeSubItem!.AnalyzeType))
                .Where(t => t != null)
                .DistinctBy(t => t!.Id)
                .ToList();

            // 🔹 Блок анализов
            AddAnalyzeTable(section, allTypes!, analysisResults, subItemResults);

            // 🔹 Блок цен — отдельная секция
            if (!app.IsFullyPaid)
            {
                Section priceSection = doc.AddSection(); // <-- новая секция для таблицы цен
                var selectedIds = app.SelectedAnalyzeTypes?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList() ?? new();

                var analyzeList = _context.AnalyzeTypes!
                    .Where(at => selectedIds.Contains(at.Id))
                    .ToList();

                var priceTable = priceSection.AddTable(true);
                priceTable.ResetCells(1, 2);
                priceTable.Rows[0].Cells[0].AddParagraph().AppendText("Показатель").CharacterFormat.Bold = true;
                priceTable.Rows[0].Cells[1].AddParagraph().AppendText("Цена").CharacterFormat.Bold = true;

                double total = 0;
                foreach (var at in analyzeList)
                {
                    var row = priceTable.AddRow();
                    row.Cells[0].AddParagraph().AppendText(at.AnalyzeName ?? "—");
                    double price = at.Price ?? 0;
                    row.Cells[1].AddParagraph().AppendText($"{price:N2} сум");
                    total += price;
                }

                var paid = app.PaymentAmount ?? 0;
                var debt = total - paid;

                var rowTotal = priceTable.AddRow();
                rowTotal.Cells[0].AddParagraph().AppendText("Итого к оплате:").CharacterFormat.Bold = true;
                rowTotal.Cells[1].AddParagraph().AppendText($"{total:N2} сум");

                var rowPaid = priceTable.AddRow();
                rowPaid.Cells[0].AddParagraph().AppendText("Оплачено:");
                rowPaid.Cells[1].AddParagraph().AppendText($"{paid:N2} сум");

                var rowDebt = priceTable.AddRow();
                rowDebt.Cells[0].AddParagraph().AppendText("Долг:").CharacterFormat.Bold = true;
                rowDebt.Cells[1].AddParagraph().AppendText($"{debt:N2} сум");
            }

            return $"analysis_results_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        }

        private void AddAnalyzeTable(Section section, List<AnalyzeType> analyzeTypes, List<AnalysisResult> results, List<SubItemResult> subItemResults)
        {
            int rowsPerPageLimit = 500;
            int currentRowCount = 0;

            // Группируем все типы анализов по категории (AnalyzeCategory)
            var groupedByCategory = analyzeTypes
                .GroupBy(t => t.AnalyzeCategory?.CategoryName ?? "Другое");

            foreach (var categoryGroup in groupedByCategory)
            {
                var categoryName = categoryGroup.Key;

                // Заголовок категории
                var title = section.AddParagraph();
                var titleRange = title.AppendText(categoryName);

                titleRange.CharacterFormat.FontSize = 16; // например, 16pt
                titleRange.CharacterFormat.Bold = true;

                title.Format.HorizontalAlignment = HorizontalAlignment.Center;
                title.Format.AfterSpacing = 5;

                // Создаём одну таблицу на категорию
                var table = section.AddTable(true);
                table.ResetCells(1, 4);
                table.TableFormat.Borders.BorderType = BorderStyle.Single;
                table.TableFormat.IsBreakAcrossPages = true;

                AddCell(table.Rows[0].Cells[0], "Показатель", true, HorizontalAlignment.Center);
                AddCell(table.Rows[0].Cells[1], "Результат", true, HorizontalAlignment.Center);
                AddCell(table.Rows[0].Cells[2], "Норма", true, HorizontalAlignment.Center);
                AddCell(table.Rows[0].Cells[3], "Ед. измерения", true, HorizontalAlignment.Center);

                currentRowCount++;

                foreach (var type in categoryGroup)
                {
                    var subs = subItemResults.Where(s => s.AnalyzeSubItem?.AnalyzeTypeId == type.Id).ToList();

                    if (subs.Any())
                    {
                        foreach (var s in subs)
                        {
                            if (currentRowCount >= rowsPerPageLimit)
                            {
                                section = section.Document.AddSection();
                                table = section.AddTable(true);
                                table.ResetCells(1, 4);
                                currentRowCount = 0;
                            }

                            var row = table.AddRow();
                            AddCell(row.Cells[0], s.AnalyzeSubItem?.Name ?? "—", false, HorizontalAlignment.Left);
                            AddCell(row.Cells[1], s.Result ?? "—", false, HorizontalAlignment.Center);
                            AddCell(row.Cells[2], s.NormalRange ?? s.AnalyzeSubItem?.NormalRange ?? "—", false, HorizontalAlignment.Center);
                            AddCell(row.Cells[3], s.AnalyzeSubItem?.Unit ?? "—", false, HorizontalAlignment.Center);

                            currentRowCount++;
                        }
                    }
                    else
                    {
                        var result = results.FirstOrDefault(r => r.AnalyzeTypeId == type.Id);
                        if (result != null)
                        {
                            var row = table.AddRow();
                            AddCell(row.Cells[0], type.AnalyzeName ?? "—", false, HorizontalAlignment.Left);
                            AddCell(row.Cells[1], result.Result ?? "—", false, HorizontalAlignment.Center);
                            AddCell(row.Cells[2], result.NormalRange ?? "—", false, HorizontalAlignment.Center);
                            AddCell(row.Cells[3], type.Unit ?? "—", false, HorizontalAlignment.Center);

                            currentRowCount++;
                        }
                    }
                }

                var spacer = section.AddParagraph();
                spacer.AppendText("");
                spacer.Format.AfterSpacing = 2;
            }
        }

        private static void AddCell(TableCell cell, string text, bool bold = false, HorizontalAlignment align = HorizontalAlignment.Left, bool isBlue = false)
        {
            var para = cell.AddParagraph();
            para.AppendText(text);
            para.Format.HorizontalAlignment = align;

            var textRange = para.ChildObjects[0] as TextRange;
            if (textRange != null)
            {
                textRange.CharacterFormat.Bold = bold;
                textRange.CharacterFormat.FontSize = 12;
                textRange.CharacterFormat.FontName = "Times New Roman";
                if (isBlue)
                    textRange.CharacterFormat.TextColor = Color.Blue;
            }
        }

        //private static void AddQrCode(Document document, PatientApplication application, string unId)
        //{
        //    string qrCodeUrl = $"http://198.163.204.214/Application/ShowApplication/{application.Patient?.GuidId}?unId={unId}";

        //    // Генерация QR
        //    QRCodeGenerator qrGenerator = new();
        //    QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);
        //    QRCode qrCode = new(qrCodeData);
        //    Bitmap qrBitmap = qrCode.GetGraphic(4);

        //    using var qrStream = new MemoryStream();
        //    qrBitmap.Save(qrStream, System.Drawing.Imaging.ImageFormat.Png);
        //    qrStream.Position = 0;

        //    if (document.Sections.Count == 0)
        //        document.AddSection();

        //    // ✅ Берем последнюю секцию
        //    Section section = document.Sections[document.Sections.Count - 1];

        //    // 🔹 Получаем нижний колонтитул
        //    HeaderFooter footer = section.HeadersFooters.Footer;

        //    // Добавляем абзац в футер
        //    Paragraph qrParagraph = footer.AddParagraph();
        //    qrParagraph.Format.HorizontalAlignment = HorizontalAlignment.Right;

        //    // Вставляем QR в футер
        //    DocPicture qrPicture = qrParagraph.AppendPicture(qrStream);
        //    qrPicture.Width = 60;
        //    qrPicture.Height = 60;
        //}

        private static void AddQrCode(Document document, PatientApplication application, string unId)
        {
            string qrCodeUrl = $"http://198.163.204.214/Application/ShowApplication/{application.Patient?.GuidId}?unId={unId}";

            // Генерация QR
            QRCodeGenerator qrGenerator = new();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new(qrCodeData);
            Bitmap qrBitmap = qrCode.GetGraphic(6);   // ← единственный qrBitmap

            using var qrStream = new MemoryStream();
            qrBitmap.Save(qrStream, System.Drawing.Imaging.ImageFormat.Png);
            qrStream.Position = 0;

            if (document.Sections.Count == 0)
                document.AddSection();

            Section section = document.Sections[document.Sections.Count - 1];
            HeaderFooter footer = section.HeadersFooters.Footer;
            Paragraph qrParagraph = footer.AddParagraph();
            qrParagraph.Format.HorizontalAlignment = HorizontalAlignment.Right;

            DocPicture qrPicture = qrParagraph.AppendPicture(qrStream);
            qrPicture.Width = 90;
            qrPicture.Height = 90;

        }




        //[Authorize(Roles = "Doctor, Admin")]
        [HttpGet]
        public IActionResult AddPrescription(int applicationId)
        {
            var application = _context.PatientApplications?
                .Include(a => a.Patient)
                .Include(a => a.Prescriptions)
                .FirstOrDefault(a => a.Id == applicationId);

            if (application == null)
            {
                return NotFound("Заявка не найдена.");
            }

            ViewBag.PatientName = $"{application.Patient?.Surname} {application.Patient?.Name} {application.Patient?.Lastname}";
            ViewBag.ApplicationId = application.Id;

            // ✅ 1. Если есть назначение — используем его
            var existingPrescription = application.Prescriptions.FirstOrDefault();
            string finalText = existingPrescription?.Text!;

            // ✅ 2. Если назначения нет — пробуем взять из AnalyzeTypes → TextResult
            if (string.IsNullOrWhiteSpace(finalText))
            {
                var selectedAnalyzeIds = application.SelectedAnalyzeTypes?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList() ?? new List<int>();

                var textBlocks = _context.AnalyzeTypes!
                    .Where(at => selectedAnalyzeIds.Contains(at.Id) && at.TypeAnalyzeID == false && !string.IsNullOrWhiteSpace(at.TextResult))
                    .Select(at => at.TextResult)
                    .ToList();

                if (textBlocks.Any())
                {
                    finalText = string.Join("\n\n", textBlocks);
                }
            }

            ViewBag.PrescriptionText = finalText ?? "";
            return View(application);
        }




        [HttpPost]
        public IActionResult AddPrescription(int applicationId, string prescriptionText)
        {
            var application = _context.PatientApplications?
                .Include(a => a.Prescriptions)
                .FirstOrDefault(a => a.Id == applicationId);

            if (application == null)
            {
                return NotFound("Заявка не найдена.");
            }

            var existingPrescription = application.Prescriptions.FirstOrDefault();

            if (existingPrescription != null)
            {
                existingPrescription.Text = prescriptionText;
                existingPrescription.AddDate = DateTime.Now;
            }
            else
            {
                var prescription = new Prescription
                {
                    PatientApplicationId = applicationId,
                    Text = prescriptionText,
                    AddDate = DateTime.Now
                };
                _context.Prescriptions!.Add(prescription);
            }

            _context.SaveChanges();

            return RedirectToAction("PatientApplications", new { patientId = application.PatientId });
        }

        public class DiscountRequest
        {
            public decimal TotalCost { get; set; }
            public decimal Discount { get; set; }
        }

        #region "HELPER METHODS"

        private static void ReplaceTextInDocument(Spire.Doc.Document document, string placeholder, string replacement)
        {
            foreach (Section section in document.Sections)
            {
                foreach (Paragraph paragraph in section.Paragraphs)
                {
                    ReplaceTextInParagraph(paragraph, placeholder, replacement);
                }
                foreach (Table table in section.Tables)
                {
                    foreach (TableRow row in table.Rows)
                    {
                        foreach (TableCell cell in row.Cells)
                        {
                            foreach (Paragraph cellParagraph in cell.Paragraphs)
                            {
                                ReplaceTextInParagraph(cellParagraph, placeholder, replacement);
                            }
                        }
                    }
                }
            }
        }


        private static void ReplaceTextInParagraph(Paragraph paragraph, string placeholder, string replacement)
        {
            foreach (DocumentObject obj in paragraph.ChildObjects)
            {
                if (obj is TextRange textRange && textRange.Text.Contains(placeholder))
                {
                    textRange.Text = textRange.Text.Replace(placeholder, replacement);
                }
            }
        }

        [HttpGet]
        public IActionResult GetAnalysisResults(int applicationId)
        {
            var application = _context.PatientApplications?
                .Include(a => a.AnalysisResults)
                    .ThenInclude(ar => ar.AnalyzeType)
                .Include(a => a.Prescriptions)
                .FirstOrDefault(a => a.Id == applicationId);

            if (application == null)
            {
                return NotFound("Заявка не найдена.");
            }

            // Проверка на наличие назначения
            var selectedAnalyzeIds = application.SelectedAnalyzeTypes?
                .Split(',')
                .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var hasPrescription = _context.AnalyzeTypes!
                .Any(at => selectedAnalyzeIds!.Contains(at.Id) && at.TypeAnalyzeID == false);

            if (hasPrescription)
            {
                var prescription = application.Prescriptions.FirstOrDefault();
                return Json(new
                {
                    IsPrescription = true,
                    Text = prescription?.Text ?? "Назначение не указано"
                });
            }

            var results = application.AnalysisResults
                .Select(ar => new
                {
                    AnalyzeName = ar.AnalyzeType?.AnalyzeName ?? "Неизвестно",
                    Result = ar.Result,
                    NormalRange = ar.NormalRange,
                    ResultDate = ar.ResultDate.HasValue ? ar.ResultDate.Value.ToString("dd.MM.yyyy HH:mm") : ""
                }).ToList();

            return Json(new
            {
                IsPrescription = false,
                Data = results
            });
        }

        private string GetDoctorsNames(string? doctorIds)
        {
            if (string.IsNullOrWhiteSpace(doctorIds))
                return "Неизвестно";

            var idsList = doctorIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(id => id.Trim())
                                   .ToList();

            var doctors = _context.Users
                .Where(u => idsList.Contains(u.Id))
                .Select(u => $"{u.LastName} {u.FirsName}")
                .ToList();

            return doctors.Any() ? string.Join(", ", doctors) : "Неизвестно";
        }

        private List<User> GetDoctorsList(string? doctorIds)
        {
            if (string.IsNullOrWhiteSpace(doctorIds))
                return new List<User>();

            var idsList = doctorIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                   .Select(id => id.Trim())
                                   .ToList();

            return _userManager.Users
                .Where(u => idsList.Contains(u.Id))
                .ToList();
        }


        [HttpPost]
        public IActionResult GetTotalCost([FromBody] List<int> selectedAnalyzeIds)
        {
            if (selectedAnalyzeIds == null || !selectedAnalyzeIds.Any())
                return Json(new { success = true, totalCost = 0 });

            var totalCost = _context.AnalyzeTypes!
                .Where(a => selectedAnalyzeIds.Contains(a.Id))
                .Sum(a => a.Price);

            return Json(new { success = true, totalCost });
        }

        [HttpPost]
        public IActionResult AddToPaymentAmount(int applicationId, string additionalAmount)
        {
            var application = _context.PatientApplications?.FirstOrDefault(a => a.Id == applicationId);
            if (application == null)
            {
                return NotFound("Заявка не найдена.");
            }
            //application.PaymentAmount = (application.PaymentAmount ?? 0) + float.Parse(additionalAmount);

            if (float.TryParse(additionalAmount.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedAmount))
            {
                application.PaymentAmount = (application.PaymentAmount ?? 0) + parsedAmount;
            }
            else
            {
                ViewBag.AlertMessage = "Неправильно введена сумма. (Либо есть сторонние знаки, либо сумма слишком большая!)";
                return View("PatientApplications");
            }

            if (application.PaymentAmount >= application.FinalCost)
            {
                application.IsFullyPaid = true;
            }

            _context.SaveChanges();

            return RedirectToAction("PatientApplications", new { patientId = application.PatientId });
        }

        [HttpPost]
        public IActionResult UpdateDiscount(int applicationId, int discount)
        {
            if (discount < 0 || discount > 100)
            {
                return BadRequest("Некорректное значение скидки.");
            }

            var application = _context.PatientApplications?.FirstOrDefault(a => a.Id == applicationId);
            if (application == null)
            {
                return NotFound("Заявка не найдена.");
            }
            application.Discount = discount;
            application.FinalCost = application.TotalCost * (1 - discount / 100.0);
            application.IsFullyPaid = application.PaymentAmount >= application.FinalCost;

            _context.SaveChanges();

            return RedirectToAction("PatientApplications", new { patientId = application.PatientId });
        }

        [HttpPost]
        public IActionResult UpdateDiscountedCost([FromBody] DiscountRequest request)
        {
            if (request.TotalCost < 0 || request.Discount < 0 || request.Discount > 100)
            {
                return Json(new { success = false, message = "Некорректные данные." });
            }

            var discountedCost = request.TotalCost - (request.TotalCost * request.Discount / 100);

            return Json(new { success = true, discountedCost });
        }


        [HttpGet]
        public IActionResult RedirectToResultEntry(int applicationId)
        {
            var application = _context.PatientApplications?
                .FirstOrDefault(a => a.Id == applicationId);

            if (application == null || string.IsNullOrEmpty(application.SelectedAnalyzeTypes))
            {
                return NotFound("Заявка не найдена или не содержит анализов.");
            }

            var selectedIds = application.SelectedAnalyzeTypes
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            var analyzeTypes = _context.AnalyzeTypes!
                .Where(at => selectedIds.Contains(at.Id))
                .ToList();

            // Все ли это назначения?
            bool allArePrescriptions = analyzeTypes.All(at => !at.TypeAnalyzeID);
            bool hasAnalyzes = analyzeTypes.Any(at => at.TypeAnalyzeID);

            if (allArePrescriptions)
            {
                return RedirectToAction("AddPrescription", new { applicationId });
            }
            else
            {
                return RedirectToAction("BulkAddAnalysisResults", new { applicationId });
            }
        }


        [HttpPost]
        public IActionResult RouteToAnalysisResult(int applicationId)
        {
            var application = _context.PatientApplications!
                .FirstOrDefault(a => a.Id == applicationId);

            if (application == null)
                return NotFound("Заявка не найдена.");

            var analyzeIds = application.SelectedAnalyzeTypes?
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();

            if (analyzeIds == null || !analyzeIds.Any())
                return RedirectToAction("PatientApplications", new { patientId = application.PatientId });

            var analyzeTypes = _context.AnalyzeTypes!
                .Where(at => analyzeIds.Contains(at.Id))
                .ToList();

            bool allPrescriptions = analyzeTypes.All(at => at.TypeAnalyzeID == false);
            bool allAnalyzes = analyzeTypes.All(at => at.TypeAnalyzeID == true);

            if (allPrescriptions)
            {
                return RedirectToAction("AddPrescription", new { applicationId });
            }

            if (allAnalyzes)
            {
                return RedirectToAction("BulkAddAnalysisResults", new { applicationId });
            }

            // Смешанные типы
            TempData["Alert"] = "В заявке выбраны и анализы, и назначения. Пожалуйста, разделите их.";
            return RedirectToAction("PatientApplications", new { patientId = application.PatientId });
        }


        #endregion

    }
}
