namespace Subchron.API.Models.Entities;

public class PayrollRun
{
    public int PayrollRunID { get; set; }
    public int OrgID { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string PayCycle { get; set; } = "SemiMonthly";
    public string CompensationBasis { get; set; } = "Monthly";
    public string Status { get; set; } = "Processed";
    public int ProcessedCount { get; set; }
    public decimal TotalGrossPay { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalNetPay { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public int? ProcessedByUserID { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<PayrollRunEmployee> Employees { get; set; } = new List<PayrollRunEmployee>();
}

public class PayrollRunEmployee
{
    public int PayrollRunEmployeeID { get; set; }
    public int PayrollRunID { get; set; }
    public int OrgID { get; set; }
    public int EmpID { get; set; }
    public string EmpNumber { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public decimal WorkedHours { get; set; }
    public decimal OvertimeHours { get; set; }
    public decimal BasePay { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal Allowances { get; set; }
    public decimal GrossPay { get; set; }
    public decimal Deductions { get; set; }
    public decimal Tax { get; set; }
    public decimal NetPay { get; set; }
    public string FormulaSummary { get; set; } = string.Empty;
    public string BreakdownJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
