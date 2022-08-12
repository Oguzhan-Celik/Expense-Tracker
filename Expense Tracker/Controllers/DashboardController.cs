using Expense_Tracker.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Expense_Tracker.Controllers
{
    public class DashboardController : Controller
    {

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ActionResult> Index()
        {
            //last 7 days
            DateTime now = DateTime.Now;
            DateTime StartDate = new DateTime(now.Year,now.Month,1);
            DateTime EndDate = StartDate.AddMonths(1).AddDays(-1);
            CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
            culture.NumberFormat.CurrencyNegativePattern = 1;

            List<Transaction> SelectedTransactions = await _context.Transactions
                .Include(x =>x.Category)
                .Where(y => y.Date>=StartDate&&y.Date<=EndDate)
                .ToListAsync();

            //total income
            int TotalIncome = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .Sum(j => j.Amount);
            ViewBag.TotalIncome = String.Format(culture, "{0:C0}", TotalIncome);

            //total expense
            int TotalExpense = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .Sum(j => j.Amount);
            ViewBag.TotalExpense = String.Format(culture, "{0:C0}", TotalExpense);

            //balance
            int Balance = TotalIncome - TotalExpense;
            ViewBag.Balance = String.Format(culture, "{0:C0}", Balance);

            //doughnut chart - expense by category 
            ViewBag.DoughnutChartData = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Category.CategoryId)
                .Select(k => new
                {
                    categoryTitleWithIcon = k.First().Category.Icon+" "+ k.First().Category.Title,
                    amount = k.Sum(j => j.Amount),
                    formattedAmount = k.Sum(j=>j.Amount).ToString("C0"),
                })
                .OrderByDescending(l=>l.amount)
                .ToList();

            //Spline Chart - Income vs Expense 
            //Income
            List<SplineChartData> IncomeSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Income")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    income = k.Sum(l => l.Amount)
                })
                .ToList();

            //Expense
            List<SplineChartData> ExpenseSummary = SelectedTransactions
                .Where(i => i.Category.Type == "Expense")
                .GroupBy(j => j.Date)
                .Select(k => new SplineChartData()
                {
                    day = k.First().Date.ToString("dd-MMM"),
                    expense = k.Sum(l => l.Amount)
                })
                .ToList();

            //Combine Income & Expense
            string[] Last7Days = Enumerable.Range(0, now.Day <= 15? 15: EndDate.Day)
                .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
                .ToArray();

            ViewBag.SplineCharData = from day in Last7Days
                                     join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                     from income in dayIncomeJoined.DefaultIfEmpty()
                                     join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                     from expense in expenseJoined.DefaultIfEmpty()
                                     select new
                                     {
                                         day = day,
                                         income = income == null ? 0 : income.income,
                                         expense = expense == null ? 0 : expense.expense,
                                     };
            //recent transaction
            ViewBag.RecentTransaction = await _context.Transactions
                .Include(i => i.Category)
                .OrderByDescending(j => j.Date)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }

    public class SplineChartData
    {
        public string day;
        public int income;
        public int expense;
    }
}
