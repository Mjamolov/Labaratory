using Labaratory.DbContext;
using Labaratory.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList.Extensions;
using System.Collections.Generic;

namespace Labaratory.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly ApplicationContext _context;
        private readonly UserManager<User> _userManager;

        public StatisticsController(ApplicationContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IActionResult Index(string search, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 10)
        {
            var query = _context.Statistics!.AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(s => s.ExpenseName.Contains(search));
            }

            if (startDate.HasValue)
            {
                query = query.Where(s => s.AddDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(s => s.AddDate <= endDate.Value);
            }

            var expenses = query.OrderByDescending(s => s.AddDate).ToPagedList(page, pageSize);

            return View(expenses);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Statistics statistics)
        {
            if (ModelState.IsValid)
            {
                statistics.AddDate = DateTime.Now;
                _context.Statistics!.Add(statistics);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(statistics);
        }

        public IActionResult Edit(int id)
        {
            var statistics = _context.Statistics!.Find(id);
            if (statistics == null)
            {
                return NotFound();
            }
            return View(statistics);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Statistics statistics)
        {
            if (ModelState.IsValid)
            {
                statistics.AddDate = DateTime.Now;
                _context.Entry(statistics).State = EntityState.Modified;
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(statistics);
        }

        [HttpGet, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var statistics = _context.Statistics!.Find(id);
            if (statistics != null)
            {
                _context.Statistics.Remove(statistics);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> FinanceReport(DateTime? startDate, DateTime? endDate)
        {
            DateTime fromDate = startDate ?? DateTime.Today.AddMonths(-1);
            DateTime toDate = endDate ?? DateTime.Today;

            ViewBag.StartDate = fromDate.ToString("yyyy-MM-dd");
            ViewBag.EndDate = toDate.ToString("yyyy-MM-dd");

            var incomes = await _context.PatientApplications!
                .Where(p => p.AddDate >= fromDate && p.AddDate <= toDate)
                .Select(p => new { PaymentAmount = (decimal?)p.PaymentAmount ?? 0, AddDate = p.AddDate })
                .ToListAsync();

            var expenses = await _context.Statistics!
                .Where(s => s.AddDate >= fromDate && s.AddDate <= toDate)
                .Select(s => new { s.Amount, AddDate = s.AddDate })
                .ToListAsync();

            decimal totalIncome = incomes.Sum(p => p.PaymentAmount);
            decimal totalExpenses = expenses.Sum(s => s.Amount);
            decimal balance = totalIncome - totalExpenses;
            decimal expensePercentage = totalIncome > 0 ? (totalExpenses / totalIncome) * 100 : 0;

            var dates = incomes.Select(i => i.AddDate.Date)
                .Union(expenses.Select(e => e.AddDate.Date))
                .Distinct()
                .OrderBy(d => d)
                .ToList();

            var incomeData = dates.Select(d => incomes.Where(i => i.AddDate.Date == d).Sum(i => i.PaymentAmount)).ToList();
            var expenseData = dates.Select(d => expenses.Where(e => e.AddDate.Date == d).Sum(e => e.Amount)).ToList();

            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpenses = totalExpenses;
            ViewBag.Balance = balance;
            ViewBag.ExpensePercentage = Math.Round(expensePercentage, 2);
            ViewBag.Dates = dates.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            ViewBag.Incomes = incomeData;
            ViewBag.Expenses = expenseData;

            return View();
        }

        [HttpGet]
        public IActionResult DoctorPayouts(DateTime? startDate, DateTime? endDate)
        {
            DateTime from = startDate ?? DateTime.Today.AddMonths(-1);
            DateTime to = endDate ?? DateTime.Today;

            var paidApplications = _context.PatientApplications!
                .Where(p => p.IsFullyPaid && p.AddDate >= from && p.AddDate <= to)
                .ToList();

            var analyzeTypes = _context.AnalyzeTypes!.ToList();

            var doctorPayments = new Dictionary<string, (string DoctorName, int TotalAnalyzes, decimal TotalAmount, decimal FinalPayout)>();

            foreach (var app in paidApplications)
            {
                var analyzeIds = app.SelectedAnalyzeTypes?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                if (analyzeIds == null || !analyzeIds.Any())
                    continue;

                var appAnalyzes = analyzeTypes
                    .Where(at => analyzeIds.Contains(at.Id) && !string.IsNullOrEmpty(at.DoctorId))
                    .ToList();

                var selectedDoctorIds = app.SelectedDoctors?
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => id.Trim())
                    .ToHashSet() ?? new HashSet<string>();

                foreach (var analyze in appAnalyzes)
                {
                    var doctorId = analyze.DoctorId;

                    if (!selectedDoctorIds.Contains(doctorId!))
                        continue;

                    var doctorName = analyze.DoctorName;
                    decimal price = analyze.Price.HasValue
    ? (decimal)analyze.Price.Value
    : 0m;


                    if (app.Discount.HasValue && app.Discount.Value > 0)
                    {
                        var discountMultiplier = 1 - ((decimal)app.Discount.Value / 100m);
                        price = Math.Round(price * discountMultiplier, 2);
                    }

                    // ✅ используем процент выплаты из AnalyzeTypes
                    decimal payoutPercent = analyze.DoctorPayoutPercentage ?? 0m;
                    decimal payout = Math.Round(price * (payoutPercent / 100m), 2);

                    if (doctorPayments.ContainsKey(doctorId))
                    {
                        var entry = doctorPayments[doctorId];
                        doctorPayments[doctorId] = (
                            entry.DoctorName,
                            entry.TotalAnalyzes + 1,
                            entry.TotalAmount + price,
                            entry.FinalPayout + payout
                        );
                    }
                    else
                    {
                        doctorPayments[doctorId] = (doctorName, 1, price, payout);
                    }
                }

            }

            return View("DoctorPayouts", doctorPayments);
        }

    }
}