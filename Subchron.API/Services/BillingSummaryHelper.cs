namespace Subchron.API.Services;

// Shared billing math for the billing page and any receipts or emails.
public static class BillingSummaryHelper
{
    // PHP to USD rate used for display; adjust or move to config as needed.
    public const decimal PhpToUsdRate = 60.3573m;

    // Tax rate; set to something else when you add tax.
    public const decimal TaxRate = 0m;

    // Computes subtotal, tax, total and formatted strings; free trial means total due today is 0.
    public static BillingSummaryVm GetSummary(decimal amountPesos, bool isFreeTrial, string planName)
    {
        var subtotal = isFreeTrial ? 0m : amountPesos;
        var tax = subtotal * TaxRate;
        var totalDueToday = subtotal + tax;
        var amountUsd = amountPesos > 0 ? amountPesos / PhpToUsdRate : 0m;

        return new BillingSummaryVm
        {
            AmountPerMonthPesos = amountPesos,
            AmountPerMonthFormatted = FormatPeso(amountPesos),
            SubtotalPesos = subtotal,
            SubtotalFormatted = FormatPeso(subtotal),
            TaxPesos = tax,
            TaxFormatted = FormatPeso(tax),
            TotalDueTodayPesos = totalDueToday,
            TotalDueTodayFormatted = FormatPeso(totalDueToday),
            AmountUsdFormatted = amountUsd > 0 ? amountUsd.ToString("N2") + " USD" : null,
            ExchangeRateNote = "1 USD = " + PhpToUsdRate.ToString("N4") + " PHP. Charges can vary based on exchange rates.",
            PlanName = planName ?? "Subscription",
            IsFreeTrial = isFreeTrial,
            BilledMonthly = !isFreeTrial,
            AnnualPerMonthPesos = amountPesos > 0 ? amountPesos * 0.8m : 0m, // example: 20% off annual
            AnnualSavingsPesos = amountPesos > 0 ? amountPesos * 12 * 0.2m : 0m
        };
    }

    public static string FormatPeso(decimal value)
    {
        return "₱" + value.ToString("N2");
    }

    public class BillingSummaryVm
    {
        public decimal AmountPerMonthPesos { get; set; }
        public string AmountPerMonthFormatted { get; set; } = "";
        public decimal SubtotalPesos { get; set; }
        public string SubtotalFormatted { get; set; } = "";
        public decimal TaxPesos { get; set; }
        public string TaxFormatted { get; set; } = "";
        public decimal TotalDueTodayPesos { get; set; }
        public string TotalDueTodayFormatted { get; set; } = "";
        public string? AmountUsdFormatted { get; set; }
        public string ExchangeRateNote { get; set; } = "";
        public string PlanName { get; set; } = "";
        public bool IsFreeTrial { get; set; }
        public bool BilledMonthly { get; set; }
        public decimal AnnualPerMonthPesos { get; set; }
        public decimal AnnualSavingsPesos { get; set; }
    }
}
