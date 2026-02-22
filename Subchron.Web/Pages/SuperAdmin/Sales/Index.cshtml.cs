using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Subchron.Web.Pages.SuperAdmin.Sales
{
    public class IndexModel : PageModel
    {
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetProfit => TotalIncome - TotalExpenses;

        // Filters
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }
        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? DateStart { get; set; }
        [BindProperty(SupportsGet = true)]
        public DateTime? DateEnd { get; set; }

        // Pagination
        [BindProperty(SupportsGet = true)]
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }

        public List<TransactionViewModel> Transactions { get; set; } = new();

        public SelectList TypeOptions { get; set; } = new SelectList(new[] { "Income", "Expense" });

        [BindProperty]
        public TransactionInputModel NewTransaction { get; set; } = new();

        public void OnGet()
        {
            TypeOptions = new SelectList(new[] { "Income", "Expense" });
            LoadData();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                TypeOptions = new SelectList(new[] { "Income", "Expense" });
                LoadData();
                return Page();
            }

            // In a real app, save to DB here.
            // For this mock, we simply reload.
            return RedirectToPage();
        }

        private void LoadData()
        {
            // 1. Generate Mock Data (In real app, this comes from DB)
            var allTransactions = new List<TransactionViewModel>
            {
                new() { Id = 1, Date = DateTime.Now.AddDays(-2), Description = "Subscription Payment - Org A", Amount = 1500.00m, Type = TransactionType.Income, ReferenceNumber="OR-2023-001", Tin="123-456-789-000", Category="Subscription", TaxAmount=160.71m, Notes="VAT Inclusive" },
                new() { Id = 2, Date = DateTime.Now.AddDays(-5), Description = "Server Hosting Bill (Azure)", Amount = 500.00m, Type = TransactionType.Expense, ReferenceNumber="INV-MS-992", Tin="987-654-321-000", Category="Infrastructure", TaxAmount=0, Notes="Zero Rated" },
                new() { Id = 3, Date = DateTime.Now.AddDays(-1), Description = "Enterprise Plan - Org B", Amount = 5000.00m, Type = TransactionType.Income, ReferenceNumber="OR-2023-002", Tin="111-222-333-000", Category="Enterprise", TaxAmount=535.71m, Notes="VAT Inclusive" },
                new() { Id = 4, Date = DateTime.Now, Description = "Marketing Ads (FB)", Amount = 2000.00m, Type = TransactionType.Expense, ReferenceNumber="INV-FB-221", Tin="000-000-000-000", Category="Marketing", TaxAmount=0, Notes="Foreign Corp" },
                new() { Id = 5, Date = DateTime.Now.AddDays(-10), Description = "Freelance Dev Support", Amount = 3000.00m, Type = TransactionType.Expense, ReferenceNumber="PVC-001", Tin="555-555-555-000", Category="Personnel", TaxAmount=300.00m, Notes="Withholding Tax Applied" },
                // Add more specific mock data for pagination testing
                new() { Id = 6, Date = DateTime.Now.AddDays(-12), Description = "Consulting Fee", Amount = 1200.00m, Type = TransactionType.Income, ReferenceNumber="OR-2023-003", Tin="123-123-123-000", Category="Services", TaxAmount=128.57m },
                new() { Id = 7, Date = DateTime.Now.AddDays(-15), Description = "Office Supplies", Amount = 150.00m, Type = TransactionType.Expense, ReferenceNumber="INV-OFF-001", Tin="999-999-999-000", Category="Office", TaxAmount=16.07m },
            };

            // 2. Calculate Totals (Global, before filter usually, or after? Let's do Global for the top cards)
            TotalIncome = allTransactions.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            TotalExpenses = allTransactions.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount);

            // 3. Apply Filters
            var query = allTransactions.AsQueryable();

            if (!string.IsNullOrEmpty(SearchTerm))
            {
                var term = SearchTerm.ToLower();
                query = query.Where(t => t.Description.ToLower().Contains(term) || t.ReferenceNumber.ToLower().Contains(term) || t.Category.ToLower().Contains(term));
            }

            if (!string.IsNullOrEmpty(TypeFilter) && Enum.TryParse<TransactionType>(TypeFilter, out var typeEnum))
            {
                query = query.Where(t => t.Type == typeEnum);
            }

            if (DateStart.HasValue)
            {
                query = query.Where(t => t.Date.Date >= DateStart.Value.Date);
            }

            if (DateEnd.HasValue)
            {
                query = query.Where(t => t.Date.Date <= DateEnd.Value.Date);
            }

            // 4. Pagination Logic
            TotalCount = query.Count();
            TotalPages = (int)Math.Ceiling(TotalCount / (double)PageSize);
            
            Transactions = query
                .OrderByDescending(t => t.Date)
                .Skip((CurrentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();
        }

        public class TransactionViewModel
        {
            public int Id { get; set; }
            public DateTime Date { get; set; }
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public TransactionType Type { get; set; }
            
            // BIR / System Details
            public string ReferenceNumber { get; set; } = string.Empty; // OR or SI or Invoice
            public string Tin { get; set; } = string.Empty;
            public decimal TaxAmount { get; set; }
            public string Category { get; set; } = string.Empty;
            public string? Notes { get; set; }
        }

        public class TransactionInputModel
        {
            [Required]
            public string Description { get; set; } = string.Empty;

            [Required]
            [Range(0.01, 10000000, ErrorMessage = "Amount must be greater than 0")]
            public decimal Amount { get; set; }

            [Required]
            public TransactionType Type { get; set; } = TransactionType.Expense;

            [Required(ErrorMessage = "Reference Number (OR/SI) is required")]
            public string ReferenceNumber { get; set; } = string.Empty;

            public string? Tin { get; set; }
            public decimal TaxAmount { get; set; }
            public string? Category { get; set; }
            public string? Notes { get; set; }
        }

        public enum TransactionType
        {
            Income,
            Expense
        }
    }
}
